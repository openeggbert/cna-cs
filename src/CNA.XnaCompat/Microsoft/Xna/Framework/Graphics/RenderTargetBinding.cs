namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>RenderTargetBinding</c>: a duplicated value type with a conversion to its
/// <c>CNA.Graphics</c> counterpart, the pattern this layer uses for every struct.
///
/// Landed with <see cref="GraphicsDevice.SetRenderTargets"/> in the WP16 re-audit -- both the type
/// and the multiple-render-target route it exists for were missing.
/// </summary>
public readonly struct RenderTargetBinding : IEquatable<RenderTargetBinding>
{
    private readonly CNA.Graphics.RenderTargetBinding _framework;

    public RenderTargetBinding(RenderTarget2D renderTarget, int arraySlice = 0)
    {
        ArgumentNullException.ThrowIfNull(renderTarget);
        _framework = new CNA.Graphics.RenderTargetBinding(renderTarget, arraySlice);
    }

    public RenderTargetBinding(RenderTargetCube renderTarget, CubeMapFace cubeMapFace)
    {
        ArgumentNullException.ThrowIfNull(renderTarget);
        _framework = new CNA.Graphics.RenderTargetBinding(
            renderTarget, (CNA.Graphics.CubeMapFace)(int)cubeMapFace);
    }

    /// <summary>The bound target, re-typed. <see langword="null"/> for a default-constructed
    /// binding, and also when the underlying target is a <c>CNA.Graphics</c> instance this layer
    /// did not create -- which cannot happen through this type's own constructors.</summary>
    public Texture? RenderTarget => _framework.RenderTarget as Texture;

    public int ArraySlice => _framework.ArraySlice;

    public CubeMapFace CubeMapFace => (CubeMapFace)(int)_framework.CubeMapFace;

    public static implicit operator RenderTargetBinding(RenderTarget2D renderTarget) => new(renderTarget);

    public static implicit operator CNA.Graphics.RenderTargetBinding(RenderTargetBinding value) => value._framework;

    public readonly bool Equals(RenderTargetBinding other) => _framework.Equals(other._framework);

    public override readonly bool Equals(object? obj) => obj is RenderTargetBinding other && Equals(other);

    public override readonly int GetHashCode() => _framework.GetHashCode();

    public static bool operator ==(RenderTargetBinding a, RenderTargetBinding b) => a.Equals(b);

    public static bool operator !=(RenderTargetBinding a, RenderTargetBinding b) => !a.Equals(b);
}
