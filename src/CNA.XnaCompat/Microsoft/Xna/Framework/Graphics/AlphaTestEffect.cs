namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>AlphaTestEffect</c>. Derives from this namespace's <see cref="Effect"/>
/// -- see that type's doc comment for why these forward to an inner <c>CNA.Graphics</c> effect
/// rather than inheriting from it, and how the two stay one native effect.</summary>
public class AlphaTestEffect : Effect, IEffectMatrices, IEffectFog
{
    public AlphaTestEffect(GraphicsDevice device)
        : base(device, new CNA.Graphics.AlphaTestEffect(
            (device ?? throw new ArgumentNullException(nameof(device))).Framework))
    {
    }

    private CNA.Graphics.AlphaTestEffect Typed => (CNA.Graphics.AlphaTestEffect)Inner;

    public Vector3 DiffuseColor
    {
        get => Typed.DiffuseColor.ToCompat();
        set => Typed.DiffuseColor = value.ToFramework();
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
        get => Typed.World.ToCompat();
        set => Typed.World = value.ToFramework();
    }

    public Matrix View
    {
        get => Typed.View.ToCompat();
        set => Typed.View = value.ToFramework();
    }

    public Matrix Projection
    {
        get => Typed.Projection.ToCompat();
        set => Typed.Projection = value.ToFramework();
    }

    public bool FogEnabled
    {
        get => Typed.FogEnabled;
        set => Typed.FogEnabled = value;
    }

    public Vector3 FogColor
    {
        get => Typed.FogColor.ToCompat();
        set => Typed.FogColor = value.ToFramework();
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
    public override Effect Clone() => new AlphaTestEffect(this);

    protected AlphaTestEffect(AlphaTestEffect cloneSource)
        : this(
            (cloneSource ?? throw new ArgumentNullException(nameof(cloneSource))).GraphicsDevice,
            (CNA.Graphics.AlphaTestEffect)cloneSource.Typed.Clone())
    {
    }

    protected internal override void OnApply() => base.OnApply();

    /// <summary>Adopts an already-cloned inner effect. Private: only <see cref="Clone"/> has
    /// one.</summary>
    private AlphaTestEffect(GraphicsDevice graphicsDevice, CNA.Graphics.AlphaTestEffect inner)
        : base(graphicsDevice, inner)
    {
    }
}
