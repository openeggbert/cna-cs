namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>BasicEffect</c>. Extends <see cref="CNA.Graphics.BasicEffect"/> directly
/// (not a separate compat <c>Effect</c> base) -- the same "preserve the real logic's lineage over
/// namespace purity" trade-off <c>RenderTarget2D</c> already made this session, for the same
/// reason: <c>BasicEffect</c>'s actual value is its property surface and
/// <see cref="CNA.Graphics.BasicEffect.OnApply"/> algorithm, and a full separate compat
/// <c>Effect</c>/<c>EffectTechnique</c>/<c>EffectPass</c> hierarchy would either duplicate all of
/// that from scratch or hit the exact "two independent pieces of mutable state that can desync"
/// bug class this session already found and fixed once for <c>GraphicsDevice.Indices</c> --
/// <c>DirectionalLight0</c>/<c>1</c>/<c>2</c> are constructed once, inside the base class's own
/// constructor, with no seam for a compat subclass to intervene, so there is no safe way to give
/// them a compat-typed <c>DirectionalLight</c> without either duplicating construction (risking
/// the OnApply() computation reading a different light object than compat code mutated) or a
/// runtime-unsafe downcast (the actual objects were never constructed as any compat subclass).
///
/// **Real, narrow, documented compat gap as a result:** <c>effect.CurrentTechnique</c>,
/// <c>.Passes</c>, and <c>DirectionalLight0/1/2</c> are all inherited unchanged, so they return
/// this project's own <c>CNA.Graphics</c>-namespaced types, not XNA-namespaced ones. The common
/// <c>effect.CurrentTechnique.Passes[0].Apply();</c> and <c>effect.DirectionalLight0.Enabled =
/// true;</c> idioms both still compile and work correctly (nothing about them needs to *name* the
/// intermediate types) -- only an explicit type declaration like
/// <c>Microsoft.Xna.Framework.Graphics.EffectTechnique technique = effect.CurrentTechnique;</c> or
/// <c>...DirectionalLight light = effect.DirectionalLight0;</c> would fail to compile. All other
/// properties (<c>World</c>/<c>View</c>/<c>Projection</c>, <c>DiffuseColor</c>, ...) are
/// <c>CNA</c>-namespace-typed fields/properties that convert automatically through their implicit
/// operators at ordinary read/write call sites, same as every other inherited-unchanged member
/// elsewhere in this compat layer. Only <see cref="Texture"/> needs its own override, for the
/// same reason <c>SpriteFont.Texture</c> did.
/// </summary>
public class BasicEffect : CNA.Graphics.BasicEffect
{
    public BasicEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
    }

    public new Texture2D? Texture
    {
        get => (Texture2D?)base.Texture;
        set => base.Texture = value;
    }
}
