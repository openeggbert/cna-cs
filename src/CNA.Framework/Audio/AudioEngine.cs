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
    /// <summary>
    /// Every audio renderer this engine describes. Matches real XNA's <c>RendererDetails</c>.
    ///
    /// Re-enumerated on every read rather than cached: the ABI addresses a renderer by index into a
    /// list that reflects the devices present now, so a cached array would go stale the moment one
    /// is plugged in or removed. The header notes this engine currently describes exactly one
    /// renderer -- a fact about the backend, not a shape this property assumes.
    /// </summary>
    public unsafe IReadOnlyList<RendererDetail> RendererDetails
    {
        get
        {
            CnaResult countResult = Native.cna_audio_engine_get_renderer_count(NativeHandle, out ulong count);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(countResult, nameof(RendererDetails));

            var details = new RendererDetail[count];
            for (ulong i = 0; i < count; i++)
            {
                string friendlyName = NativeStringReader.ReadIndexed(
                    Native.cna_audio_engine_get_renderer_friendly_name_size,
                    Native.cna_audio_engine_copy_renderer_friendly_name,
                    NativeHandle, i, nameof(RendererDetails));

                string rendererId = NativeStringReader.ReadIndexed(
                    Native.cna_audio_engine_get_renderer_id_size,
                    Native.cna_audio_engine_copy_renderer_id,
                    NativeHandle, i, nameof(RendererDetails));

                CnaResult hashResult = Native.cna_audio_engine_get_renderer_hash_code(NativeHandle, i, out int hash);
                GC.KeepAlive(this);
                CnaException.ThrowIfFailed(hashResult, nameof(RendererDetails));

                details[i] = new RendererDetail(friendlyName, rendererId, hash);
            }

            return details;
        }
    }

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
