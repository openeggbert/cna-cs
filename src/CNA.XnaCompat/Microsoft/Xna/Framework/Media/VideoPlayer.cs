namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible video-player facade.</summary>
public sealed class VideoPlayer : IDisposable
{
    private readonly CNA.Media.VideoPlayer _player = new();
    private Video? _video;
    // ParentOwned/transient. The CNA ABI invalidates this borrowed alias on the next call made on
    // the player. Keep at most one facade live and mark it disposed before crossing that boundary;
    // it must never release the player's actual frame texture.
    private Graphics.Texture2D? _frameTexture;
    private bool _isLooped;
    private bool _isMuted;
    private float _volume = 1f;
    private bool _disposed;

    ~VideoPlayer()
    {
        Dispose(false);
    }

    public bool IsDisposed
    {
        get
        {
            if (_disposed)
            {
                return true;
            }

            InvalidateFrameTexture();
            return _player.IsDisposed;
        }
    }

    public Video? Video => _video;

    public MediaState State
    {
        get
        {
            ThrowIfDisposed();
            InvalidateFrameTexture();
            return (MediaState)(int)_player.State;
        }
    }

    public bool IsLooped
    {
        get => _isLooped;
        set
        {
            ThrowIfDisposed();
            if (value == _isLooped)
            {
                return;
            }

            InvalidateFrameTexture();
            _player.IsLooped = value;
            _isLooped = value;
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            ThrowIfDisposed();
            if (value == _isMuted)
            {
                return;
            }

            InvalidateFrameTexture();
            _player.IsMuted = value;
            _isMuted = value;
        }
    }

    public float Volume
    {
        get => _volume;
        set
        {
            ThrowIfDisposed();
            if (value == _volume)
            {
                return;
            }

            // XNA's unsigned floating-point comparison accepts NaN but rejects finite values
            // outside [0,1]. Preserve that unusual ordering exactly.
            if (value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            InvalidateFrameTexture();
            _player.Volume = value;
            _volume = value;
        }
    }

    public TimeSpan PlayPosition
    {
        get
        {
            ThrowIfDisposed();
            InvalidateFrameTexture();
            return _player.PlayPosition;
        }
    }

    public void Play(Video video)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(video);
        InvalidateFrameTexture();
        _player.Play(video.Framework);
        _video = video;
    }

    public void Pause()
    {
        ThrowIfDisposed();
        InvalidateFrameTexture();
        _player.Pause();
    }

    public void Resume()
    {
        ThrowIfDisposed();
        InvalidateFrameTexture();
        _player.Resume();
    }

    public void Stop()
    {
        ThrowIfDisposed();
        InvalidateFrameTexture();
        _player.Stop();
    }

    public Graphics.Texture2D? GetTexture()
    {
        ThrowIfDisposed();
        InvalidateFrameTexture();
        if (_video is null)
        {
            throw new InvalidOperationException("No video has been played.");
        }

        CNA.Graphics.Texture? frame = _player.GetTexture();
        if (frame is not CNA.Graphics.Texture2D texture)
        {
            return null;
        }

        _frameTexture = new Graphics.Texture2D(_video.GraphicsDevice, texture);
        return _frameTexture;
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
        InvalidateFrameTexture();
        _player?.Dispose();
    }

    private void InvalidateFrameTexture()
    {
        if (_frameTexture is not null)
        {
            _frameTexture.Dispose();
            _frameTexture = null;
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
