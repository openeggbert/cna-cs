using CNA.Interop;

namespace CNA.Graphics.Experimental;

/// <summary>
/// The curve a <see cref="TonemapSettings"/> applies when mapping HDR values into a displayable
/// range.
///
/// <see cref="Uncharted2"/> sits apart from the other four in CNA's own headers -- appended rather
/// than inserted, because the preceding ordinals are stored in pipeline settings and compared
/// numerically elsewhere. It is also the one curve that does not bake gamma into itself, so
/// <see cref="TonemapSettings.Gamma"/> still applies after it.
/// </summary>
public enum TonemappingMode
{
    /// <summary>No curve. Exposure and gamma still apply.</summary>
    None = 0,
    Reinhard = 1,
    Filmic = 2,
    Aces = 3,
    Uncharted2 = 4,
}

/// <summary>A quality tier, for the settings CNA derives from one.</summary>
public enum RenderQuality
{
    Low = 0,
    Medium = 1,
    High = 2,
    Ultra = 3,
}

/// <summary>
/// A bloom pass's settings: what counts as bright, how much of it comes back, and how wide the blur
/// spreads.
///
/// A view over the native pass, not a copy of its state. Reading a property asks CNA, so two views
/// over one pass cannot disagree, and a pass reconfigured after it was added to a chain takes the
/// new value.
///
/// A class rather than a struct, and the compiler decided that: a settings <em>struct</em> returned
/// from a property cannot have its members assigned, because assigning through a temporary is
/// almost always a bug even when -- as here -- it would have forwarded to native and worked. One
/// instance is created per pass and kept, so the natural spelling costs no allocation per access.
/// </summary>
public sealed class BloomSettings
{
    private readonly PostProcessPass _pass;

    internal BloomSettings(PostProcessPass pass) => _pass = pass;

    /// <summary>Luminance above which a texel contributes to the bloom.</summary>
    /// <exception cref="CnaException">The pass is not a bloom pass. CNA answers
    /// <c>InvalidArgument</c>, which is native's own type check rather than one repeated here.</exception>
    public float Threshold
    {
        get => Read(Native.cna_bloom_pass_get_threshold, nameof(Threshold));
        set => Write(Native.cna_bloom_pass_set_threshold, value, nameof(Threshold));
    }

    /// <summary>How strongly the blurred result is added back.</summary>
    public float Intensity
    {
        get => Read(Native.cna_bloom_pass_get_intensity, nameof(Intensity));
        set => Write(Native.cna_bloom_pass_set_intensity, value, nameof(Intensity));
    }

    /// <summary>How many blur iterations run. Wider bloom costs more targets.</summary>
    public int Iterations
    {
        get
        {
            CnaResult result = Native.cna_bloom_pass_get_iterations(_pass.NativeHandle, out int value);
            GC.KeepAlive(_pass);
            CnaException.ThrowIfFailed(result, nameof(Iterations));
            return value;
        }

        set
        {
            CnaResult result = Native.cna_bloom_pass_set_iterations(_pass.NativeHandle, value);
            GC.KeepAlive(_pass);
            CnaException.ThrowIfFailed(result, nameof(Iterations));
        }
    }

    /// <summary>
    /// Drops the intermediate targets the pass is holding, so the next apply allocates them again at
    /// whatever size it then needs.
    ///
    /// The reason a game calls this is a resolution change: the blur chain is sized from the frame,
    /// and targets kept from a larger one are wasted memory rather than a correctness problem.
    /// </summary>
    public void ResetTargets()
    {
        CnaResult result = Native.cna_bloom_pass_reset_targets(_pass.NativeHandle);
        GC.KeepAlive(_pass);
        CnaException.ThrowIfFailed(result, nameof(ResetTargets));
    }

    /// <summary>
    /// What the extraction step does to one channel, computed by CNA without a pass and without a
    /// device.
    ///
    /// <b>This is the only exact evidence available about what the pass does.</b> A shader's output
    /// can otherwise only be checked against a reimplementation of the shader, which tests the
    /// reimplementation. CNA exposing its own arithmetic as a function means a caller -- and a test
    /// -- can ask what the curve is rather than infer it from pixels.
    ///
    /// <b>It still needs the engine layer.</b> Deviceless is not the same as unconditional:
    /// measured on a build without the extended graphics layer, this answers <c>NotSupported</c>
    /// like every other route in the header. The arithmetic is pure; its presence in the binary is
    /// not.
    /// </summary>
    /// <exception cref="CnaException"><c>NotSupported</c> when the build has no engine layer; ask
    /// <c>GraphicsDevice.IsCnaEngineLayerAvailable()</c> first.</exception>
    public static float ExtractChannel(float value, float threshold)
    {
        CnaResult result = Native.cna_bloom_pass_extract_channel(value, threshold, out float extracted);
        CnaException.ThrowIfFailed(result, nameof(ExtractChannel));
        return extracted;
    }

    /// <summary>The iteration count CNA recommends for a quality tier.</summary>
    public static int IterationsForQuality(RenderQuality quality)
    {
        CnaResult result = Native.cna_bloom_pass_iterations_for_quality(
            (CnaRenderQuality)quality, out int iterations);
        CnaException.ThrowIfFailed(result, nameof(IterationsForQuality));
        return iterations;
    }

    private float Read(FloatGetter getter, string context)
    {
        CnaResult result = getter(_pass.NativeHandle, out float value);
        GC.KeepAlive(_pass);
        CnaException.ThrowIfFailed(result, context);
        return value;
    }

    private void Write(FloatSetter setter, float value, string context)
    {
        CnaResult result = setter(_pass.NativeHandle, value);
        GC.KeepAlive(_pass);
        CnaException.ThrowIfFailed(result, context);
    }

    internal delegate CnaResult FloatGetter(CnaHandle pass, out float value);

    internal delegate CnaResult FloatSetter(CnaHandle pass, float value);
}

/// <summary>
/// A tonemap pass's settings. A view over the native pass, on the same terms as
/// <see cref="BloomSettings"/>, including why it is a class.
/// </summary>
public sealed class TonemapSettings
{
    private readonly PostProcessPass _pass;

    internal TonemapSettings(PostProcessPass pass) => _pass = pass;

    /// <summary>The curve applied.</summary>
    public TonemappingMode Mode
    {
        get
        {
            CnaResult result = Native.cna_tonemap_pass_get_mode(_pass.NativeHandle, out CnaTonemappingMode mode);
            GC.KeepAlive(_pass);
            CnaException.ThrowIfFailed(result, nameof(Mode));
            return (TonemappingMode)mode;
        }

        set
        {
            CnaResult result = Native.cna_tonemap_pass_set_mode(_pass.NativeHandle, (CnaTonemappingMode)value);
            GC.KeepAlive(_pass);
            CnaException.ThrowIfFailed(result, nameof(Mode));
        }
    }

    /// <summary>Linear multiplier applied before the curve.</summary>
    public float Exposure
    {
        get => Read(Native.cna_tonemap_pass_get_exposure, nameof(Exposure));
        set => Write(Native.cna_tonemap_pass_set_exposure, value, nameof(Exposure));
    }

    /// <summary>The display gamma applied after the curve.</summary>
    public float Gamma
    {
        get => Read(Native.cna_tonemap_pass_get_gamma, nameof(Gamma));
        set => Write(Native.cna_tonemap_pass_set_gamma, value, nameof(Gamma));
    }

    /// <summary>Whether a dither is applied to hide banding in smooth gradients.</summary>
    public bool DebandEnabled
    {
        get
        {
            CnaResult result = Native.cna_tonemap_pass_is_deband_enabled(_pass.NativeHandle, out byte enabled);
            GC.KeepAlive(_pass);
            CnaException.ThrowIfFailed(result, nameof(DebandEnabled));
            return enabled != 0;
        }

        set
        {
            CnaResult result = Native.cna_tonemap_pass_set_deband_enabled(
                _pass.NativeHandle, (byte)(value ? 1 : 0));
            GC.KeepAlive(_pass);
            CnaException.ThrowIfFailed(result, nameof(DebandEnabled));
        }
    }

    /// <summary>How strong that dither is.</summary>
    public float DebandStrength
    {
        get => Read(Native.cna_tonemap_pass_get_deband_strength, nameof(DebandStrength));
        set => Write(Native.cna_tonemap_pass_set_deband_strength, value, nameof(DebandStrength));
    }

    /// <summary>
    /// What the pass does to one channel, computed by CNA without a pass and without a device -- the
    /// exact counterpart of <see cref="BloomSettings.ExtractChannel"/>, and for the same reason.
    ///
    /// Useful beyond testing: a game that has to match its tonemapping outside the pass -- picking a
    /// UI colour that will sit correctly against the tonemapped scene, say -- can ask for the curve
    /// rather than reimplement it and drift.
    ///
    /// Needs the engine layer, exactly as <see cref="BloomSettings.ExtractChannel"/> does.
    /// </summary>
    /// <exception cref="CnaException"><c>NotSupported</c> when the build has no engine layer.</exception>
    public static float TonemapChannel(TonemappingMode mode, float value, float exposure, float gamma)
    {
        CnaResult result = Native.cna_tonemap_pass_tonemap_channel(
            (CnaTonemappingMode)mode, value, exposure, gamma, out float mapped);
        CnaException.ThrowIfFailed(result, nameof(TonemapChannel));
        return mapped;
    }

    private float Read(BloomSettings.FloatGetter getter, string context)
    {
        CnaResult result = getter(_pass.NativeHandle, out float value);
        GC.KeepAlive(_pass);
        CnaException.ThrowIfFailed(result, context);
        return value;
    }

    private void Write(BloomSettings.FloatSetter setter, float value, string context)
    {
        CnaResult result = setter(_pass.NativeHandle, value);
        GC.KeepAlive(_pass);
        CnaException.ThrowIfFailed(result, context);
    }
}
