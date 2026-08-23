namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>SkinnedEffect</c>. Derives from this namespace's <see cref="Effect"/>
/// -- see that type's doc comment for why these forward to an inner <c>CNA.Graphics</c> effect
/// rather than inheriting from it, and how the two stay one native effect.</summary>
public class SkinnedEffect : Effect, IEffectMatrices, IEffectFog, IEffectLights
{
    public SkinnedEffect(GraphicsDevice device)
        : base(device, new CNA.Graphics.SkinnedEffect(
            (device ?? throw new ArgumentNullException(nameof(device))).Framework))
    {
    }

    private CNA.Graphics.SkinnedEffect Typed => (CNA.Graphics.SkinnedEffect)Inner;

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

    public Vector3 SpecularColor
    {
        get => Typed.SpecularColor.ToCompat();
        set => Typed.SpecularColor = value.ToFramework();
    }

    public float SpecularPower
    {
        get => Typed.SpecularPower;
        set => Typed.SpecularPower = value;
    }

    public float Alpha
    {
        get => Typed.Alpha;
        set => Typed.Alpha = value;
    }

    public bool PreferPerPixelLighting
    {
        get => Typed.PreferPerPixelLighting;
        set => Typed.PreferPerPixelLighting = value;
    }

    internal bool VertexColorEnabled
    {
        get => Typed.VertexColorEnabled;
        set => Typed.VertexColorEnabled = value;
    }

    public int WeightsPerVertex
    {
        get => Typed.WeightsPerVertex;
        set => Typed.WeightsPerVertex = value;
    }

    public Texture2D? Texture
    {
        get => global::Microsoft.Xna.Framework.Graphics.Texture.FromFramework(Typed.Texture) as Texture2D;
        set => Typed.Texture = value?.FrameworkTexture;
    }

    /// <summary>Real XNA's documented bone ceiling, forwarded rather than restated so the two
    /// cannot drift.</summary>
    public const int MaxBones = CNA.Graphics.SkinnedEffect.MaxBones;

    public void SetBoneTransforms(Matrix[] boneTransforms)
    {
        ArgumentNullException.ThrowIfNull(boneTransforms);

        var converted = new CNA.Matrix[boneTransforms.Length];
        for (int i = 0; i < converted.Length; i++)
        {
            converted[i] = boneTransforms[i].ToFramework();
        }

        Typed.SetBoneTransforms(converted);
    }

    public Matrix[] GetBoneTransforms(int count)
    {
        CNA.Matrix[] source = Typed.GetBoneTransforms(count);
        var converted = new Matrix[source.Length];
        for (int i = 0; i < converted.Length; i++)
        {
            converted[i] = source[i].ToCompat();
        }

        return converted;
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
    public override Effect Clone() => new SkinnedEffect(this);

    protected SkinnedEffect(SkinnedEffect cloneSource)
        : this(
            (cloneSource ?? throw new ArgumentNullException(nameof(cloneSource))).GraphicsDevice,
            (CNA.Graphics.SkinnedEffect)cloneSource.Typed.Clone())
    {
    }

    protected internal override void OnApply() => base.OnApply();

    /// <summary>Adopts an already-cloned inner effect. Private: only <see cref="Clone"/> has
    /// one.</summary>
    private SkinnedEffect(GraphicsDevice graphicsDevice, CNA.Graphics.SkinnedEffect inner)
        : base(graphicsDevice, inner)
    {
    }
}
