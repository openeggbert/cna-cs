using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_Viewport</c> exactly
/// (<c>graphics_device.h:59-77</c>) -- a plain fixed struct with no
/// <c>struct_size</c>/<c>struct_version</c> header, unlike this project's other interop
/// structs.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaViewport
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public float MinDepth;
    public float MaxDepth;
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_GraphicsProfile</c>
/// exactly (<c>display.h:21-25</c>).</summary>
internal enum CnaGraphicsProfile : uint
{
    Reach = 0,
    HiDef = 1,
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_UserVertexSource</c>
/// exactly (<c>graphics_device.h:897-908</c>). Only the four typed sources are used here --
/// <see cref="RawStream"/> would need a caller-supplied <c>CNA_VertexDeclarationHandle</c> this
/// project has no native-backed declaration resource for (this project's
/// <c>CNA.Graphics.VertexDeclaration</c> is a client-side-only description, never uploaded to
/// native as its own resource).</summary>
internal enum CnaUserVertexSource : uint
{
    RawStream = 0,
    PositionColor = 1,
    PositionColorTexture = 2,
    PositionTexture = 3,
    PositionNormalTexture = 4,
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_UserPrimitives</c>
/// exactly (<c>graphics_device.h:913-949</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CnaUserPrimitives
{
    public uint StructSize;
    public uint StructVersion;
    public int PrimitiveType;
    public CnaUserVertexSource VertexSource;
    public void* VertexData;
    public CnaHandle VertexDeclaration;
    public int VertexOffset;
    public int NumVertices;
    public int PrimitiveCount;
    public uint Reserved;

    public CnaUserPrimitives()
    {
        StructSize = (uint)sizeof(CnaUserPrimitives);
        StructVersion = 1;
    }
}
