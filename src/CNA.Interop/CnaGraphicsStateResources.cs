using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_BlendStatePreset</c>
/// exactly (<c>graphics_state.h:163-174</c>).</summary>
internal enum CnaBlendStatePreset : uint
{
    Default = 0,
    Additive = 1,
    AlphaBlend = 2,
    NonPremultiplied = 3,
    Opaque = 4,
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own
/// <c>CNA_DepthStencilStatePreset</c> exactly (<c>graphics_state.h:176-183</c>).</summary>
internal enum CnaDepthStencilStatePreset : uint
{
    Default = 0,
    DepthRead = 1,
    None = 2,
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_RasterizerStatePreset</c>
/// exactly (<c>graphics_state.h:185-194</c>).</summary>
internal enum CnaRasterizerStatePreset : uint
{
    Default = 0,
    CullClockwise = 1,
    CullCounterClockwise = 2,
    CullNone = 3,
}

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_BlendState</c> exactly
/// (<c>graphics_state.h:223-252</c>). Unlike this project's other self-populating-constructor
/// interop structs, callers never construct one directly -- every instance comes from
/// <see cref="Native.cna_blend_state_init"/> (preset descriptor) or
/// <see cref="Native.cna_graphics_device_get_blend_state"/> (device query), both of which fill in
/// <see cref="StructSize"/>/<see cref="StructVersion"/> themselves as part of "a complete
/// version-one descriptor" (<c>graphics_state.h:346</c>).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaBlendState
{
    public uint StructSize;
    public uint StructVersion;
    public uint AlphaBlendFunction;
    public uint AlphaDestinationBlend;
    public uint AlphaSourceBlend;
    public uint ColorBlendFunction;
    public uint ColorDestinationBlend;
    public uint ColorSourceBlend;
    public uint ColorWriteChannels;
    public uint ColorWriteChannels1;
    public uint ColorWriteChannels2;
    public uint ColorWriteChannels3;
    public CnaColor BlendFactor;
    public int MultiSampleMask;
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_DepthStencilState</c>
/// exactly (<c>graphics_state.h:255-294</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaDepthStencilState
{
    public uint StructSize;
    public uint StructVersion;
    public byte DepthBufferEnable;
    public byte DepthBufferWriteEnable;
    public byte StencilEnable;
    public byte TwoSidedStencilMode;
    public uint DepthBufferFunction;
    public uint StencilFunction;
    public int StencilMask;
    public int StencilWriteMask;
    public int ReferenceStencil;
    public uint StencilFail;
    public uint StencilDepthBufferFail;
    public uint StencilPass;
    public uint CounterClockwiseStencilFunction;
    public uint CounterClockwiseStencilFail;
    public uint CounterClockwiseStencilDepthBufferFail;
    public uint CounterClockwiseStencilPass;
    public uint Reserved;
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_SamplerStatePreset</c>
/// exactly (<c>graphics_state.h:196-211</c>).</summary>
internal enum CnaSamplerStatePreset : uint
{
    Default = 0,
    AnisotropicClamp = 1,
    AnisotropicWrap = 2,
    LinearClamp = 3,
    LinearWrap = 4,
    PointClamp = 5,
    PointWrap = 6,
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_ShaderStage</c> exactly
/// (<c>graphics_state.h:213-218</c>) -- selects which of a device's two sampler collections
/// <see cref="Native.cna_graphics_device_get_sampler_state"/> addresses.</summary>
internal enum CnaShaderStage : uint
{
    Pixel = 0,
    Vertex = 1,
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_SamplerState</c> exactly
/// (<c>graphics_state.h:319-340</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaSamplerState
{
    /// <summary>Matches <c>CNA_MAX_SAMPLERS</c> (<c>graphics_state.h:220</c>) -- the entry count of
    /// each of a device's sampler collections.</summary>
    public const int MaxSamplers = 16;

    public uint StructSize;
    public uint StructVersion;
    public uint AddressU;
    public uint AddressV;
    public uint AddressW;
    public uint Filter;
    public int MaxAnisotropy;
    public int MaxMipLevel;
    public float MipMapLevelOfDetailBias;
    public uint Reserved;
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_RasterizerState</c>
/// exactly (<c>graphics_state.h:297-316</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaRasterizerState
{
    public uint StructSize;
    public uint StructVersion;
    public uint CullMode;
    public uint FillMode;
    public float DepthBias;
    public float SlopeScaleDepthBias;
    public byte MultiSampleAntiAlias;
    public byte ScissorTestEnable;
    public ushort ReservedTail;
}
