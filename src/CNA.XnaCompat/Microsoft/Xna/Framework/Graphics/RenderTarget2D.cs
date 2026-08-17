namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>RenderTarget2D</c>. Inherits from this namespace's own <c>Texture2D</c>
/// (not <c>CNA.Graphics.RenderTarget2D</c>) so <c>Texture2D t = someRenderTarget;</c> compiles in
/// game code, matching real XNA's <c>RenderTarget2D : Texture2D</c>. Creation still goes through
/// <c>CNA.Graphics.RenderTarget2D.CreateNativeHandle</c> -- see that method's doc comment for why
/// it exists and why this doesn't just subclass it directly.
///
/// Because this class's base is CNA.XnaCompat's own <c>Texture2D</c> rather than
/// <c>CNA.Graphics.RenderTarget2D</c>, it does not inherit that type's own
/// <c>ReleaseNative</c>/<c>Width</c>/<c>Height</c> overrides -- without overriding them here too,
/// this class would silently call the plain-texture natives on a handle that
/// <c>cna_render_target2d_create</c> actually created, exactly the bug this migration's step 5
/// fixed on the CNA.Framework side. Reuses <c>CNA.Graphics.RenderTarget2D</c>'s own
/// <c>internal static</c> helpers rather than duplicating the native calls.
/// </summary>
public class RenderTarget2D : Texture2D
{
    public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height)
        : base(CNA.Graphics.RenderTarget2D.CreateNativeHandle(graphicsDevice, width, height))
    {
    }

    protected override void ReleaseNative(nint handleValue) => CNA.Graphics.RenderTarget2D.ReleaseNativeRenderTarget(handleValue);

    public override int Width => CNA.Graphics.RenderTarget2D.GetDimensions(NativeHandleValue).Width;

    public override int Height => CNA.Graphics.RenderTarget2D.GetDimensions(NativeHandleValue).Height;
}
