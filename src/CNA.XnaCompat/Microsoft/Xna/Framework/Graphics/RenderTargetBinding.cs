namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>RenderTargetBinding</c>: a duplicated value type with a conversion to its
/// <c>CNA.Graphics</c> counterpart, the pattern this layer uses for every struct.
///
/// Landed with <see cref="GraphicsDevice.SetRenderTargets"/> in the WP16 re-audit -- both the type
/// and the multiple-render-target route it exists for were missing.
/// </summary>
public readonly struct RenderTargetBinding
{
    private readonly CNA.Graphics.RenderTargetBinding _framework;

    public RenderTargetBinding(RenderTarget2D renderTarget)
    {
        ArgumentNullException.ThrowIfNull(renderTarget);
        _framework = new CNA.Graphics.RenderTargetBinding(
            (CNA.Graphics.RenderTarget2D)renderTarget.FrameworkTexture);
    }

    public RenderTargetBinding(RenderTargetCube renderTarget, CubeMapFace cubeMapFace)
    {
        ArgumentNullException.ThrowIfNull(renderTarget);
        _framework = new CNA.Graphics.RenderTargetBinding(
            (CNA.Graphics.RenderTargetCube)renderTarget.FrameworkTexture,
            (CNA.Graphics.CubeMapFace)(int)cubeMapFace);
    }

    /// <summary>The bound target, re-typed. <see langword="null"/> for a default-constructed
    /// binding, and also when the underlying target is a <c>CNA.Graphics</c> instance this layer
    /// did not create -- which cannot happen through this type's own constructors.</summary>
    public Texture? RenderTarget => Texture.FromFramework(_framework.RenderTarget);

    public CubeMapFace CubeMapFace => (CubeMapFace)(int)_framework.CubeMapFace;

    public static implicit operator RenderTargetBinding(RenderTarget2D renderTarget) => new(renderTarget);

    internal CNA.Graphics.RenderTargetBinding Framework => _framework;

    internal static RenderTargetBinding FromFramework(CNA.Graphics.RenderTargetBinding value) =>
        new(value);

    private RenderTargetBinding(CNA.Graphics.RenderTargetBinding value)
    {
        _framework = value;
    }
}
