namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>RenderTargetCube</c>. Derives from *this namespace's*
/// <see cref="TextureCube"/> (not <see cref="CNA.Graphics.RenderTargetCube"/>), so real XNA's own
/// <c>RenderTargetCube : TextureCube</c> ancestry holds here and
/// <c>TextureCube t = someRenderTargetCube;</c> compiles in game code -- the identical fork
/// <see cref="RenderTarget2D"/> already documents, resolved the same way: by reusing the base
/// assembly's <c>internal static</c> native helpers rather than duplicating any logic.
/// </summary>
public class RenderTargetCube : TextureCube
{
    public RenderTargetCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat)
        : this(graphicsDevice, size, mipMap, preferredFormat, preferredDepthFormat, 0, RenderTargetUsage.DiscardContents)
    {
    }

    public RenderTargetCube(
        GraphicsDevice graphicsDevice,
        int size,
        bool mipMap,
        SurfaceFormat preferredFormat,
        DepthFormat preferredDepthFormat,
        int preferredMultiSampleCount,
        RenderTargetUsage usage)
        : base(graphicsDevice, CNA.Graphics.RenderTargetCube.CreateNativeCubeHandle(
            graphicsDevice,
            size,
            mipMap,
            (CNA.Graphics.SurfaceFormat)(int)preferredFormat,
            (CNA.Graphics.DepthFormat)(int)preferredDepthFormat,
            preferredMultiSampleCount,
            (CNA.Graphics.RenderTargetUsage)(int)usage))
    {
    }

    protected override void ReleaseNative(nint handleValue) =>
        CNA.Graphics.RenderTarget2D.ReleaseNativeRenderTarget(handleValue);
}
