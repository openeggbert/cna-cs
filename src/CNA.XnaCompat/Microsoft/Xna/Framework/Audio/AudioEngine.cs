using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible facade over an authored-audio engine.</summary>
public class AudioEngine : IDisposable
{
    private readonly CNA.Audio.AudioEngine _engine;
    private bool _isDisposed;

    public const int ContentVersion = 39;

    public AudioEngine(string settingsFile)
        : this(settingsFile, TimeSpan.FromMilliseconds(250), string.Empty)
    {
    }

    public AudioEngine(string settingsFile, TimeSpan lookAheadTime, string rendererId)
    {
        ArgumentNullException.ThrowIfNull(settingsFile);
        _ = lookAheadTime;
        _ = rendererId;
        _engine = new CNA.Audio.AudioEngine(settingsFile);
    }

    ~AudioEngine()
    {
        Dispose(false);
    }

    internal CNA.Audio.AudioEngine Framework => _engine;

    public ReadOnlyCollection<RendererDetail> RendererDetails
    {
        get
        {
            IReadOnlyList<CNA.Audio.RendererDetail> source = _engine.RendererDetails;
            var details = new RendererDetail[source.Count];
            for (int i = 0; i < details.Length; i++)
            {
                details[i] = new RendererDetail(source[i]);
            }

            return Array.AsReadOnly(details);
        }
    }

    public bool IsDisposed => _isDisposed || _engine.IsDisposed;

    public event EventHandler<EventArgs>? Disposing;

    public AudioCategory GetCategory(string name) => new(this, _engine.GetCategory(name));

    public float GetGlobalVariable(string name) => _engine.GetGlobalVariable(name);

    public void SetGlobalVariable(string name, float value) => _engine.SetGlobalVariable(name, value);

    public void Update() => _engine.Update();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _engine?.Dispose();
        if (disposing)
        {
            Disposing?.Invoke(this, EventArgs.Empty);
        }
    }
}
