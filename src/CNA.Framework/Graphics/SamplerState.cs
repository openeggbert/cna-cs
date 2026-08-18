using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>SamplerState</c> surface. Seeded from
/// <see cref="Native.cna_sampler_state_init"/>, the real ABI's own preset-descriptor initializer
/// (<c>graphics_state.h:382</c>) -- see <see cref="BlendState"/>'s own doc comment for why the
/// presets are native-sourced rather than hardcoded, and for why this isn't sealed.
///
/// Real XNA exposes six presets and no public default-preset member; CNA's C API has a seventh,
/// <c>CNA_SAMPLER_STATE_PRESET_DEFAULT</c>, which this type uses for its public parameterless
/// constructor (real XNA's own <c>new SamplerState()</c> likewise starts from its documented
/// defaults) rather than exposing as a static member XNA never had.
/// </summary>
public class SamplerState
{
    private CnaSamplerState _native;

    public SamplerState()
        : this(CnaSamplerStatePreset.Default)
    {
    }

    private SamplerState(CnaSamplerStatePreset preset)
    {
        CnaResult result = Native.cna_sampler_state_init(preset, out _native);
        CnaException.ThrowIfFailed(result, nameof(SamplerState));
    }

    private SamplerState(CnaSamplerState native)
    {
        _native = native;
    }

    protected SamplerState(SamplerState copyFrom)
    {
        ArgumentNullException.ThrowIfNull(copyFrom);
        _native = copyFrom._native;
    }

    public static SamplerState AnisotropicClamp { get; } = new(CnaSamplerStatePreset.AnisotropicClamp);
    public static SamplerState AnisotropicWrap { get; } = new(CnaSamplerStatePreset.AnisotropicWrap);
    public static SamplerState LinearClamp { get; } = new(CnaSamplerStatePreset.LinearClamp);
    public static SamplerState LinearWrap { get; } = new(CnaSamplerStatePreset.LinearWrap);
    public static SamplerState PointClamp { get; } = new(CnaSamplerStatePreset.PointClamp);
    public static SamplerState PointWrap { get; } = new(CnaSamplerStatePreset.PointWrap);

    public TextureAddressMode AddressU
    {
        get => (TextureAddressMode)_native.AddressU;
        set => _native.AddressU = (uint)value;
    }

    public TextureAddressMode AddressV
    {
        get => (TextureAddressMode)_native.AddressV;
        set => _native.AddressV = (uint)value;
    }

    public TextureAddressMode AddressW
    {
        get => (TextureAddressMode)_native.AddressW;
        set => _native.AddressW = (uint)value;
    }

    public TextureFilter Filter
    {
        get => (TextureFilter)_native.Filter;
        set => _native.Filter = (uint)value;
    }

    public int MaxAnisotropy
    {
        get => _native.MaxAnisotropy;
        set => _native.MaxAnisotropy = value;
    }

    public int MaxMipLevel
    {
        get => _native.MaxMipLevel;
        set => _native.MaxMipLevel = value;
    }

    public float MipMapLevelOfDetailBias
    {
        get => _native.MipMapLevelOfDetailBias;
        set => _native.MipMapLevelOfDetailBias = value;
    }

    internal CnaSamplerState ToNative() => _native;

    internal static SamplerState FromNative(CnaSamplerState native) => new(native);
}
