using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own
/// <c>CNA_ContentReaderCreateInfo</c> exactly (<c>content_readers.h</c>). The stream is a
/// <c>CNA_StorageStreamHandle</c> -- the same handle type <c>CNA.Storage</c> wraps, which is why a
/// content reader can be built over storage-container data as well as a plain asset file.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaContentReaderCreateInfo
{
    public uint StructSize;
    public uint StructVersion;
    public CnaHandle ContentManager;
    public CnaHandle Stream;
    public CnaStringView AssetName;
    public int Version;
    public byte Platform;
    public CnaReservedBytes3 Reserved;

    public unsafe CnaContentReaderCreateInfo()
    {
        StructSize = (uint)sizeof(CnaContentReaderCreateInfo);
        StructVersion = 1;
    }
}
