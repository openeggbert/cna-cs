namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible video-player facade.</summary>
public sealed class VideoPlayer : IDisposable
{
    private readonly CNA.Media.VideoPlayer _player = new();
    private Video? _video;
    private bool _disposed;

    ~VideoPlayer()
    {
        Dispose(false);
    }

    public bool IsDisposed => _disposed || _player.IsDisposed;

    public Video? Video => _video;

    public MediaState State => (MediaState)(int)_player.State;

    public bool IsLooped
    {
        get => _player.IsLooped;
        set => _player.IsLooped = value;
    }

    public bool IsMuted
    {
        get => _player.IsMuted;
        set => _player.IsMuted = value;
    }

    public float Volume
    {
        get => _player.Volume;
        set => _player.Volume = value;
    }

    public TimeSpan PlayPosition => _player.PlayPosition;

    public void Play(Video video)
    {
        ArgumentNullException.ThrowIfNull(video);
        _player.Play(video.Framework);
        _video = video;
    }

    public void Pause() => _player.Pause();

    public void Resume() => _player.Resume();

    public void Stop() => _player.Stop();

    public Graphics.Texture2D? GetTexture()
    {
        CNA.Graphics.Texture? frame = _player.GetTexture();
        if (frame is not CNA.Graphics.Texture2D texture || _video is null)
        {
            return null;
        }

        return new Graphics.Texture2D(_video.GraphicsDevice, texture);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        _ = disposing;
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _player?.Dispose();
    }
}
