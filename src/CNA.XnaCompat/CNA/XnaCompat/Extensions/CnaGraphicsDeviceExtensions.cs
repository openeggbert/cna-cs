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
}
