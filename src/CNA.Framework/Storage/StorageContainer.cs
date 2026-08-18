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
    private readonly NativeResourceHandle _handle;

    internal StorageContainer(nint nativeHandleValue, StorageDevice storageDevice)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, h => Native.cna_storage_container_destroy(new CnaHandle(h)));
        StorageDevice = storageDevice;
    }

    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public StorageDevice StorageDevice { get; }

    public unsafe string DisplayName => NativeStringReader.Read(
        Native.cna_storage_container_get_display_name_size,
        Native.cna_storage_container_copy_display_name,
        NativeHandle,
        nameof(DisplayName));

    public bool IsDisposed
    {
        get
        {
            CnaResult result = Native.cna_storage_container_get_is_disposed(NativeHandle, out byte value);
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
        CnaResult result = Native.cna_storage_container_dispose(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Dispose));
        _handle.Dispose();
        GC.SuppressFinalize(this);
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
        CnaException.ThrowIfFailed(result, context);
    }

    private bool WithPathQuery(PathQueryFunc call, string path, string context)
    {
        ArgumentNullException.ThrowIfNull(path);

        byte exists = 0;
        CnaResult result = CnaStringMarshal.WithStringView(path, view => call(NativeHandle, view, out exists));
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
                CnaException.ThrowIfFailed(copyResult, context);
            }

            names[i] = System.Text.Encoding.UTF8.GetString(buffer, 0, (int)written);
        }

        return names;
    }
}
