using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CNA.Interop;

namespace CNA.Content;

/// <summary>
/// Builds one object from a <c>.cnj</c> descriptor's raw JSON.
///
/// The <c>.cnj</c> counterpart of <see cref="ManagedContentTypeReader"/>: a registered reader
/// answers for a compiled <c>.xnb</c>, this answers for a loose <c>.cnj</c> descriptor, and
/// <see cref="ContentManager.LoadForeign{T}"/> takes either without being told which.
/// </summary>
public abstract class CnjLoader
{
    /// <summary>Builds the object.</summary>
    /// <param name="descriptorJson">The descriptor's whole text. Borrowed for the duration of the
    /// call, so anything kept past it must be copied -- which is automatic here, since this
    /// receives a <see cref="string"/> the binding has already materialised.</param>
    public abstract object Load(string descriptorJson);
}

/// <summary>
/// A <see cref="CnjLoader"/> registered against one content manager for one <c>"type"</c> value.
///
/// <b>Per manager, and there is no unregister.</b> That is the one way this differs from
/// <see cref="ContentTypeReaderRegistration"/>, which is process-wide and hands back something to
/// release: the canonical <c>.cnj</c> table belongs to the content manager and goes with it. So the
/// managed roots here are held for the manager's lifetime, released when it is disposed, and the
/// type carries no <see cref="IDisposable"/> to suggest otherwise.
/// </summary>
public sealed class CnjLoaderRegistration
{
    /// <summary>What the native context pointer roots: the loader plus its registration, so the
    /// callback needs one <see cref="GCHandle"/> lookup rather than a side table.</summary>
    private sealed class Slot(CnjLoader loader, CnjLoaderRegistration owner)
    {
        public CnjLoader Loader { get; } = loader;

        public CnjLoaderRegistration Owner { get; } = owner;
    }

    private readonly List<GCHandle> _roots = [];
    private readonly object _gate = new();

    private CnjLoaderRegistration(string typeName)
    {
        TypeName = typeName;
    }

    /// <summary>The <c>"type"</c> value in a descriptor that selects this loader.</summary>
    public string TypeName { get; }

    /// <summary>
    /// Registers <paramref name="loader"/> for descriptors whose <c>"type"</c> is
    /// <paramref name="typeName"/>, against the content manager
    /// <paramref name="contentManagerHandle"/> names.
    /// </summary>
    /// <exception cref="CnaException">If that type name is already registered on this manager. A
    /// descriptor naming nothing registered fails its load rather than falling back, which is the
    /// same rule a compiled asset naming an unregistered reader follows.</exception>
    internal static unsafe CnjLoaderRegistration Register(
        nint contentManagerHandle, string typeName, CnjLoader loader)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(loader);

        var registration = new CnjLoaderRegistration(typeName);
        GCHandle root = GCHandle.Alloc(new Slot(loader, registration));

        lock (registration._gate)
        {
            registration._roots.Add(root);
        }

        try
        {
            CnaResult result = CnaStringMarshal.WithStringView(
                typeName,
                view => Native.cna_content_manager_register_cnj_loader_ext(
                    new CnaHandle(contentManagerHandle), view, &OnLoad, GCHandle.ToIntPtr(root)));

            CnaException.ThrowIfFailed(result, nameof(Register));
            return registration;
        }
        catch
        {
            root.Free();
            throw;
        }
    }

    /// <summary>Frees the roots this registration handed to native. Called by the owning
    /// <see cref="ContentManager"/> on disposal, because native's table dies with the manager and
    /// nothing else marks the moment.</summary>
    internal void ReleaseRoots()
    {
        lock (_gate)
        {
            foreach (GCHandle root in _roots)
            {
                root.Free();
            }

            _roots.Clear();
        }
    }

    /// <summary>
    /// Catches rather than unwinding -- an exception crossing an <c>UnmanagedCallersOnly</c>
    /// boundary is undefined behaviour -- and reports through the result, which fails the load that
    /// asked for it.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe CnaResult OnLoad(nint context, CnaStringView descriptor, nint* outObject)
    {
        if (outObject is null || context == 0)
        {
            return CnaResult.InvalidArgument;
        }

        *outObject = 0;

        if (GCHandle.FromIntPtr(context).Target is not Slot slot)
        {
            return CnaResult.InvalidArgument;
        }

        try
        {
            // Not NUL-terminated: the header is explicit that exactly ByteLength bytes are valid,
            // and reading to a terminator would run past a borrowed buffer.
            string json = descriptor.ToManagedString();

            object produced = slot.Loader.Load(json);
            GCHandle root = GCHandle.Alloc(produced);

            lock (slot.Owner._gate)
            {
                slot.Owner._roots.Add(root);
            }

            *outObject = GCHandle.ToIntPtr(root);
            return CnaResult.Success;
        }
        catch
        {
            return CnaResult.Callback;
        }
    }
}
