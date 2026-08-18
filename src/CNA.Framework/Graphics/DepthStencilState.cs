using CNA.Interop;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>DepthStencilState</c> surface. See <see cref="BlendState"/>'s
/// own doc comment for why the static presets are seeded from
/// <see cref="Native.cna_depth_stencil_state_init"/> rather than hardcoded by hand.</summary>
public sealed class DepthStencilState
{
    private CnaDepthStencilState _native;

    public DepthStencilState()
        : this(CnaDepthStencilStatePreset.Default)
    {
    }

    private DepthStencilState(CnaDepthStencilStatePreset preset)
    {
        CnaResult result = Native.cna_depth_stencil_state_init(preset, out _native);
        CnaException.ThrowIfFailed(result, nameof(DepthStencilState));
    }

    private DepthStencilState(CnaDepthStencilState native)
    {
        _native = native;
    }

    public static DepthStencilState Default { get; } = new(CnaDepthStencilStatePreset.Default);
    public static DepthStencilState DepthRead { get; } = new(CnaDepthStencilStatePreset.DepthRead);
    public static DepthStencilState None { get; } = new(CnaDepthStencilStatePreset.None);

    public bool DepthBufferEnable
    {
        get => _native.DepthBufferEnable != 0;
        set => _native.DepthBufferEnable = (byte)(value ? 1 : 0);
    }

    public bool DepthBufferWriteEnable
    {
        get => _native.DepthBufferWriteEnable != 0;
        set => _native.DepthBufferWriteEnable = (byte)(value ? 1 : 0);
    }

    public bool StencilEnable
    {
        get => _native.StencilEnable != 0;
        set => _native.StencilEnable = (byte)(value ? 1 : 0);
    }

    public bool TwoSidedStencilMode
    {
        get => _native.TwoSidedStencilMode != 0;
        set => _native.TwoSidedStencilMode = (byte)(value ? 1 : 0);
    }

    public CompareFunction DepthBufferFunction
    {
        get => (CompareFunction)_native.DepthBufferFunction;
        set => _native.DepthBufferFunction = (uint)value;
    }

    public CompareFunction StencilFunction
    {
        get => (CompareFunction)_native.StencilFunction;
        set => _native.StencilFunction = (uint)value;
    }

    public int StencilMask
    {
        get => _native.StencilMask;
        set => _native.StencilMask = value;
    }

    public int StencilWriteMask
    {
        get => _native.StencilWriteMask;
        set => _native.StencilWriteMask = value;
    }

    public int ReferenceStencil
    {
        get => _native.ReferenceStencil;
        set => _native.ReferenceStencil = value;
    }

    public StencilOperation StencilFail
    {
        get => (StencilOperation)_native.StencilFail;
        set => _native.StencilFail = (uint)value;
    }

    public StencilOperation StencilDepthBufferFail
    {
        get => (StencilOperation)_native.StencilDepthBufferFail;
        set => _native.StencilDepthBufferFail = (uint)value;
    }

    public StencilOperation StencilPass
    {
        get => (StencilOperation)_native.StencilPass;
        set => _native.StencilPass = (uint)value;
    }

    public CompareFunction CounterClockwiseStencilFunction
    {
        get => (CompareFunction)_native.CounterClockwiseStencilFunction;
        set => _native.CounterClockwiseStencilFunction = (uint)value;
    }

    public StencilOperation CounterClockwiseStencilFail
    {
        get => (StencilOperation)_native.CounterClockwiseStencilFail;
        set => _native.CounterClockwiseStencilFail = (uint)value;
    }

    public StencilOperation CounterClockwiseStencilDepthBufferFail
    {
        get => (StencilOperation)_native.CounterClockwiseStencilDepthBufferFail;
        set => _native.CounterClockwiseStencilDepthBufferFail = (uint)value;
    }

    public StencilOperation CounterClockwiseStencilPass
    {
        get => (StencilOperation)_native.CounterClockwiseStencilPass;
        set => _native.CounterClockwiseStencilPass = (uint)value;
    }

    internal CnaDepthStencilState ToNative() => _native;

    internal static DepthStencilState FromNative(CnaDepthStencilState native) => new(native);
}
