namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>AlphaTestEffect</c>. Derives from this namespace's <see cref="Effect"/>
/// -- see that type's doc comment for why these forward to an inner <c>CNA.Graphics</c> effect
/// rather than inheriting from it, and how the two stay one native effect.</summary>
public class AlphaTestEffect : Effect, IEffectMatrices, IEffectFog
{
    public AlphaTestEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice, new CNA.Graphics.AlphaTestEffect(graphicsDevice.Framework))
    {
    }

    private CNA.Graphics.AlphaTestEffect Typed => (CNA.Graphics.AlphaTestEffect)Inner;

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

    public int ReferenceAlpha
    {
        get => Typed.ReferenceAlpha;
        set => Typed.ReferenceAlpha = value;
    }

    public CompareFunction AlphaFunction
    {
        get => (CompareFunction)(int)Typed.AlphaFunction;
        set => Typed.AlphaFunction = (CNA.Graphics.CompareFunction)(int)value;
    }

    public Texture2D? Texture
    {
        get => global::Microsoft.Xna.Framework.Graphics.Texture.FromFramework(Typed.Texture) as Texture2D;
        set => Typed.Texture = value?.FrameworkTexture;
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

    /// <summary>Clones both halves: the native effect and a matching compat wrapper around it. See
    /// <see cref="Effect.Clone"/> for why the base cannot do this.</summary>
    public override Effect Clone() =>
        new AlphaTestEffect((GraphicsDevice)GraphicsDevice, (CNA.Graphics.AlphaTestEffect)Typed.Clone());

    /// <summary>Adopts an already-cloned inner effect. Private: only <see cref="Clone"/> has
    /// one.</summary>
    private AlphaTestEffect(GraphicsDevice graphicsDevice, CNA.Graphics.AlphaTestEffect inner)
        : base(graphicsDevice, inner)
    {
    }
}
