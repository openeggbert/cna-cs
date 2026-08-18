using CNA.Interop;

namespace CNA.Audio;

/// <summary>
/// Matches real XNA's <c>AudioEngine</c>: the root of the XACT authored-audio system, loaded from
/// a project settings file (<c>.xgs</c>). <see cref="WaveBank"/>s and <see cref="SoundBank"/>s are
/// loaded against it, and <see cref="Cue"/>s are played from those.
///
/// <see cref="Update"/> must be called once per frame, exactly as in real XNA -- XACT's own
/// per-cue state machine and variable evaluation run there, so without it cues never advance.
/// Unlike <c>MediaPlayer.Update</c>, this is *not* pumped by
/// <see cref="FrameworkDispatcher.Update"/>: an engine is created and owned by game code, and this
/// project has no registry of live engines to walk.
/// </summary>
public class AudioEngine : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public AudioEngine(string settingsFile)
    {
        ArgumentNullException.ThrowIfNull(settingsFile);

        CnaHandle engine = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            settingsFile, view => Native.cna_audio_engine_create(CnaAmbientGame.Current, view, out engine));
        CnaException.ThrowIfFailed(result, nameof(AudioEngine));

        _handle = new NativeResourceHandle(engine.AsNint, h => Native.cna_audio_engine_destroy(new CnaHandle(h)));
    }

    internal CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public bool IsDisposed
    {
        get
        {
            CnaResult result = Native.cna_audio_engine_get_is_disposed(NativeHandle, out byte value);
            CnaException.ThrowIfFailed(result, nameof(IsDisposed));
            return value != 0;
        }
    }

    /// <summary>Advances the audio engine. Call once per frame -- see this class's own doc
    /// comment.</summary>
    public void Update()
    {
        CnaResult result = Native.cna_audio_engine_update(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Update));
    }

    public float GetGlobalVariable(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        float value = 0f;
        CnaResult result = CnaStringMarshal.WithStringView(
            name, view => Native.cna_audio_engine_get_global_variable(NativeHandle, view, out value));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetGlobalVariable));
        return value;
    }

    public void SetGlobalVariable(string name, float value)
    {
        ArgumentNullException.ThrowIfNull(name);

        CnaResult result = CnaStringMarshal.WithStringView(
            name, view => Native.cna_audio_engine_set_global_variable(NativeHandle, view, value));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(SetGlobalVariable));
    }

    public AudioCategory GetCategory(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        CnaHandle category = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            name, view => Native.cna_audio_engine_get_category(NativeHandle, view, out category));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetCategory));
        return new AudioCategory(category.AsNint);
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
