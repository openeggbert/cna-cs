using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CNA.Interop;

namespace CNA.Content.Cnb;

/// <summary>
/// A game extension's loader for one of its own <c>.cnb</c> asset types.
///
/// This is the CNB counterpart of <see cref="ManagedContentTypeReader"/>, and the two exist for the
/// same reason: a container format is only extensible if a game can teach it about its own types.
/// CNA's header says so directly -- the registry "matches how the <c>.xnb</c> reader table already
/// works" -- so this binding gives it the same managed shape.
/// </summary>
public abstract class CnbAssetLoader
{
    /// <summary>
    /// Builds the asset a document holds.
    /// </summary>
    /// <param name="document">The container, <b>borrowed</b> for the duration of this call. Do not
    /// dispose it and do not keep it: it belongs to whoever started the load, and this object is
    /// non-owning precisely so a loader cannot destroy it by accident.</param>
    /// <param name="assetName">The logical asset name, for diagnostics.</param>
    /// <returns>The loaded object. CNA never dereferences, copies or frees it; its lifetime is this
    /// registration's, which holds it until the registration is disposed.</returns>
    public abstract object Load(CnbDocument document, string assetName);
}

/// <summary>
/// A live registration of a <see cref="CnbAssetLoader"/> with CNA's CNB loader registry. Disposing
/// it withdraws the loader.
///
/// <b>The registry is process-wide</b>, exactly as the <c>.xnb</c> reader table is -- the header is
/// explicit that there is no per-content-manager variant -- so a registration outlives any one
/// <c>Game</c> and two games in one process share it. That is why this is disposable rather than
/// tied to a game.
///
/// <b>The identifier is minted from the name, not chosen.</b> A custom asset type identifier is the
/// hash of its canonical type name, and the registry refuses one that does not hash from the name it
/// is offered under. That is not bureaucracy: it is what makes a 31-bit identifier space safe to
/// share between games that have never heard of each other, because the only way to collide is to
/// pick the same type name. A collision under a *different* name is refused rather than won by the
/// second registration, which would mean loading one game's file with another game's loader.
/// </summary>
public sealed class CnbLoaderRegistration : IDisposable
{
    /// <summary>
    /// Asset type identifiers a <see cref="CnbLoaderRegistration"/> registered, and therefore the
    /// only ones whose produced pointer is a <see cref="GCHandle"/> this binding allocated.
    ///
    /// <b>This exists because the registry is shared with CNA.</b> Built-in loaders live in the same
    /// table -- <c>cna_cnb_loader_registry_register_builtins</c> puts them there and every content
    /// manager calls it -- and what a built-in hands back through <c>out_object</c> is a C++ object
    /// pointer. Treating that as a managed handle is undefined behaviour, not a wrong answer, so
    /// <see cref="CnbLoader.Invoke"/> refuses rather than guesses.
    /// </summary>
    private static readonly HashSet<uint> ManagedTypeIds = [];

    private static readonly object ManagedTypeIdsGate = new();

    internal static bool IsManagedLoader(uint assetTypeId)
    {
        lock (ManagedTypeIdsGate)
        {
            return ManagedTypeIds.Contains(assetTypeId);
        }
    }

    private static void RememberManaged(uint assetTypeId)
    {
        lock (ManagedTypeIdsGate)
        {
            ManagedTypeIds.Add(assetTypeId);
        }
    }

    private static void ForgetManaged(uint assetTypeId)
    {
        lock (ManagedTypeIdsGate)
        {
            ManagedTypeIds.Remove(assetTypeId);
        }
    }

    private readonly GCHandle _self;
    private readonly CnbAssetLoader _loader;
    private readonly List<GCHandle> _roots = [];
    private readonly object _gate = new();

    private bool _disposed;

    private CnbLoaderRegistration(uint assetTypeId, string canonicalTypeName, CnbAssetLoader loader)
    {
        AssetTypeId = assetTypeId;
        CanonicalTypeName = canonicalTypeName;
        _loader = loader;
        _self = GCHandle.Alloc(this);
    }

    /// <summary>The custom asset type identifier this loader is registered for.</summary>
    public uint AssetTypeId { get; }

    /// <summary>The canonical type name the identifier was minted from, and which a file must carry
    /// in its metadata for <see cref="ResolveFor"/> to accept it.</summary>
    public string CanonicalTypeName { get; }

    /// <summary>
    /// The custom asset type identifier a canonical type name hashes to.
    ///
    /// Public because a game needs it to author its own containers: the identifier goes in the
    /// <c>.cnb</c> header and the name goes in its metadata, and the two must agree.
    /// </summary>
    public static uint AssetTypeIdFromName(string canonicalTypeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(canonicalTypeName);

        uint id = 0;
        CnaResult result = CnaStringMarshal.WithStringView(
            canonicalTypeName, view => Native.cna_cnb_asset_type_id_from_name(view, out id));
        CnaException.ThrowIfFailed(result, nameof(AssetTypeIdFromName));
        return id;
    }

    /// <summary>Registers <paramref name="loader"/> for the type named
    /// <paramref name="canonicalTypeName"/>.</summary>
    /// <exception cref="CnaException"><c>InvalidState</c> when that identifier is already registered
    /// under a different name -- see this type's own remarks for why that is refused rather than
    /// overwritten.</exception>
    public static unsafe CnbLoaderRegistration Register(string canonicalTypeName, CnbAssetLoader loader)
    {
        ArgumentException.ThrowIfNullOrEmpty(canonicalTypeName);
        ArgumentNullException.ThrowIfNull(loader);

        uint assetTypeId = AssetTypeIdFromName(canonicalTypeName);
        var registration = new CnbLoaderRegistration(assetTypeId, canonicalTypeName, loader);

        try
        {
            CnaResult result = CnaStringMarshal.WithStringView(
                canonicalTypeName,
                view => Native.cna_cnb_loader_registry_register(
                    assetTypeId,
                    view,
                    (nint)(delegate* unmanaged[Cdecl]<nint, CnaHandle, CnaHandle, CnaStringView, nint*, CnaResult>)
                        &OnLoad,
                    GCHandle.ToIntPtr(registration._self)));
            CnaException.ThrowIfFailed(result, nameof(Register));
            RememberManaged(assetTypeId);
            return registration;
        }
        catch
        {
            registration._self.Free();
            throw;
        }
    }

    /// <summary>Whether any loader is registered for that identifier, CNA's own or a game's.</summary>
    public static bool IsRegistered(uint assetTypeId)
    {
        CnaResult result = Native.cna_cnb_loader_registry_is_registered(assetTypeId, out byte registered);
        CnaException.ThrowIfFailed(result, nameof(IsRegistered));
        return registered != 0;
    }

    /// <summary>The canonical type name registered under an identifier, or empty when none is.</summary>
    public static unsafe string RegisteredTypeName(uint assetTypeId)
    {
        CnaResult sizeResult = Native.cna_cnb_loader_registry_get_registered_type_name_size(
            assetTypeId, out ulong byteCount);
        CnaException.ThrowIfFailed(sizeResult, nameof(RegisteredTypeName));

        if (byteCount == 0)
        {
            return string.Empty;
        }

        var bytes = new byte[checked((int)byteCount)];
        fixed (byte* destination = bytes)
        {
            CnaResult result = Native.cna_cnb_loader_registry_copy_registered_type_name(
                assetTypeId, destination, byteCount, out ulong written);
            CnaException.ThrowIfFailed(result, nameof(RegisteredTypeName));
            return Encoding.UTF8.GetString(bytes, 0, checked((int)written));
        }
    }

    /// <summary>
    /// Registers CNA's own loaders for its built-in asset types.
    ///
    /// Idempotent and process-wide, and every content manager calls it already, so a game normally
    /// need not. It covers the built-ins that need nothing but their own codec -- <c>Curve</c> and
    /// <c>AnimationClip</c>; the rest need a graphics device or the manager itself and are
    /// registered by a content manager.
    ///
    /// <b>A built-in loader cannot be invoked through <see cref="CnbLoader.Invoke"/>.</b> What it
    /// produces is a C++ object, and this binding has no way to turn that pointer into a managed
    /// one; <see cref="CnbLoader.Invoke"/> says so rather than reinterpreting it. Reach CNA's own
    /// asset types through their typed entry points -- <see cref="CnbTexture"/>,
    /// <see cref="CnbModel"/> -- which is what those slices are for.
    /// </summary>
    public static void RegisterBuiltins() =>
        CnaException.ThrowIfFailed(Native.cna_cnb_loader_registry_register_builtins(), nameof(RegisterBuiltins));

    /// <summary>
    /// The loader a document's own header and metadata select.
    ///
    /// <b>Resolution is by file, not by request.</b> A custom-typed container carrying no canonical
    /// type name, or one whose name disagrees with what is registered under its identifier, is
    /// <c>IO</c> -- because each is a statement about the file rather than about the call, and
    /// dispatching anyway would be a silent misinterpretation of someone's content.
    /// </summary>
    public static CnbLoader ResolveFor(CnbDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        CnaResult result = Native.cna_cnb_loader_registry_resolve_for_document(
            document.NativeHandle, out CnaHandle loader);
        GC.KeepAlive(document);
        CnaException.ThrowIfFailed(result, nameof(ResolveFor));
        return new CnbLoader(loader.AsNint, document.AssetTypeId);
    }

    /// <summary>Withdraws the registration and releases every object its loader produced.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _ = Native.cna_cnb_loader_registry_remove(AssetTypeId, out _);
            ForgetManaged(AssetTypeId);
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
    /// Turns the opaque pointer a load handed back into the object the loader produced.
    ///
    /// A lookup, not a transfer: the pointer is a <see cref="GCHandle"/> this registration allocated
    /// and still holds. CNA states it never dereferences, copies or frees the value, so nothing else
    /// could have released it.
    /// </summary>
    internal static object? Resolve(nint produced) =>
        produced == 0 ? null : GCHandle.FromIntPtr(produced).Target;

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
    /// The callback CNA calls.
    ///
    /// It catches rather than letting an exception unwind, because crossing an
    /// <c>UnmanagedCallersOnly</c> boundary with one is undefined behaviour. There is a real error
    /// channel here, so a loader that throws fails the load that asked for it -- which is what a
    /// failing loader should do.
    ///
    /// The document is wrapped non-owningly. A <see cref="CnbDocument"/> that owned this handle
    /// would destroy the caller's container on the way out of the callback, and the header is
    /// explicit that both the document and the content manager are borrowed for the call.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe CnaResult OnLoad(
        nint context, CnaHandle document, CnaHandle contentManager, CnaStringView assetName, nint* outObject)
    {
        if (outObject is null)
        {
            return CnaResult.InvalidArgument;
        }

        *outObject = 0;

        if (context == 0 ||
            GCHandle.FromIntPtr(context).Target is not CnbLoaderRegistration registration ||
            registration._disposed)
        {
            return CnaResult.InvalidArgument;
        }

        try
        {
            using CnbDocument borrowed = CnbDocument.Borrowing(document);
            object produced = registration._loader.Load(borrowed, assetName.ToManagedString());
            *outObject = GCHandle.ToIntPtr(registration.Root(produced));
            return CnaResult.Success;
        }
        catch
        {
            return CnaResult.Callback;
        }
    }
}

/// <summary>
/// A resolved CNB loader, ready to be invoked.
///
/// Owned: <c>cna_cnb_loader_registry_find</c> and <c>_resolve_for_document</c> each document their
/// output as "a new loader handle the caller releases", so this destroys it. Releasing the handle
/// does not withdraw the registration behind it -- the two are separate lifetimes, and conflating
/// them would let a single failed load unregister a game's type.
/// </summary>
public sealed class CnbLoader : IDisposable
{
    private readonly NativeResourceHandle _handle;
    private readonly uint _assetTypeId;

    internal CnbLoader(nint handleValue, uint assetTypeId)
    {
        _handle = new NativeResourceHandle(
            handleValue,
            h => Native.cna_cnb_loader_destroy(new CnaHandle(h)).IsSuccess());
        _assetTypeId = assetTypeId;
    }

    /// <summary>The asset type identifier this loader was found for.</summary>
    public uint AssetTypeId => _assetTypeId;

    /// <summary>
    /// Whether this loader is one a <see cref="CnbLoaderRegistration"/> installed, and therefore
    /// whether <see cref="Invoke"/> can hand its object back.
    ///
    /// <see langword="false"/> for CNA's own built-in loaders, which share the registry.
    /// </summary>
    public bool IsManaged => CnbLoaderRegistration.IsManagedLoader(_assetTypeId);

    /// <summary>The loader registered for an identifier, or <see langword="null"/> when none is.
    /// Answering null rather than throwing because "nothing is registered" is an ordinary question
    /// to ask of a registry.</summary>
    public static CnbLoader? Find(uint assetTypeId)
    {
        CnaResult result = Native.cna_cnb_loader_registry_find(
            assetTypeId, out byte found, out CnaHandle loader);
        CnaException.ThrowIfFailed(result, nameof(Find));
        return found == 0 ? null : new CnbLoader(loader.AsNint, assetTypeId);
    }

    /// <summary>
    /// Runs the loader over a document and returns what it produced.
    /// </summary>
    /// <param name="document">The container. Borrowed; this call does not take it.</param>
    /// <param name="contentManager">The manager the load belongs to. <b>Required</b>, and measured
    /// rather than assumed: passing no manager answers <c>InvalidHandle</c>. The header explains
    /// why -- the canonical loader signature takes the manager by reference, so there is never a
    /// load without one, and a loader may use it to resolve the asset's own dependencies.</param>
    /// <param name="assetName">The logical asset name, passed through for diagnostics.</param>
    public unsafe object? Invoke(CnbDocument document, ContentManager contentManager, string assetName)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(contentManager);
        ArgumentNullException.ThrowIfNull(assetName);

        // The registry is shared with CNA. A built-in loader answers with a C++ object pointer, and
        // the unwrap below would hand that to GCHandle.FromIntPtr -- undefined behaviour rather than
        // a wrong answer. Refusing is the only safe option, because there is no managed object to
        // return and no way to tell that from the pointer itself.
        if (!IsManaged)
        {
            throw new NotSupportedException(
                $"The CNB loader for asset type 0x{_assetTypeId:X8} is one of CNA's own, and what it " +
                "produces is a C++ object rather than a managed one. Use the typed entry points " +
                "(CnbTexture, CnbModel) for CNA's built-in asset types.");
        }

        // The out-pointer lives in an array rather than a local: C# will not let a lambda take the
        // address of a local, and the string marshaller's callback shape is what puts one here.
        var produced = new nint[1];
        CnaResult result;
        fixed (nint* slot = produced)
        {
            nint* captured = slot;
            result = CnaStringMarshal.WithStringView(
                assetName,
                view => Native.cna_cnb_loader_invoke(
                    Handle, document.NativeHandle, new CnaHandle(contentManager.NativeHandleValue),
                    view, captured));
        }

        GC.KeepAlive(this);
        GC.KeepAlive(document);
        GC.KeepAlive(contentManager);
        CnaException.ThrowIfFailed(result, nameof(Invoke));

        return CnbLoaderRegistration.Resolve(produced[0]);
    }

    public void Dispose() => _handle.Dispose();

    private CnaHandle Handle => new(_handle.DangerousGetHandle());
}
