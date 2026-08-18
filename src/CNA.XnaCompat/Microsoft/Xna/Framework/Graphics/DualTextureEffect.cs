namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>DualTextureEffect</c>. Derives from this namespace's <see cref="Effect"/>
/// -- see that type's doc comment for why these forward to an inner <c>CNA.Graphics</c> effect
/// rather than inheriting from it, and how the two stay one native effect.</summary>
public class DualTextureEffect : Effect, IEffectMatrices, IEffectFog, CNA.Graphics.IEffectMatrices, CNA.Graphics.IEffectFog
{
    public DualTextureEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice, new CNA.Graphics.DualTextureEffect(graphicsDevice))
    {
    }

    private CNA.Graphics.DualTextureEffect Typed => (CNA.Graphics.DualTextureEffect)Inner;

    public Vector3 DiffuseColor
    {
        get => Typed.DiffuseColor;
        set => Typed.DiffuseColor = value;
    }

    public float Alpha
    {
        get => Typed.Alpha;
        set => Typed.Alpha = value;
    }

    public bool VertexColorEnabled
    {
        get => Typed.VertexColorEnabled;
        set => Typed.VertexColorEnabled = value;
    }

    public Texture? Texture
    {
        get => Typed.Texture as Texture;
        set => Typed.Texture = value;
    }

    public Texture? Texture2
    {
        get => Typed.Texture2 as Texture;
        set => Typed.Texture2 = value;
    }

    public Matrix World
    {
        get => Typed.World;
        set => Typed.World = value;
    }

    public Matrix View
    {
        get => Typed.View;
        set => Typed.View = value;
    }

    public Matrix Projection
    {
        get => Typed.Projection;
        set => Typed.Projection = value;
    }

    public bool FogEnabled
    {
        get => Typed.FogEnabled;
        set => Typed.FogEnabled = value;
    }

    public Vector3 FogColor
    {
        get => Typed.FogColor;
        set => Typed.FogColor = value;
    }

    public float FogStart
    {
        get => Typed.FogStart;
        set => Typed.FogStart = value;
    }

    public float FogEnd
    {
        get => Typed.FogEnd;
        set => Typed.FogEnd = value;
    }

    // -- CNA.Graphics.IEffectMatrices ---------------------------------------------------------
    //
    // Implemented ALONGSIDE the compat interface of the same name, not instead of it. Before
    // Phase 8 WP4c these effects inherited it by deriving from their CNA counterpart; now that
    // they hold one instead, the contract has to be restated or it is silently lost -- and
    // CNA.Graphics.Model.Draw type-tests for exactly this interface, so losing it made every
    // compat-loaded model throw on Draw. Caught by a code-review pass, not by the build.

    CNA.Matrix CNA.Graphics.IEffectMatrices.World
    {
        get => Typed.World;
        set => Typed.World = value;
    }

    CNA.Matrix CNA.Graphics.IEffectMatrices.View
    {
        get => Typed.View;
        set => Typed.View = value;
    }

    CNA.Matrix CNA.Graphics.IEffectMatrices.Projection
    {
        get => Typed.Projection;
        set => Typed.Projection = value;
    }

    CNA.Vector3 CNA.Graphics.IEffectFog.FogColor
    {
        get => Typed.FogColor;
        set => Typed.FogColor = value;
    }

    bool CNA.Graphics.IEffectFog.FogEnabled
    {
        get => Typed.FogEnabled;
        set => Typed.FogEnabled = value;
    }

    float CNA.Graphics.IEffectFog.FogStart
    {
        get => Typed.FogStart;
        set => Typed.FogStart = value;
    }

    float CNA.Graphics.IEffectFog.FogEnd
    {
        get => Typed.FogEnd;
        set => Typed.FogEnd = value;
    }

}
