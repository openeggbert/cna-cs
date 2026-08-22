namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EnvironmentMapEffect</c>. Derives from this namespace's <see cref="Effect"/>
/// -- see that type's doc comment for why these forward to an inner <c>CNA.Graphics</c> effect
/// rather than inheriting from it, and how the two stay one native effect.</summary>
public class EnvironmentMapEffect : Effect, IEffectMatrices, IEffectFog, IEffectLights
{
    public EnvironmentMapEffect(GraphicsDevice device)
        : base(device, new CNA.Graphics.EnvironmentMapEffect(device.Framework))
    {
    }

    private CNA.Graphics.EnvironmentMapEffect Typed => (CNA.Graphics.EnvironmentMapEffect)Inner;

    public Vector3 DiffuseColor
    {
        get => Typed.DiffuseColor.ToCompat();
        set => Typed.DiffuseColor = value.ToFramework();
    }

    public Vector3 EmissiveColor
    {
        get => Typed.EmissiveColor.ToCompat();
        set => Typed.EmissiveColor = value.ToFramework();
    }

    public float Alpha
    {
        get => Typed.Alpha;
        set => Typed.Alpha = value;
    }

    public float EnvironmentMapAmount
    {
        get => Typed.EnvironmentMapAmount;
        set => Typed.EnvironmentMapAmount = value;
    }

    public Vector3 EnvironmentMapSpecular
    {
        get => Typed.EnvironmentMapSpecular.ToCompat();
        set => Typed.EnvironmentMapSpecular = value.ToFramework();
    }

    public float FresnelFactor
    {
        get => Typed.FresnelFactor;
        set => Typed.FresnelFactor = value;
    }

    public Texture2D? Texture
    {
        get => global::Microsoft.Xna.Framework.Graphics.Texture.FromFramework(Typed.Texture) as Texture2D;
        set => Typed.Texture = value?.FrameworkTexture;
    }

    public TextureCube? EnvironmentMap
    {
        get => global::Microsoft.Xna.Framework.Graphics.Texture.FromFramework(Typed.EnvironmentMap) as TextureCube;
        set => Typed.EnvironmentMap = value?.FrameworkTexture as CNA.Graphics.TextureCube;
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

    public Vector3 AmbientLightColor
    {
        get => Typed.AmbientLightColor.ToCompat();
        set => Typed.AmbientLightColor = value.ToFramework();
    }

    bool IEffectLights.LightingEnabled
    {
        get => Typed.LightingEnabled;
        set => Typed.LightingEnabled = value;
    }

    public void EnableDefaultLighting() => Typed.EnableDefaultLighting();

    /// <summary>Each wraps the single light the inner effect already constructed rather than
    /// building a second one -- see <see cref="DirectionalLight"/>'s own doc comment.</summary>
    public DirectionalLight DirectionalLight0 => _light0 ??= new DirectionalLight(Typed.DirectionalLight0);

    public DirectionalLight DirectionalLight1 => _light1 ??= new DirectionalLight(Typed.DirectionalLight1);

    public DirectionalLight DirectionalLight2 => _light2 ??= new DirectionalLight(Typed.DirectionalLight2);

    private DirectionalLight? _light0;
    private DirectionalLight? _light1;
    private DirectionalLight? _light2;

    /// <summary>Clones both halves: the native effect and a matching compat wrapper around it. See
    /// <see cref="Effect.Clone"/> for why the base cannot do this.</summary>
    public override Effect Clone() => new EnvironmentMapEffect(this);

    protected EnvironmentMapEffect(EnvironmentMapEffect cloneSource)
        : this(
            (cloneSource ?? throw new ArgumentNullException(nameof(cloneSource))).GraphicsDevice,
            (CNA.Graphics.EnvironmentMapEffect)cloneSource.Typed.Clone())
    {
    }

    protected internal override void OnApply() => base.OnApply();

    /// <summary>Adopts an already-cloned inner effect. Private: only <see cref="Clone"/> has
    /// one.</summary>
    private EnvironmentMapEffect(GraphicsDevice graphicsDevice, CNA.Graphics.EnvironmentMapEffect inner)
        : base(graphicsDevice, inner)
    {
    }
}
