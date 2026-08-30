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
/// exactly (<c>graphics_device.h:897-908</c>). All five are used.
///
/// <see cref="RawStream"/> was previously described as unreachable, "would need a caller-supplied
/// <c>CNA_VertexDeclarationHandle</c> this project has no native-backed declaration resource for".
/// That was wrong twice over, and a header audit found it: <c>vertex_resources.h:110-135</c>
/// creates real owned declarations, this repository already bound and used
/// <c>cna_vertex_declaration_create_with_stride</c> for <c>VertexBuffer</c>, and
/// <c>graphics_device.h:928-933</c> says a raw stream with no declaration at all falls back to the
/// implicit <c>VertexPositionColor</c> layout. <c>DrawUserPrimitives&lt;T&gt;</c> was throwing
/// <c>NotSupportedException</c> for a limit that did not exist.</summary>
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
    /// <summary>C calls this <c>CNA_PrimitiveType</c>, a <c>uint32_t</c>. It was declared
    /// <c>int</c> until B2; the widths agree, so the layout probe could not see it.</summary>
    public uint PrimitiveType;
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

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_UserIndices</c> exactly
/// (<c>graphics_device.h:954-969</c>) -- the index-array counterpart of
/// <see cref="CnaUserPrimitives"/>, passed alongside it to the indexed user-primitive draw.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CnaUserIndices
{
    public uint StructSize;
    public uint StructVersion;
    public uint IndexElementSize;
    public int IndexOffset;
    public void* IndexData;

    public CnaUserIndices()
    {
        StructSize = (uint)sizeof(CnaUserIndices);
        StructVersion = 1;
    }
}

/// <summary>Capacity of each of a device's two texture collections, matching
/// <c>CNA_TEXTURE_COLLECTION_MAX_TEXTURES</c> (<c>graphics_device.h:584</c>). Deliberately a
/// separate constant from <see cref="CnaSamplerState.MaxSamplers"/> even though both are 16
/// today: the C API states them independently, so this project does too rather than assuming
/// they must move together.</summary>
internal static class CnaTextureCollectionLimits
{
    public const int MaxTextures = 16;
}

/// <summary>
/// Mirrors <c>CNA_BackBufferReadback</c> exactly (<c>graphics_device.h:663</c>): the optional
/// source window for <c>cna_graphics_device_get_backbuffer_data_window</c>. Caller-initialised and
/// versioned, so it self-populates its header.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaBackBufferReadback
{
    public uint StructSize;
    public uint StructVersion;
    public byte HasSourceRectangle;
    public byte Reserved0;
    public byte Reserved1;
    public byte Reserved2;
    public CnaRect SourceRectangle;
    public ulong StartIndex;
    public ulong ElementCount;

    public unsafe CnaBackBufferReadback()
    {
        StructSize = (uint)sizeof(CnaBackBufferReadback);
        StructVersion = 1;
    }
}
