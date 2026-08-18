using CNA.Interop;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>RasterizerState</c> surface. See <see cref="BlendState"/>'s own
/// doc comment for why the static presets are seeded from
/// <see cref="Native.cna_rasterizer_state_init"/> rather than hardcoded by hand.</summary>
public sealed class RasterizerState
{
    private CnaRasterizerState _native;

    public RasterizerState()
        : this(CnaRasterizerStatePreset.Default)
    {
    }

    private RasterizerState(CnaRasterizerStatePreset preset)
    {
        CnaResult result = Native.cna_rasterizer_state_init(preset, out _native);
        CnaException.ThrowIfFailed(result, nameof(RasterizerState));
    }

    private RasterizerState(CnaRasterizerState native)
    {
        _native = native;
    }

    public static RasterizerState CullClockwise { get; } = new(CnaRasterizerStatePreset.CullClockwise);
    public static RasterizerState CullCounterClockwise { get; } = new(CnaRasterizerStatePreset.CullCounterClockwise);
    public static RasterizerState CullNone { get; } = new(CnaRasterizerStatePreset.CullNone);

    public CullMode CullMode
    {
        get => (CullMode)_native.CullMode;
        set => _native.CullMode = (uint)value;
    }

    public FillMode FillMode
    {
        get => (FillMode)_native.FillMode;
        set => _native.FillMode = (uint)value;
    }

    public float DepthBias
    {
        get => _native.DepthBias;
        set => _native.DepthBias = value;
    }

    public float SlopeScaleDepthBias
    {
        get => _native.SlopeScaleDepthBias;
        set => _native.SlopeScaleDepthBias = value;
    }

    public bool MultiSampleAntiAlias
    {
        get => _native.MultiSampleAntiAlias != 0;
        set => _native.MultiSampleAntiAlias = (byte)(value ? 1 : 0);
    }

    public bool ScissorTestEnable
    {
        get => _native.ScissorTestEnable != 0;
        set => _native.ScissorTestEnable = (byte)(value ? 1 : 0);
    }

    internal CnaRasterizerState ToNative() => _native;

    internal static RasterizerState FromNative(CnaRasterizerState native) => new(native);
}
