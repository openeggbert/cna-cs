using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>Exact managed mirror of <c>CNA_ContentManagerCreateInfo</c> from
/// <c>CNA/C/content.h</c>. The string view is borrowed only for the duration of the create call.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CnaContentManagerCreateInfo
{
    public uint StructSize;
    public uint StructVersion;
    public CnaStringView RootDirectory;
    public ulong Reserved;

    public CnaContentManagerCreateInfo(CnaStringView rootDirectory)
    {
        StructSize = (uint)sizeof(CnaContentManagerCreateInfo);
        StructVersion = 1;
        RootDirectory = rootDirectory;
        Reserved = 0;
    }
}
