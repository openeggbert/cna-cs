namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EnvironmentMapEffect</c>. Derives from this namespace's <see cref="Effect"/>
/// -- see that type's doc comment for why these forward to an inner <c>CNA.Graphics</c> effect
/// rather than inheriting from it, and how the two stay one native effect.</summary>
public class EnvironmentMapEffect : Effect, IEffectMatrices, IEffectFog, IEffectLights, CNA.Graphics.IEffectMatrices, CNA.Graphics.IEffectFog, CNA.Graphics.IEffectLights
{
    public EnvironmentMapEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice, new CNA.Graphics.EnvironmentMapEffect(graphicsDevice))
    {
    }

    private CNA.Graphics.EnvironmentMapEffect Typed => (CNA.Graphics.EnvironmentMapEffect)Inner;

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
        get => Typed.EnvironmentMapSpecular;
        set => Typed.EnvironmentMapSpecular = value;
    }

    public float FresnelFactor
    {
        get => Typed.FresnelFactor;
        set => Typed.FresnelFactor = value;
    }

    public Texture? Texture
    {
        get => Typed.Texture as Texture;
        set => Typed.Texture = value;
    }

    public TextureCube? EnvironmentMap
    {
        get => Typed.EnvironmentMap as TextureCube;
        set => Typed.EnvironmentMap = value;
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


    /// <summary>Clones both halves: the native effect and a matching compat wrapper around it. See
    /// <see cref="Effect.Clone"/> for why the base cannot do this.</summary>
    public override Effect Clone() =>
        new EnvironmentMapEffect((GraphicsDevice)GraphicsDevice, (CNA.Graphics.EnvironmentMapEffect)Typed.Clone());

    /// <summary>Adopts an already-cloned inner effect. Private: only <see cref="Clone"/> has
    /// one.</summary>
    private EnvironmentMapEffect(GraphicsDevice graphicsDevice, CNA.Graphics.EnvironmentMapEffect inner)
        : base(graphicsDevice, inner)
    {
    }
}
