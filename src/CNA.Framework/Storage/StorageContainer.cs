using CNA.Interop;

namespace CNA.Storage;

/// <summary>
/// Matches real XNA's <c>StorageContainer</c>: a named, isolated folder on a
/// <see cref="StorageDevice"/> holding one title's saved data.
///
/// File access returns real <see cref="Stream"/>s, exactly as in XNA -- see
/// <see cref="StorageStream"/> for why that is a BCL type rather than a CNA one.
/// </summary>
public class StorageContainer : IDisposable
{
    // Owned, with a strong parent edge. Opening transfers one container handle to this wrapper;
    // StorageDevice must remain alive for the container's full XNA-visible lifetime.
    private readonly NativeResourceHandle _handle;

    internal StorageContainer(nint nativeHandleValue, StorageDevice storageDevice)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, h => Native.cna_storage_container_destroy(new CnaHandle(h)).IsSuccess());
        StorageDevice = storageDevice;
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

    public StorageDevice StorageDevice { get; }

    public unsafe string DisplayName
    {
        get
        {
            string value = NativeStringReader.Read(
                Native.cna_storage_container_get_display_name_size,
                Native.cna_storage_container_copy_display_name,
                NativeHandle,
                nameof(DisplayName));
            GC.KeepAlive(this);
            return value;
        }
    }

    public bool IsDisposed
    {
        get
        {
            CnaResult result = Native.cna_storage_container_get_is_disposed(NativeHandle, out byte value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(IsDisposed));
            return value != 0;
        }
    }

    public void CreateDirectory(string directory) => WithPath(Native.cna_storage_container_create_directory, directory, nameof(CreateDirectory));

    public void DeleteDirectory(string directory) => WithPath(Native.cna_storage_container_delete_directory, directory, nameof(DeleteDirectory));

    public bool DirectoryExists(string directory) =>
        WithPathQuery(Native.cna_storage_container_directory_exists, directory, nameof(DirectoryExists));

    public void DeleteFile(string file) => WithPath(Native.cna_storage_container_delete_file, file, nameof(DeleteFile));

    public bool FileExists(string file) => WithPathQuery(Native.cna_storage_container_file_exists, file, nameof(FileExists));

    /// <summary>Matches real XNA's parameterless <c>GetFileNames()</c>, which lists everything.
    /// Passes an empty pattern, which <c>storage.h:459</c> documents as the "every name" sentinel
    /// ("a zero-length view for every name") -- not <c>"*"</c>, which would be run through the
    /// native glob and, in most glob implementations, skip dot-prefixed names. A code-review pass
    /// caught that.</summary>
    public string[] GetFileNames() => GetFileNames(string.Empty);

    public unsafe string[] GetFileNames(string searchPattern) => CopyNames(
        Native.cna_storage_container_get_file_name_count,
        Native.cna_storage_container_copy_file_name,
        searchPattern,
        nameof(GetFileNames));

    /// <summary>Empty pattern means "every name" -- see <see cref="GetFileNames()"/>.</summary>
    public string[] GetDirectoryNames() => GetDirectoryNames(string.Empty);

    public unsafe string[] GetDirectoryNames(string searchPattern) => CopyNames(
        Native.cna_storage_container_get_directory_name_count,
        Native.cna_storage_container_copy_directory_name,
        searchPattern,
        nameof(GetDirectoryNames));

    public Stream CreateFile(string file)
    {
        ArgumentNullException.ThrowIfNull(file);

        CnaHandle stream = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            file, view => Native.cna_storage_container_create_file(NativeHandle, view, out stream));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(CreateFile));
        return new StorageStream(stream.AsNint);
    }

    /// <summary>Takes <see cref="System.IO.FileMode"/> directly: <c>CNA_FileMode</c>'s values match
    /// it one-to-one, so a CNA-flavoured duplicate would violate design invariant #7 for no
    /// gain.</summary>
    public Stream OpenFile(string file, FileMode fileMode)
    {
        ArgumentNullException.ThrowIfNull(file);

        CnaHandle stream = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            file, view => Native.cna_storage_container_open_file(NativeHandle, view, (uint)fileMode, out stream));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(OpenFile));
        return new StorageStream(stream.AsNint);
    }

    /// <summary>Matches real XNA's <c>OpenFile(string, FileMode, FileAccess)</c>. Sharing defaults
    /// to read/write, which is what the ABI's own three-argument route does.</summary>
    public Stream OpenFile(string file, FileMode fileMode, FileAccess fileAccess)
    {
        ArgumentNullException.ThrowIfNull(file);

        CnaHandle stream = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            file,
            view => Native.cna_storage_container_open_file_access(
                NativeHandle, view, (uint)fileMode, (uint)fileAccess, out stream));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(OpenFile));
        return new StorageStream(stream.AsNint);
    }

    /// <summary>Matches real XNA's <c>OpenFile(string, FileMode, FileAccess, FileShare)</c>. All
    /// three enums are <see cref="System.IO"/>'s own -- <c>CNA_FileMode</c>/<c>CNA_FileAccess</c>/
    /// <c>CNA_FileShare</c> match them value for value, so a CNA-flavoured duplicate would violate
    /// design invariant #7 for no gain.</summary>
    public Stream OpenFile(string file, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
    {
        ArgumentNullException.ThrowIfNull(file);

        CnaHandle stream = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            file,
            view => Native.cna_storage_container_open_file_share(
                NativeHandle, view, (uint)fileMode, (uint)fileAccess, (uint)fileShare, out stream));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(OpenFile));
        return new StorageStream(stream.AsNint);
    }

    private bool _disposed;

    /// <summary>Guarded: <see cref="System.Runtime.InteropServices.SafeHandle.DangerousGetHandle"/>
    /// keeps returning the stale value after close, so an unguarded second call would pass a
    /// released handle to <c>cna_storage_container_dispose</c>. Found by a code-review
    /// pass.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Exception? pending = null;
        bool nativeDisposed = false;
        try
        {
            CnaResult result = Native.cna_storage_container_dispose(NativeHandle);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Dispose));
            nativeDisposed = true;
        }
        catch (Exception exception)
        {
            pending = exception;
        }

        // ABI 0.6.0 documents a synchronous native callback here, but the measured implementation
        // emits none. This wrapper is the exclusive explicit-disposal route, so deliver the known
        // sender once in managed code after native has accepted disposal. That also guarantees a
        // handler exception never crosses an unmanaged frame.
        if (nativeDisposed)
        {
            try
            {
                _disposingHandler?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception exception)
            {
                pending ??= exception;
            }
        }

        _disposingHandler = null;
        _handle.Dispose();
        GC.SuppressFinalize(this);

        if (pending is not null)
        {
            throw pending;
        }
    }

    private delegate CnaResult PathFunc(CnaHandle container, CnaStringView path);

    private delegate CnaResult PathQueryFunc(CnaHandle container, CnaStringView path, out byte outResult);

    private delegate CnaResult NameCountFunc(CnaHandle container, CnaStringView searchPattern, out ulong outCount);

    private unsafe delegate CnaResult NameCopyFunc(
        CnaHandle container, CnaStringView searchPattern, ulong index, byte* destination, ulong capacity, out ulong outBytes);

    private void WithPath(PathFunc call, string path, string context)
    {
        ArgumentNullException.ThrowIfNull(path);
        CnaResult result = CnaStringMarshal.WithStringView(path, view => call(NativeHandle, view));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, context);
    }

    private bool WithPathQuery(PathQueryFunc call, string path, string context)
    {
        ArgumentNullException.ThrowIfNull(path);

        byte exists = 0;
        CnaResult result = CnaStringMarshal.WithStringView(path, view => call(NativeHandle, view, out exists));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, context);
        return exists != 0;
    }

    /// <summary>The name-listing calls take an index rather than filling an array, so the count is
    /// read once and each name copied individually -- one size-then-copy round per entry, which is
    /// the shape the C API offers.</summary>
    private unsafe string[] CopyNames(NameCountFunc countCall, NameCopyFunc copyCall, string searchPattern, string context)
    {
        ArgumentNullException.ThrowIfNull(searchPattern);

        ulong count = 0;
        CnaResult countResult = CnaStringMarshal.WithStringView(
            searchPattern, view => countCall(NativeHandle, view, out count));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(countResult, context);

        var names = new string[count];
        for (ulong i = 0; i < count; i++)
        {
            ulong index = i;
            ulong needed = 0;

            // Ask for the size with a zero-capacity call, the same two-call pattern the rest of
            // this ABI uses -- there is no separate size function for an indexed name.
            CnaResult sizeResult = CnaStringMarshal.WithStringView(
                searchPattern, view => copyCall(NativeHandle, view, index, null, 0, out needed));
            GC.KeepAlive(this);

            if (sizeResult.IsFailure() && sizeResult != CnaResult.BufferTooSmall)
            {
                CnaException.ThrowIfFailed(sizeResult, context);
            }

            byte[] buffer = new byte[needed];
            ulong written = 0;
            fixed (byte* bufferPtr = buffer)
            {
                byte* ptr = bufferPtr;
                CnaResult copyResult = CnaStringMarshal.WithStringView(
                    searchPattern, view => copyCall(NativeHandle, view, index, ptr, needed, out written));
                GC.KeepAlive(this);
                CnaException.ThrowIfFailed(copyResult, context);
            }

            names[i] = System.Text.Encoding.UTF8.GetString(buffer, 0, (int)written);
        }

        return names;
    }

    /// <summary>Raised once after native accepts explicit disposal, matching real XNA. ABI 0.6.0's
    /// documented native callback is observably silent, so this event is kept managed rather than
    /// pinning a native registration that never fires; see <c>docs/native-behavior-blockers.md</c>.
    /// </summary>
    public event EventHandler<EventArgs>? Disposing
    {
        add => _disposingHandler += value;
        remove => _disposingHandler -= value;
    }

    private EventHandler<EventArgs>? _disposingHandler;
}
