namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible <c>Cue</c>. A thin re-typing wrapper rather than a subclass,
/// because <see cref="CNA.Audio.Cue"/>'s only constructor is internal (cues come from a sound
/// bank lookup).</summary>
public class Cue : IDisposable
{
    private readonly CNA.Audio.Cue _cue;

    internal Cue(CNA.Audio.Cue cue)
    {
        _cue = cue;
    }

    public string Name => _cue.Name;

    public bool IsCreated => _cue.IsCreated;

    public bool IsDisposed => _cue.IsDisposed;

    public bool IsPaused => _cue.IsPaused;

    public bool IsPlaying => _cue.IsPlaying;

    public bool IsPrepared => _cue.IsPrepared;

    public bool IsPreparing => _cue.IsPreparing;

    public bool IsStopped => _cue.IsStopped;

    public bool IsStopping => _cue.IsStopping;

    public void Play() => _cue.Play();

    public void Pause() => _cue.Pause();

    public void Resume() => _cue.Resume();

    public void Stop(AudioStopOptions options) => _cue.Stop((CNA.Audio.AudioStopOptions)(int)options);

    public void Apply3D(AudioListener listener, AudioEmitter emitter) => _cue.Apply3D(listener, emitter);

    public float GetVariable(string name) => _cue.GetVariable(name);

    public void SetVariable(string name, float value) => _cue.SetVariable(name, value);

    public void Dispose()
    {
        _cue.Dispose();
        GC.SuppressFinalize(this);
    }
}
