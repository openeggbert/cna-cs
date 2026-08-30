using Microsoft.Xna.Framework.Graphics;

namespace CNA.XnaCompat.Extensions;

/// <summary>Capabilities reported by CNA renderers beyond the XNA 4.0 contract.</summary>
public enum CnaGraphicsCapability : uint
{
    ThreeD = 0,
    DepthStencilBuffer = 1,
    MultiSampleAntiAliasing = 2,
    MultipleRenderTargets = 3,
    AnisotropicFiltering = 4,
    WireFrame = 5,
    OcclusionQuery = 6,
    CustomEffects = 7,
    Texture3D = 8,
    MultiStreamVertexInput = 9,
    Instancing = 10,
    StencilBuffer = 11,
    AdditiveBlending = 12,
    CompiledEffects = 13,
    FloatRenderTargets = 14,
    HalfFloatRenderTargets = 15,
    HalfFloatTextureLinearFiltering = 16,
    ComputeShaders = 17,
    IndirectDraw = 18,
}

/// <summary>
/// CNA-specific renderer diagnostics kept outside the strict XNA-compatible namespace.
/// </summary>
public static class CnaGraphicsDeviceExtensions
{
    /// <summary>Returns whether the active CNA renderer advertises a capability.</summary>
    public static bool SupportsCnaCapability(
        this GraphicsDevice graphicsDevice,
        CnaGraphicsCapability capability)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        return graphicsDevice.SupportsCnaCapabilityCore(capability);
    }

    /// <summary>Returns the active CNA renderer's diagnostic name.</summary>
    public static string GetCnaRendererName(this GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        return graphicsDevice.CnaRendererName;
    }

    /// <summary>
    /// Whether this build of CNA has its CNAEXT engine layer -- compute shaders, storage buffers,
    /// GPU timers, render-target pools, post-process passes, PBR material binding and the rest.
    ///
    /// The whole engine-layer surface is exported by every CNA build, and the routes that need a
    /// native engine-layer object answer `NOT_SUPPORTED` when the layer is absent. That keeps the
    /// ABI one shape regardless of build options, and it means a symbol existing proves nothing
    /// about whether the capability does. Ask this first.
    ///
    /// Not a device method: the answer is a property of the linked library, not of a device, and
    /// there is no device to ask before one exists.
    /// </summary>
    public static bool IsCnaEngineLayerAvailable() =>
        CNA.Graphics.GraphicsDevice.IsCnaEngineLayerAvailable();

    /// <summary>
    /// The engine layer's revision, or zero when this build has no engine layer.
    ///
    /// A revision marker rather than an ABI compatibility promise -- CNA says so explicitly. It is
    /// worth reading because a header and a library from different builds disagree here, which is
    /// otherwise invisible.
    /// </summary>
    public static int CnaEngineLayerVersion() =>
        CNA.Graphics.GraphicsDevice.CnaEngineLayerVersion();
}
