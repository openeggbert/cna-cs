namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>DualTextureEffect</c>. Derives from this namespace's <see cref="Effect"/>
/// -- see that type's doc comment for why these forward to an inner <c>CNA.Graphics</c> effect
/// rather than inheriting from it, and how the two stay one native effect. <c>Texture2</c> throws, as on the CNA side -- the C API has no second-layer function.</summary>
public class DualTextureEffect : Effect, IEffectMatrices, IEffectFog
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
}
