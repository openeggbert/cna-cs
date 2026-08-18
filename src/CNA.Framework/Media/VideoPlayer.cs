using CNA.Graphics;
using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// Matches real XNA's <c>VideoPlayer</c>: plays a <see cref="Video"/> and hands each decoded frame
/// back as a texture through <see cref="GetTexture"/>, which the game draws itself (typically with
/// <see cref="SpriteBatch"/>) -- XNA never composites video for you.
///
/// Created against the *game*, not a graphics device (<c>cna_video_player_create</c> takes a game
/// handle), which is why the constructor takes no argument and uses the ambient game handle, the
/// same way <c>Keyboard</c>/<c>Mouse</c>/<c>TouchPanel</c> do.
/// </summary>
public class VideoPlayer : IDisposable
{
    private readonly NativeResourceHandle _handle;
    private Video? _video;

    public VideoPlayer()
    {
        CnaResult result = Native.cna_video_player_create(CnaAmbientGame.Current, out CnaHandle player);
        CnaException.ThrowIfFailed(result, nameof(VideoPlayer));

        _handle = new NativeResourceHandle(player.AsNint, h => Native.cna_video_player_destroy(new CnaHandle(h)));
    }

    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    /// <summary>The video currently loaded, or <see langword="null"/> before the first
    /// <see cref="Play"/>. Tracked managed-side: the native getter reports a handle this project
    /// cannot map back to the managed <see cref="Video"/> wrapper that owns it -- the same
    /// limitation <c>TextureCollection</c> documents -- so returning the object the caller passed
    /// to <see cref="Play"/> is both correct and what XNA callers expect (reference
    /// equality).</summary>
    public Video? Video => _video;

    public MediaState State
    {
        get
        {
            CnaResult result = Native.cna_video_player_get_state(NativeHandle, out uint value);
            CnaException.ThrowIfFailed(result, nameof(State));
            return (MediaState)value;
        }
    }

    public bool IsLooped
    {
        get
        {
            CnaResult result = Native.cna_video_player_get_is_looped(NativeHandle, out byte value);
            CnaException.ThrowIfFailed(result, nameof(IsLooped));
            return value != 0;
        }
        set
        {
            CnaResult result = Native.cna_video_player_set_is_looped(NativeHandle, (byte)(value ? 1 : 0));
            CnaException.ThrowIfFailed(result, nameof(IsLooped));
        }
    }

    public bool IsMuted
    {
        get
        {
            CnaResult result = Native.cna_video_player_get_is_muted(NativeHandle, out byte value);
            CnaException.ThrowIfFailed(result, nameof(IsMuted));
            return value != 0;
        }
        set
        {
            CnaResult result = Native.cna_video_player_set_is_muted(NativeHandle, (byte)(value ? 1 : 0));
            CnaException.ThrowIfFailed(result, nameof(IsMuted));
        }
    }

    public float Volume
    {
        get
        {
            CnaResult result = Native.cna_video_player_get_volume(NativeHandle, out float value);
            CnaException.ThrowIfFailed(result, nameof(Volume));
            return value;
        }
        set
        {
            CnaResult result = Native.cna_video_player_set_volume(NativeHandle, value);
            CnaException.ThrowIfFailed(result, nameof(Volume));
        }
    }

    public TimeSpan PlayPosition
    {
        get
        {
            CnaResult result = Native.cna_video_player_get_play_position_ticks(NativeHandle, out long ticks);
            CnaException.ThrowIfFailed(result, nameof(PlayPosition));
            return TimeSpan.FromTicks(ticks);
        }
    }

    public void Play(Video video)
    {
        ArgumentNullException.ThrowIfNull(video);
        CnaResult result = Native.cna_video_player_play(NativeHandle, video.NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Play));
        _video = video;
    }

    public void Stop()
    {
        CnaResult result = Native.cna_video_player_stop(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Stop));
    }

    public void Pause()
    {
        CnaResult result = Native.cna_video_player_pause(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Pause));
    }

    public void Resume()
    {
        CnaResult result = Native.cna_video_player_resume(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Resume));
    }

    /// <summary>
    /// The current frame, or <see langword="null"/> when none is available yet (before playback
    /// starts, or between decoded frames). The C API reports availability as a separate flag from
    /// the handle precisely so this case is distinguishable, so this returns <see langword="null"/>
    /// rather than throwing -- real XNA instead throws <c>InvalidOperationException</c> when
    /// stopped, but a null is the more useful shape for a per-frame draw loop and cannot be
    /// mistaken for a valid texture.
    ///
    /// The returned texture wraps a handle the *player* owns, so it must not be disposed by the
    /// caller and must not be cached across frames.
    ///
    /// Returns the <see cref="Texture"/> base rather than <see cref="Texture2D"/>, unlike real
    /// XNA. That is forced by WP3's texture reparenting: <c>CNA.Graphics.Texture2D</c> and
    /// <c>Microsoft.Xna.Framework.Graphics.Texture2D</c> are now siblings under
    /// <see cref="Texture"/>, not one deriving from the other, so no single <c>Texture2D</c> type
    /// can be the return type both namespaces need. CNA.XnaCompat's own <c>VideoPlayer</c> narrows
    /// this back to its <c>Texture2D</c>, so XNA-shaped game code is unaffected.
    /// </summary>
    public Texture? GetTexture()
    {
        CnaResult result = Native.cna_video_player_get_texture(NativeHandle, out CnaHandle texture, out byte available);
        CnaException.ThrowIfFailed(result, nameof(GetTexture));

        // _video is non-null whenever native reports a frame: a frame only exists after Play(),
        // which is the only thing that sets it.
        if (available == 0 || _video is null)
        {
            return null;
        }

        // The device comes from the Video the caller handed to Play() -- deliberately not a fresh
        // wrapper around the ambient game's device, which would give the frame texture a second
        // GraphicsDevice object with its own cached state (see Video.GraphicsDevice).
        return CreateFrameTexture(_video!.GraphicsDevice, texture.AsNint);
    }

    /// <summary>Covariant-return factory hook so CNA.XnaCompat can hand back its own
    /// <c>Texture2D</c> -- same pattern as <c>GraphicsDevice.QueryBlendState</c>.</summary>
    protected virtual Texture CreateFrameTexture(GraphicsDevice graphicsDevice, nint nativeHandleValue) =>
        new Texture2D(graphicsDevice, nativeHandleValue);

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
