using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>BlendState</c> surface. Every instance -- including the four static
/// presets below -- is seeded by <see cref="Native.cna_blend_state_init"/>, the real ABI's own
/// preset-descriptor initializer (<c>graphics_state.h:349</c>), rather than by hardcoding XNA's
/// well-known field values by hand: the native call takes no device handle at all, so it is safe
/// to run at static-field-init time, before any game or graphics device exists.
/// </summary>
public sealed class BlendState
{
    private CnaBlendState _native;

    public BlendState()
        : this(CnaBlendStatePreset.Default)
    {
    }

    private BlendState(CnaBlendStatePreset preset)
    {
        CnaResult result = Native.cna_blend_state_init(preset, out _native);
        CnaException.ThrowIfFailed(result, nameof(BlendState));
    }

    private BlendState(CnaBlendState native)
    {
        _native = native;
    }

    public static BlendState Opaque { get; } = new(CnaBlendStatePreset.Opaque);
    public static BlendState AlphaBlend { get; } = new(CnaBlendStatePreset.AlphaBlend);
    public static BlendState Additive { get; } = new(CnaBlendStatePreset.Additive);
    public static BlendState NonPremultiplied { get; } = new(CnaBlendStatePreset.NonPremultiplied);

    public BlendFunction AlphaBlendFunction
    {
        get => (BlendFunction)_native.AlphaBlendFunction;
        set => _native.AlphaBlendFunction = (uint)value;
    }

    public Blend AlphaDestinationBlend
    {
        get => (Blend)_native.AlphaDestinationBlend;
        set => _native.AlphaDestinationBlend = (uint)value;
    }

    public Blend AlphaSourceBlend
    {
        get => (Blend)_native.AlphaSourceBlend;
        set => _native.AlphaSourceBlend = (uint)value;
    }

    public BlendFunction ColorBlendFunction
    {
        get => (BlendFunction)_native.ColorBlendFunction;
        set => _native.ColorBlendFunction = (uint)value;
    }

    public Blend ColorDestinationBlend
    {
        get => (Blend)_native.ColorDestinationBlend;
        set => _native.ColorDestinationBlend = (uint)value;
    }

    public Blend ColorSourceBlend
    {
        get => (Blend)_native.ColorSourceBlend;
        set => _native.ColorSourceBlend = (uint)value;
    }

    public ColorWriteChannels ColorWriteChannels
    {
        get => (ColorWriteChannels)_native.ColorWriteChannels;
        set => _native.ColorWriteChannels = (uint)value;
    }

    public ColorWriteChannels ColorWriteChannels1
    {
        get => (ColorWriteChannels)_native.ColorWriteChannels1;
        set => _native.ColorWriteChannels1 = (uint)value;
    }

    public ColorWriteChannels ColorWriteChannels2
    {
        get => (ColorWriteChannels)_native.ColorWriteChannels2;
        set => _native.ColorWriteChannels2 = (uint)value;
    }

    public ColorWriteChannels ColorWriteChannels3
    {
        get => (ColorWriteChannels)_native.ColorWriteChannels3;
        set => _native.ColorWriteChannels3 = (uint)value;
    }

    public Color BlendFactor
    {
        get => Color.FromNative(_native.BlendFactor);
        set => _native.BlendFactor = value.ToNative();
    }

    public int MultiSampleMask
    {
        get => _native.MultiSampleMask;
        set => _native.MultiSampleMask = value;
    }

    internal CnaBlendState ToNative() => _native;

    internal static BlendState FromNative(CnaBlendState native) => new(native);
}
