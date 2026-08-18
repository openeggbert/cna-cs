namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>SkinnedEffect</c>. Derives from this namespace's <see cref="Effect"/>
/// -- see that type's doc comment for why these forward to an inner <c>CNA.Graphics</c> effect
/// rather than inheriting from it, and how the two stay one native effect.</summary>
public class SkinnedEffect : Effect, IEffectMatrices, IEffectFog, IEffectLights, CNA.Graphics.IEffectMatrices, CNA.Graphics.IEffectFog, CNA.Graphics.IEffectLights
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

    public Texture? Texture
    {
        get => Typed.Texture as Texture;
        set => Typed.Texture = value;
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

    CNA.Vector3 CNA.Graphics.IEffectLights.AmbientLightColor
    {
        get => Typed.AmbientLightColor;
        set => Typed.AmbientLightColor = value;
    }

    bool CNA.Graphics.IEffectLights.LightingEnabled
    {
        get => Typed.LightingEnabled;
        set => Typed.LightingEnabled = value;
    }

    CNA.Graphics.DirectionalLight CNA.Graphics.IEffectLights.DirectionalLight0 => Typed.DirectionalLight0;

    CNA.Graphics.DirectionalLight CNA.Graphics.IEffectLights.DirectionalLight1 => Typed.DirectionalLight1;

    CNA.Graphics.DirectionalLight CNA.Graphics.IEffectLights.DirectionalLight2 => Typed.DirectionalLight2;

    void CNA.Graphics.IEffectLights.EnableDefaultLighting() => Typed.EnableDefaultLighting();

}
