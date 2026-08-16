using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// XNA's built-in general-purpose shader effect (per-vertex lighting, fog, texturing, vertex
/// color). No ABI shape for any of this exists in the analysis docs, but this is unusually
/// well-grounded for a self-designed surface: every property here, <see cref="EnableDefaultLighting"/>'s
/// exact default light values, and <see cref="OnApply"/>'s parameter-computation algorithm are
/// read directly from the real openeggbert/cna C++ engine's own
/// <c>Microsoft::Xna::Framework::Graphics::BasicEffect</c> implementation (headers and
/// <c>BasicEffect.cpp</c>'s <c>FillGpuDrawParams</c> method) -- not invented, not guessed. That
/// implementation confirmed something important: constructing a <c>BasicEffect</c> and setting
/// its properties needs **no native call at all** (matching <c>SpriteFont</c>'s own zero-ABI
/// escape hatch) -- the real C++ constructor chain is pure object state, no renderer/GPU handle
/// allocation happens until a draw call actually applies the effect. Only <see cref="OnApply"/>
/// (via <see cref="Effect.Apply"/>) crosses into native code.
///
/// Deliberately not implemented, all real, separate follow-ups rather than gaps in this pass:
/// 3D positional audio has no analog here, but the equivalent omissions are <c>Texture1</c>
/// (<c>DualTextureEffect</c>-only), environment/cube mapping, skinning (bone transforms),
/// PBR, and fresnel -- none of which <c>BasicEffect</c> itself uses; see
/// <c>CnaBasicEffectParams</c>'s own doc comment for the full reduced-field-set reasoning.
///
/// Implements <see cref="IEffectMatrices"/>/<see cref="IEffectFog"/>/<see cref="IEffectLights"/>,
/// same as the real C++ engine's own <c>BasicEffect</c> (confirmed against its header, not
/// invented) -- <see cref="Model.Draw"/> is the reason <see cref="IEffectMatrices"/> exists at
/// all in this project. <see cref="IEffectFog"/>/<see cref="IEffectLights"/>'s members already
/// match this class's own property names/types exactly, so they're implicitly satisfied; only
/// <see cref="IEffectMatrices"/> needs an explicit forwarding implementation, because
/// <see cref="World"/>/<see cref="View"/>/<see cref="Projection"/> are public fields here (matching
/// the real C++ engine's own field-not-property choice) and a field cannot satisfy an interface
/// property directly in C# -- the real C++ engine hits the same shape mismatch against its own
/// <c>IEffectMatrices</c> and resolves it the identical way, with explicit override methods
/// wrapping the field.
/// </summary>
public class BasicEffect : Effect, IEffectMatrices, IEffectFog, IEffectLights
{
    private Texture2D? _texture;

    public Matrix World = Matrix.Identity;
    public Matrix View = Matrix.Identity;
    public Matrix Projection = Matrix.Identity;
    public bool VertexColorEnabled;

    Matrix IEffectMatrices.World
    {
        get => World;
        set => World = value;
    }

    Matrix IEffectMatrices.View
    {
        get => View;
        set => View = value;
    }

    Matrix IEffectMatrices.Projection
    {
        get => Projection;
        set => Projection = value;
    }

    public BasicEffect(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
        // Matches the real C++ engine's own constructor exactly: only DirectionalLight0 starts
        // enabled; DirectionalLight1/2 start disabled until EnableDefaultLighting() (or manual
        // configuration) turns them on. Direction/color defaults for the not-yet-configured
        // lights aren't specified by anything the source research turned up -- Vector3.Down with
        // zero diffuse/specular is a reasonable inert default (matches the real GpuDrawParams
        // struct's own default light direction), not a value read from real XNA/the C++ engine.
        DirectionalLight0 = new DirectionalLight(Vector3.Down, Vector3.Zero, Vector3.Zero, enabled: true);
        DirectionalLight1 = new DirectionalLight(Vector3.Down, Vector3.Zero, Vector3.Zero, enabled: false);
        DirectionalLight2 = new DirectionalLight(Vector3.Down, Vector3.Zero, Vector3.Zero, enabled: false);
    }

    public DirectionalLight DirectionalLight0 { get; }

    public DirectionalLight DirectionalLight1 { get; }

    public DirectionalLight DirectionalLight2 { get; }

    public Vector3 DiffuseColor { get; set; } = Vector3.One;

    public Vector3 EmissiveColor { get; set; } = Vector3.Zero;

    public Vector3 SpecularColor { get; set; } = Vector3.One;

    public float SpecularPower { get; set; } = 16f;

    public Vector3 AmbientLightColor { get; set; } = Vector3.Zero;

    public float Alpha { get; set; } = 1f;

    public bool LightingEnabled { get; set; }

    public bool PreferPerPixelLighting { get; set; }

    public bool TextureEnabled { get; set; }

    public Texture2D? Texture
    {
        get => _texture;
        set => _texture = value;
    }

    public bool FogEnabled { get; set; }

    public Vector3 FogColor { get; set; } = Vector3.Zero;

    public float FogStart { get; set; }

    public float FogEnd { get; set; } = 1f;

    /// <summary>
    /// The exact real XNA/the real C++ engine's default three-point lighting rig -- every numeric
    /// literal here is quoted verbatim from <c>BasicEffect.cpp</c>'s own
    /// <c>EnableDefaultLighting()</c> implementation, not approximated.
    /// </summary>
    public void EnableDefaultLighting()
    {
        LightingEnabled = true;
        AmbientLightColor = new Vector3(0.05333332f, 0.09882354f, 0.1819608f);

        DirectionalLight0.Direction = new Vector3(-0.5265408f, -0.5735765f, -0.6275069f);
        DirectionalLight0.DiffuseColor = new Vector3(1f, 0.9607844f, 0.8078432f);
        DirectionalLight0.SpecularColor = new Vector3(1f, 0.9607844f, 0.8078432f);
        DirectionalLight0.Enabled = true;

        DirectionalLight1.Direction = new Vector3(0.7198464f, 0.3420201f, 0.6040227f);
        DirectionalLight1.DiffuseColor = new Vector3(0.9647059f, 0.7607844f, 0.4078432f);
        DirectionalLight1.SpecularColor = Vector3.Zero;
        DirectionalLight1.Enabled = true;

        DirectionalLight2.Direction = new Vector3(0.4545195f, -0.7660444f, 0.4545195f);
        DirectionalLight2.DiffuseColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f);
        DirectionalLight2.SpecularColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f);
        DirectionalLight2.Enabled = true;

        SpecularColor = Vector3.One;
        SpecularPower = 16f;
    }

    /// <summary>
    /// Reproduces the real C++ engine's <c>BasicEffect::FillGpuDrawParams</c> algorithm exactly
    /// (read from its source, not reinvented), computed here in managed code using this project's
    /// own already-tested <see cref="Matrix"/>/<see cref="Vector3"/> math rather than crossing the
    /// ABI with raw <see cref="View"/>/<see cref="Projection"/> for native code to redo the same
    /// work -- see <see cref="EyePositionWorldForTests"/>/<see cref="FogVectorForTests"/> for the
    /// two derived values pulled out for direct unit testing.
    /// </summary>
    protected override void OnApply()
    {
        (Vector4 diffuse, Vector3 emissive, Vector3 specular, float specularPower, Vector3 eyePositionWorld) = ComputeLightingParams();
        Vector4 fogVector = ComputeFogVector();

        bool textureEnabled = TextureEnabled && _texture is not null;

        var nativeParams = new CnaBasicEffectParams
        {
            Texture = textureEnabled ? new CnaHandle(_texture!.NativeHandleValue) : CnaHandle.Zero,
            TextureEnabled = textureEnabled ? (byte)1 : (byte)0,
            VertexColorEnabled = VertexColorEnabled ? (byte)1 : (byte)0,
            LightingEnabled = LightingEnabled ? (byte)1 : (byte)0,
            PreferPerPixelLighting = PreferPerPixelLighting ? (byte)1 : (byte)0,
            DiffuseColor = diffuse.ToNative(),
            AmbientColor = AmbientLightColor.ToNative(),
            Light0 = ToNative(DirectionalLight0),
            Light1 = ToNative(DirectionalLight1),
            Light2 = ToNative(DirectionalLight2),
            EmissiveColor = emissive.ToNative(),
            SpecularColor = specular.ToNative(),
            SpecularPower = specularPower,
            EyePositionWorld = eyePositionWorld.ToNative(),
            FogEnabled = FogEnabled ? (byte)1 : (byte)0,
            FogColor = FogColor.ToNative(),
            FogVector = fogVector.ToNative(),
        };
        WriteColumnMajor(World, ref nativeParams.WorldColMajor);

        CnaResult result = Native.cna_graphics_device_apply_basic_effect(new CnaHandle(GraphicsDevice.NativeHandleValue), in nativeParams);
        CnaException.ThrowIfFailed(result, nameof(Apply));
    }

    /// <summary>
    /// Diffuse/emissive/specular/eye-position, matching <c>FillGpuDrawParams</c>'s own logic:
    /// EmissiveColor is baked into the forwarded diffuse when unlit (the real code's own comment
    /// explains why -- the lit-path material computation that would otherwise apply EmissiveColor
    /// never runs when lighting is off, so it would be silently dropped instead of baked in);
    /// specular/eye-position are only meaningful (and only computed) on the lit path.
    /// </summary>
    private (Vector4 Diffuse, Vector3 Emissive, Vector3 Specular, float SpecularPower, Vector3 EyePositionWorld) ComputeLightingParams()
    {
        Vector3 forwardedDiffuse = LightingEnabled ? DiffuseColor : DiffuseColor + EmissiveColor;
        var diffuse = new Vector4(forwardedDiffuse.X * Alpha, forwardedDiffuse.Y * Alpha, forwardedDiffuse.Z * Alpha, Alpha);

        if (!LightingEnabled)
        {
            return (diffuse, Vector3.Zero, Vector3.Zero, 0f, Vector3.Zero);
        }

        Vector3 emissive = EmissiveColor * Alpha;
        Matrix invertedView = Matrix.Invert(View);
        Vector3 eyePositionWorld = invertedView.Translation;
        return (diffuse, emissive, SpecularColor, SpecularPower, eyePositionWorld);
    }

    /// <summary>
    /// Matches <c>FillGpuDrawParams</c>'s own fog-vector derivation exactly: zero when fog is
    /// off; <c>(0,0,0,1)</c> (fully fogged) for the degenerate <c>FogStart == FogEnd</c> case
    /// (avoids a divide-by-zero the real code also guards against); otherwise derived from the
    /// combined world-view matrix's third row so the fog factor can be computed per-vertex as
    /// <c>dot(position, fogVector)</c> without a separate distance calculation in the shader.
    /// </summary>
    private Vector4 ComputeFogVector()
    {
        if (!FogEnabled)
        {
            return Vector4.Zero;
        }

        if (FogStart == FogEnd)
        {
            return new Vector4(0f, 0f, 0f, 1f);
        }

        Matrix fogWorldView = World * View;
        float s = 1f / (FogStart - FogEnd);
        return new Vector4(fogWorldView.M13 * s, fogWorldView.M23 * s, fogWorldView.M33 * s, (fogWorldView.M43 + FogStart) * s);
    }

    private static CnaDirectionalLight ToNative(DirectionalLight light)
    {
        // Matches FillGpuDrawParams exactly: Direction always crosses the ABI regardless of
        // Enabled, but Diffuse/SpecularColor are zeroed here (not on DirectionalLight's own
        // Enabled setter, which the real C++ DirectionalLight type doesn't do either) when the
        // light is off.
        return new CnaDirectionalLight
        {
            Direction = light.Direction.ToNative(),
            DiffuseColor = (light.Enabled ? light.DiffuseColor : Vector3.Zero).ToNative(),
            SpecularColor = (light.Enabled ? light.SpecularColor : Vector3.Zero).ToNative(),
        };
    }

    /// <summary>Writing <see cref="Matrix.Transpose"/>'s result out in ordinary row order produces
    /// the same 16 floats as writing the original matrix out in column order -- reuses the
    /// already-tested <see cref="Matrix.Transpose"/> instead of re-deriving the same element
    /// mapping by hand a second time.</summary>
    private static void WriteColumnMajor(Matrix m, ref CnaMatrix16 target)
    {
        Matrix t = Matrix.Transpose(m);
        target[0] = t.M11; target[1] = t.M12; target[2] = t.M13; target[3] = t.M14;
        target[4] = t.M21; target[5] = t.M22; target[6] = t.M23; target[7] = t.M24;
        target[8] = t.M31; target[9] = t.M32; target[10] = t.M33; target[11] = t.M34;
        target[12] = t.M41; target[13] = t.M42; target[14] = t.M43; target[15] = t.M44;
    }

    /// <summary>Exposes <see cref="ComputeFogVector"/>'s result for direct unit testing (the
    /// public path to it, <see cref="Effect.Apply"/>, calls into native code and can't be
    /// exercised without a real cna-native).</summary>
    internal Vector4 FogVectorForTests => ComputeFogVector();

    /// <summary>Exposes <see cref="ComputeLightingParams"/>'s eye-position result for direct unit
    /// testing, same reasoning as <see cref="FogVectorForTests"/>.</summary>
    internal Vector3 EyePositionWorldForTests => ComputeLightingParams().EyePositionWorld;
}
