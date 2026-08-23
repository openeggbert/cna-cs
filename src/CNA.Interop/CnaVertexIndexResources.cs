using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_VertexElement</c> exactly
/// (<c>graphics3d.h:105-114</c>) -- field order (offset, format, usage, usage_index) already
/// matched <c>CNA.Graphics.VertexElement</c>'s own field order exactly before this migration, and
/// both <c>CNA.Graphics.VertexElementFormat</c>/<c>VertexElementUsage</c>'s numeric values already
/// matched the real <c>CNA_VERTEX_ELEMENT_FORMAT_*</c>/<c>CNA_VERTEX_ELEMENT_USAGE_*</c> constants
/// exactly -- confirmed, not just assumed, before relying on a direct field-for-field conversion.
/// No <c>struct_size</c>/<c>struct_version</c> header -- unlike the versioned structs elsewhere in
/// this migration, this is a fixed-layout array element (the real ABI's own binding-descriptor
/// structs, e.g. <c>CNA_VertexBufferBinding</c>, follow the same no-header convention for the same
/// reason: a plain array element, not an extensible top-level input/output).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaVertexElement
{
    public readonly int Offset;
    public readonly uint Format;
    public readonly uint Usage;
    public readonly int UsageIndex;

    public CnaVertexElement(int offset, uint format, uint usage, int usageIndex)
    {
        Offset = offset;
        Format = format;
        Usage = usage;
        UsageIndex = usageIndex;
    }
}

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_VertexBufferCreateInfo</c> exactly
/// (<c>vertex_resources.h:37-52</c>). <see cref="VertexDeclaration"/> is a *native* declaration
/// handle the real ABI requires be built first (<see cref="Native.cna_vertex_declaration_create_with_stride"/>)
/// and is only borrowed for this call -- "declaration copied into the buffer", so
/// <c>VertexBuffer.cs</c> destroys it again immediately after this struct's call succeeds or fails,
/// rather than keeping it alive for the vertex buffer's lifetime. See
/// <see cref="CnaGameFrameHooks"/>'s own constructor doc comment for why this self-populates
/// <see cref="StructSize"/>/<see cref="StructVersion"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaVertexBufferCreateInfo
{
    public uint StructSize;
    public uint StructVersion;
    public CnaHandle VertexDeclaration;
    public int VertexCount;
    public uint BufferUsage;
    public byte Dynamic;
    public CnaReservedBytes7 Reserved;

    public unsafe CnaVertexBufferCreateInfo()
    {
        StructSize = (uint)sizeof(CnaVertexBufferCreateInfo);
        StructVersion = 1;
    }
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_IndexBufferCreateInfo</c>
/// exactly (<c>index_resources.h:34-49</c>). See <see cref="CnaVertexBufferCreateInfo"/>'s own doc
/// comment for the self-populating-constructor rationale (identical here).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaIndexBufferCreateInfo
{
    public uint StructSize;
    public uint StructVersion;
    public int IndexCount;
    public uint IndexElementSize;
    public uint BufferUsage;
    public byte Dynamic;
    public CnaReservedBytes3 Reserved;

    public unsafe CnaIndexBufferCreateInfo()
    {
        StructSize = (uint)sizeof(CnaIndexBufferCreateInfo);
        StructVersion = 1;
    }
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_IndexBufferTransfer</c>
/// exactly (<c>index_resources.h:74-87</c>) -- selects an index width, a
/// <c>CNA_SetDataOptions</c> streaming hint (the strict dynamic-buffer facade forwards all three
/// XNA values on routes the ABI can represent), and a window into the *caller's* own array
/// (<see cref="StartIndex"/>/<see cref="ElementCount"/>); confirmed directly against
/// <c>cnabinding</c> that this stays fully generic for any 2- or 4-byte unmanaged element type,
/// unlike <see cref="CnaVertexBufferCreateInfo"/>'s sibling -- CNA's own C++ <c>IndexBuffer</c> only
/// ever stores <c>uint16_t</c> or <c>uint32_t</c> elements, so a width selector is the whole
/// story, not a narrowing.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaIndexBufferTransfer
{
    public uint StructSize;
    public uint StructVersion;
    public uint IndexElementSize;
    public uint Options;
    public ulong StartIndex;
    public ulong ElementCount;

    public unsafe CnaIndexBufferTransfer()
    {
        StructSize = (uint)sizeof(CnaIndexBufferTransfer);
        StructVersion = 1;
    }
}

/// <summary>Three reserved padding bytes, matching <c>CNA_IndexBufferCreateInfo::reserved</c>
/// (<c>index_resources.h:48</c>) byte-for-byte -- see <see cref="CnaFloatBuffer256"/> for why this
/// project uses the C# 12 <c>InlineArray</c> feature for fixed-size inline buffers like this
/// one.</summary>
[InlineArray(3)]
internal struct CnaReservedBytes3
{
    private byte _element0;
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_VertexBufferBinding</c>
/// exactly (<c>vertex_resources.h:95-102</c>) -- a plain fixed struct with no
/// <c>struct_size</c>/<c>struct_version</c> header, since it only ever appears as an array element
/// whose count is passed alongside.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaVertexBufferBinding
{
    public CnaHandle VertexBuffer;
    public int VertexOffset;
    public int InstanceFrequency;
}

/// <summary>
/// Mirrors <c>CNA_VertexBufferInfo</c> exactly (<c>vertex_resources.h:55-76</c>). Caller-initialized
/// and versioned, so it self-populates its header the way the other versioned structs here do.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaVertexBufferInfo
{
    public uint StructSize;
    public uint StructVersion;
    public int VertexCount;
    public uint BufferUsage;
    public byte Dynamic;
    public byte IsContentLost;
    public byte HasRenderer;
    public byte Reserved0;
    public int VertexStride;
    public ulong VertexElementCount;

    public unsafe CnaVertexBufferInfo()
    {
        StructSize = (uint)sizeof(CnaVertexBufferInfo);
        StructVersion = 1;
    }
}

/// <summary>Mirrors <c>CNA_IndexBufferInfo</c> exactly (<c>index_resources.h:52-72</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaIndexBufferInfo
{
    public uint StructSize;
    public uint StructVersion;
    public int IndexCount;
    public uint IndexElementSize;
    public uint BufferUsage;
    public byte Dynamic;
    public byte IsContentLost;
    public byte HasRenderer;
    public byte Reserved;

    public unsafe CnaIndexBufferInfo()
    {
        StructSize = (uint)sizeof(CnaIndexBufferInfo);
        StructVersion = 1;
    }
}

/// <summary>
/// Mirrors <c>CNA_VertexType</c> exactly (<c>vertex_values.h:16-28</c>): the built-in vertex
/// layouts the typed transfer routes understand.
///
/// Three of the seven are CNAEXT (tangent and skinned variants), which real XNA has no equivalent
/// for. They are listed so the numeric identities stay correct for the four that do map.
/// </summary>
internal enum CnaVertexType : uint
{
    PositionColor = 0,
    PositionColorTexture = 1,
    PositionNormalTangentTexture = 2,
    PositionNormalTangentTextureSkinned = 3,
    PositionNormalTexture = 4,
    PositionNormalTextureSkinned = 5,
    PositionTexture = 6,
}

/// <summary>
/// Mirrors <c>CNA_VertexBufferTransfer</c> exactly (<c>vertex_resources.h:78-91</c>).
/// <see cref="StartIndex"/> is an index into the <em>caller's</em> array, not into the buffer --
/// the header is explicit that native readback always begins at vertex zero.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaVertexBufferTransfer
{
    public uint StructSize;
    public uint StructVersion;
    public CnaVertexType VertexType;
    public uint Options;
    public ulong StartIndex;
    public ulong ElementCount;

    public unsafe CnaVertexBufferTransfer()
    {
        StructSize = (uint)sizeof(CnaVertexBufferTransfer);
        StructVersion = 1;
    }
}
