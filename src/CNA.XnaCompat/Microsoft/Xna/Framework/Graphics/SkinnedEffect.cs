namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>SkinnedEffect</c>. Derives from this namespace's <see cref="Effect"/>
/// -- see that type's doc comment for why these forward to an inner <c>CNA.Graphics</c> effect
/// rather than inheriting from it, and how the two stay one native effect.</summary>
public class SkinnedEffect : Effect, IEffectMatrices, IEffectFog, IEffectLights
{
    public SkinnedEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice, new CNA.Graphics.SkinnedEffect(graphicsDevice))
    {
    }

    private CNA.Graphics.SkinnedEffect Typed => (CNA.Graphics.SkinnedEffect)Inner;

    public Vector3 DiffuseColor
    {
        get => Typed.DiffuseColor;
        set => Typed.DiffuseColor = value;
    }

    public Vector3 EmissiveColor
    {
        get => Typed.EmissiveColor;
        set => Typed.EmissiveColor = value;
    }

    public Vector3 SpecularColor
    {
        get => Typed.SpecularColor;
        set => Typed.SpecularColor = value;
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

    public bool VertexColorEnabled
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
            converted[i] = boneTransforms[i];
        }

        Typed.SetBoneTransforms(converted);
    }

    public Matrix[] GetBoneTransforms(int count)
    {
        CNA.Matrix[] source = Typed.GetBoneTransforms(count);
        var converted = new Matrix[source.Length];
        for (int i = 0; i < converted.Length; i++)
        {
            converted[i] = source[i];
        }

        return converted;
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

    public Vector3 AmbientLightColor
    {
        get => Typed.AmbientLightColor;
        set => Typed.AmbientLightColor = value;
    }

    public bool LightingEnabled
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
    public override Effect Clone() =>
        new SkinnedEffect((GraphicsDevice)GraphicsDevice, (CNA.Graphics.SkinnedEffect)Typed.Clone());

    /// <summary>Adopts an already-cloned inner effect. Private: only <see cref="Clone"/> has
    /// one.</summary>
    private SkinnedEffect(GraphicsDevice graphicsDevice, CNA.Graphics.SkinnedEffect inner)
        : base(graphicsDevice, inner)
    {
    }
}
