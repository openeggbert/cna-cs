using CNA.Interop;

namespace CNA.Content;

/// <summary>
/// Matches real XNA's <c>ContentTypeReader</c>: deserializes one type from a
/// <see cref="ContentReader"/>.
///
/// This wraps a *native* type reader obtained from <see cref="ContentTypeReaderManager"/>, rather
/// than being a base class a game derives from. That is the honest shape for this ABI: the C API
/// creates readers from a registry keyed by canonical type name
/// (<c>cna_content_type_reader_manager_create_reader</c>) and has no route for registering a
/// managed one, so a derivable base class would look extensible while nothing could ever call a
/// subclass.
///
/// This is blocked upstream, not pending here. It was recorded as needing "the same callback-table
/// machinery <c>GameComponent</c> uses" -- which is wrong, and re-checking the header is what
/// showed it: <c>content_readers.h</c> exposes only <c>_clear_type_creators</c>,
/// <c>_get_is_registered</c>, <c>_create_reader</c> and
/// <c>cna_content_register_known_unsupported_xnb_readers</c>. There is no entry point that accepts
/// a caller-supplied factory at all, so no amount of callback machinery on this side can reach the
/// registry. Closing it needs a new registration route in the C API.
/// </summary>
public class ContentTypeReader : IDisposable
{
    private readonly NativeResourceHandle _handle;

    internal ContentTypeReader(nint nativeHandleValue)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, h => Native.cna_content_type_reader_destroy(new CnaHandle(h)));
    }

    /// <summary>
    /// The native handle, read out of the owning <see cref="NativeResourceHandle"/>. Every caller
    /// pairs it with <see cref="GC.KeepAlive(object)"/> after the native call: once the handle
    /// value has been read this object can be unreachable, and an unreachable
    /// <see cref="System.Runtime.InteropServices.SafeHandle"/> may have its critical finalizer run
    /// <c>destroy</c> while the call is still in flight. Defeating exactly that is what
    /// <see cref="System.Runtime.InteropServices.SafeHandle"/> is for, so reading the handle
    /// without keeping its owner alive gives the guarantee up -- see <c>plan.md</c> WP17.
    /// </summary>
    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    /// <summary>The canonical name of the type this reader produces.</summary>
    public unsafe string TargetTypeName
    {
        get
        {
            string value = NativeStringReader.Read(
                Native.cna_content_type_reader_get_target_type_name_size,
                Native.cna_content_type_reader_copy_target_type_name,
                NativeHandle,
                nameof(TargetTypeName));
            GC.KeepAlive(this);
            return value;
        }
    }

    public int TypeVersion
    {
        get
        {
            CnaResult result = Native.cna_content_type_reader_get_type_version(NativeHandle, out int value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(TypeVersion));
            return value;
        }
    }

    /// <summary>Whether this reader can populate an existing instance rather than always
    /// allocating -- what lets XNA reload an asset in place.</summary>
    public bool CanDeserializeIntoExistingObject
    {
        get
        {
            CnaResult result = Native.cna_content_type_reader_get_can_deserialize_into_existing_object(NativeHandle, out byte value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(CanDeserializeIntoExistingObject));
            return value != 0;
        }
    }

    public bool SupportsVersion(int serializedVersion)
    {
        CnaResult result = Native.cna_content_type_reader_supports_version(NativeHandle, serializedVersion, out byte value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(SupportsVersion));
        return value != 0;
    }

    public void Initialize()
    {
        CnaResult result = Native.cna_content_type_reader_initialize(NativeHandle);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Initialize));
    }

    /// <summary>Reads one value, reporting only whether there was one. The value itself stays
    /// native-side: the C API's route is <c>read_untyped</c>, which deserializes into the engine's
    /// own object graph and has no way to hand a managed object back. A managed-typed read needs
    /// the reader-registration machinery this type's own doc comment describes.</summary>
    public bool ReadUntyped(ContentReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        CnaResult result = Native.cna_content_type_reader_read_untyped(NativeHandle, reader.NativeHandle, out byte hasValue);
        GC.KeepAlive(reader);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(ReadUntyped));
        return hasValue != 0;
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
