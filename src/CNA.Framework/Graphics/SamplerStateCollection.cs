using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>SamplerStateCollection</c>: the indexer view over one of a device's two
/// sampler collections (<c>GraphicsDevice.SamplerStates</c> for the pixel shader,
/// <c>VertexSamplerStates</c> for the vertex shader).
///
/// Deliberately stateless -- every read and write goes straight to
/// <c>cna_graphics_device_get/set_sampler_state</c> rather than caching a
/// <see cref="SamplerState"/> per slot. That differs from how
/// <see cref="GraphicsDevice.BlendState"/> and friends cache their single current value, and the
/// reason is the same one that made those three need a query hook at all: a cache is only
/// correct while nothing else can change the underlying state, and 32 slots across two stages is
/// a lot of surface to keep honest for no benefit -- these are not per-frame hot-path reads.
/// </summary>
public class SamplerStateCollection
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly CnaShaderStage _stage;

    protected internal SamplerStateCollection(GraphicsDevice graphicsDevice, bool vertexStage)
    {
        _graphicsDevice = graphicsDevice;
        _stage = vertexStage ? CnaShaderStage.Vertex : CnaShaderStage.Pixel;
    }

    /// <summary>Matches <c>CNA_MAX_SAMPLERS</c> (<c>graphics_state.h:220</c>). Real XNA's own
    /// collection has no public <c>Count</c>; this one does, because the limit is a documented,
    /// fixed part of the ABI and silently throwing on slot 16 with no way to ask is worse.</summary>
    public int Count => CnaSamplerState.MaxSamplers;

    public SamplerState this[int index]
    {
        get
        {
            ValidateSlot(index);
            CnaResult result = Native.cna_graphics_device_get_sampler_state(
                _graphicsDevice.ResolveNativeDeviceHandle(), _stage, (uint)index, out CnaSamplerState native);
            CnaException.ThrowIfFailed(result, nameof(SamplerStateCollection));
            return Wrap(SamplerState.FromNative(native));
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            ValidateSlot(index);
            CnaResult result = Native.cna_graphics_device_set_sampler_state(
                _graphicsDevice.ResolveNativeDeviceHandle(), _stage, (uint)index, value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(SamplerStateCollection));
        }
    }

    /// <summary>Overridden by CNA.XnaCompat's own collection so its indexer can hand back a
    /// compat-typed <c>SamplerState</c> -- same reason
    /// <see cref="GraphicsDevice.QueryBlendState"/> exists. Takes and returns the *public*
    /// <see cref="SamplerState"/> rather than the interop struct the caller above just decoded:
    /// a <see langword="protected"/> member may not expose an <see langword="internal"/> type to a
    /// subclass in another assembly (a real <c>CS0050</c>, and design invariant #5 besides -- see
    /// <c>ContentManager</c>'s own note on the same constraint), so the base builds the base-typed
    /// instance first and the override copies it.</summary>
    protected virtual SamplerState Wrap(SamplerState state) => state;

    private static void ValidateSlot(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, CnaSamplerState.MaxSamplers);
    }
}
