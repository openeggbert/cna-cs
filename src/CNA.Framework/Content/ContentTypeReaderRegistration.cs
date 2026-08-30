using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CNA.Interop;

namespace CNA.Content;

/// <summary>
/// A managed content type reader: the thing an XNA game writes when it ships a custom type through
/// the content pipeline.
///
/// This is what <see cref="ContentTypeReader"/> could not be. That type wraps a reader native
/// already owns, because until <c>cna_content_type_reader_manager_register</c> existed there was no
/// entry point accepting a caller-supplied factory at all -- a derivable base class would have
/// looked extensible while nothing could ever call a subclass. One exists now, so this is
/// derivable and really is called.
/// </summary>
public abstract class ManagedContentTypeReader
{
    /// <summary>Deserializes one object.</summary>
    /// <param name="input">Positioned at this object's first byte. Reading past the object's own
    /// bytes corrupts every read that follows it in the same asset.</param>
    /// <param name="existingInstance">The object to deserialize into, when the registration
    /// declared it accepts one; otherwise <see langword="null"/>.</param>
    public abstract object Read(ContentReader input, object? existingInstance);
}

/// <summary>
/// A live registration of a <see cref="ManagedContentTypeReader"/> factory with the content
/// registry. Disposing it withdraws the factory.
///
/// <b>The registry is process-wide, not per-game</b> -- the header says so explicitly -- so a
/// registration outlives any one <see cref="Game"/>, and two games in one process share it. That is
/// why this is disposable rather than tied to a game's lifetime.
///
/// Assets are read through <see cref="ContentManager.LoadForeign{T}(string)"/>, which is the only route that
/// reaches a caller-supplied reader. Note the ABI's constraint: only compiled <c>.xnb</c> assets
/// get there, because a loose file or a <c>.cnj</c> descriptor is dispatched by requested C++ type
/// and a custom type has none.
/// </summary>
public sealed class ContentTypeReaderRegistration : IDisposable
{
    /// <summary>What a reader's native context pointer actually roots: the reader plus the
    /// registration that made it, so <c>OnRead</c> needs one <see cref="GCHandle"/> lookup rather
    /// than a side table keyed on the reader.</summary>
    private sealed class ReaderSlot(ManagedContentTypeReader reader, ContentTypeReaderRegistration owner)
    {
        public ManagedContentTypeReader Reader { get; } = reader;

        public ContentTypeReaderRegistration Owner { get; } = owner;
    }

    private readonly GCHandle _self;
    private readonly Func<ManagedContentTypeReader> _factory;
    private readonly List<GCHandle> _roots = [];
    private readonly object _gate = new();

    private CnaHandle _registration;
    private bool _disposed;

    private ContentTypeReaderRegistration(string canonicalName, Func<ManagedContentTypeReader> factory)
    {
        CanonicalName = canonicalName;
        _factory = factory;
        _self = GCHandle.Alloc(this);
    }

    /// <summary>The reader name assets declare, as registered.</summary>
    public string CanonicalName { get; }

    /// <summary>
    /// Registers <paramref name="factory"/> under <paramref name="canonicalName"/> -- the reader
    /// name exactly as compiled assets spell it, not the target type's name.
    /// </summary>
    /// <param name="canonicalName">The reader name assets declare.</param>
    /// <param name="targetTypeName">Canonical name of the type this reader produces.</param>
    /// <param name="factory">Called once per file to build a reader instance.</param>
    /// <param name="typeVersion">Matched against the version each file declares.</param>
    /// <param name="canDeserializeIntoExistingObject">Whether
    /// <see cref="ManagedContentTypeReader.Read"/> accepts a non-null existing instance.</param>
    /// <exception cref="CnaException">If a factory is already registered under that name. Refusing
    /// a duplicate is deliberate on the ABI's part -- silently ignoring one, as the canonical
    /// <c>AddTypeCreator</c> does, would hand back a live registration whose factory is never
    /// called, discoverable only from assets deserializing into the wrong type.</exception>
    public static unsafe ContentTypeReaderRegistration Register(
        string canonicalName,
        string targetTypeName,
        Func<ManagedContentTypeReader> factory,
        int typeVersion = 0,
        bool canDeserializeIntoExistingObject = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(canonicalName);
        ArgumentException.ThrowIfNullOrEmpty(targetTypeName);
        ArgumentNullException.ThrowIfNull(factory);

        var registration = new ContentTypeReaderRegistration(canonicalName, factory);

        try
        {
            CnaHandle handle = CnaHandle.Zero;
            CnaResult result = CnaStringMarshal.WithStringView(canonicalName, nameView =>
                CnaStringMarshal.WithStringView(targetTypeName, typeView =>
                {
                    CnaContentTypeReaderCallbacks callbacks = CnaContentTypeReaderCallbacks.Versioned();
                    callbacks.TargetTypeName = typeView;
                    callbacks.TypeVersion = typeVersion;
                    callbacks.CanDeserializeIntoExistingObject = (byte)(canDeserializeIntoExistingObject ? 1 : 0);
                    callbacks.Create = &OnCreate;
                    callbacks.Read = &OnRead;
                    callbacks.Destroy = &OnDestroy;
                    callbacks.Context = GCHandle.ToIntPtr(registration._self);

                    return Native.cna_content_type_reader_manager_register(nameView, &callbacks, out handle);
                }));

            CnaException.ThrowIfFailed(result, nameof(Register));
            registration._registration = handle;
            return registration;
        }
        catch
        {
            registration._self.Free();
            throw;
        }
    }

    /// <summary>
    /// Withdraws the factory, then frees every managed root handed to native.
    ///
    /// The order matters, and is the rule <c>NativeEventBridge</c> already follows: unregister
    /// first, so no in-flight load can reach a root that has been freed. The roots are freed in a
    /// <c>finally</c> so a failing unregister cannot leak them.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        try
        {
            CnaResult result = Native.cna_content_type_reader_manager_unregister(_registration);
            CnaException.ThrowIfFailed(result, nameof(Dispose));
        }
        finally
        {
            lock (_gate)
            {
                foreach (GCHandle root in _roots)
                {
                    root.Free();
                }

                _roots.Clear();
            }

            _self.Free();
        }
    }

    /// <summary>
    /// Turns the <c>void*</c> a load handed back into the object the reader produced.
    ///
    /// A lookup, not a transfer: the pointer is a <see cref="GCHandle"/> this registration
    /// allocated and still holds, which is what makes it safe to call twice for one asset -- as the
    /// ABI's per-name caching means a second load will.
    /// </summary>
    internal static object? Resolve(nint producedObject) =>
        producedObject == 0 ? null : GCHandle.FromIntPtr(producedObject).Target;

    private GCHandle Root(object value)
    {
        GCHandle root = GCHandle.Alloc(value);

        lock (_gate)
        {
            _roots.Add(root);
        }

        return root;
    }

    /// <summary>
    /// Builds one reader instance per file.
    ///
    /// Every callback here catches rather than letting an exception unwind: crossing an
    /// <c>UnmanagedCallersOnly</c> boundary with one is undefined behaviour. Unlike the
    /// game-component callbacks -- which return <c>void</c>, forcing the binding to stash an
    /// exception and rethrow it later -- these have a real error channel, so a reader that fails
    /// fails the load. That is what a failing content reader should do, and it is the one thing
    /// this project explicitly asked for when the route was requested.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe CnaResult OnCreate(nint context, nint* outReaderContext)
    {
        if (outReaderContext is null)
        {
            return CnaResult.InvalidArgument;
        }

        *outReaderContext = 0;

        if (context == 0 ||
            GCHandle.FromIntPtr(context).Target is not ContentTypeReaderRegistration registration ||
            registration._disposed)
        {
            return CnaResult.InvalidArgument;
        }

        try
        {
            var slot = new ReaderSlot(registration._factory(), registration);
            *outReaderContext = GCHandle.ToIntPtr(registration.Root(slot));
            return CnaResult.Success;
        }
        catch
        {
            return CnaResult.Callback;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe CnaResult OnRead(
        nint readerContext, CnaHandle input, nint existingObject, nint* outObject)
    {
        if (outObject is null || readerContext == 0)
        {
            return CnaResult.InvalidArgument;
        }

        *outObject = 0;

        if (GCHandle.FromIntPtr(readerContext).Target is not ReaderSlot slot || slot.Owner._disposed)
        {
            return CnaResult.InvalidArgument;
        }

        try
        {
            // Borrowed, not owned: the handle belongs to native for this callback's duration, and a
            // ContentReader that owned it would destroy native's reader on the way out.
            var stream = new NativeContentStream(input, slot.Reader.GetType().Name);
            var contentReader = ContentReader.Borrowing(stream, input.AsNint);

            object? existing = existingObject == 0 ? null : GCHandle.FromIntPtr(existingObject).Target;
            object produced = slot.Reader.Read(contentReader, existing);

            *outObject = GCHandle.ToIntPtr(slot.Owner.Root(produced));
            return CnaResult.Success;
        }
        catch
        {
            return CnaResult.Callback;
        }
    }

    /// <summary>
    /// Per-instance cleanup. Deliberately a no-op that keeps the root alive.
    ///
    /// Freeing the reader's root here would race the ABI's own per-asset-name caching: a second
    /// load of the same name returns the cached object without re-reading, but nothing guarantees
    /// the reader is not reused first. <see cref="Dispose"/> frees every root at once, when the
    /// registration is withdrawn and no load can be in flight.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnDestroy(nint readerContext)
    {
        // C declares this callback as returning void. It returned a result code here until B2's
        // prototype work compared the managed delegate with the C typedef: a value returned into a
        // caller that expects none is discarded on this ABI and is still the wrong signature.
        _ = readerContext;
    }
}
