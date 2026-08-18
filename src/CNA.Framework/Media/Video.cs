using CNA.Graphics;
using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// Matches real XNA's <c>Video</c>: a decodable video asset, played through
/// <see cref="VideoPlayer"/>.
///
/// Created against a <see cref="GraphicsDevice"/> because decoding produces frames as a texture --
/// which is also why <see cref="VideoPlayer.GetTexture"/> exists at all. In real XNA a
/// <c>Video</c> comes from <c>ContentManager.Load&lt;Video&gt;</c>; the C API's own creation route
/// takes a file name directly (<c>cna_video_create</c>), so that constructor is exposed here too
/// rather than making content loading the only way in.
/// </summary>
public class Video : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public Video(GraphicsDevice graphicsDevice, string fileName)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(fileName);

        GraphicsDevice = graphicsDevice;

        CnaHandle video = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            fileName, view => Native.cna_video_create(graphicsDevice.ResolveNativeDeviceHandle(), view, out video));
        CnaException.ThrowIfFailed(result, nameof(Video));

        _handle = new NativeResourceHandle(video.AsNint, h => Native.cna_video_destroy(new CnaHandle(h)));
    }

    internal Video(GraphicsDevice graphicsDevice, nint nativeHandleValue)
    {
        GraphicsDevice = graphicsDevice;
        _handle = new NativeResourceHandle(nativeHandleValue, h => Native.cna_video_destroy(new CnaHandle(h)));
    }

    /// <summary>The device this video decodes into. Held so <see cref="VideoPlayer.GetTexture"/>
    /// can attach each frame texture to the caller's *own* device wrapper rather than building a
    /// second one around the same native device -- two wrappers would each carry their own cached
    /// <c>BlendState</c>/<c>Indices</c>/sampler state and could disagree, which is the same reason
    /// <see cref="GraphicsDeviceManager.GraphicsDevice"/> reuses the game's.</summary>
    public GraphicsDevice GraphicsDevice { get; }

    internal CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public int Width
    {
        get
        {
            CnaResult result = Native.cna_video_get_width(NativeHandle, out int value);
            CnaException.ThrowIfFailed(result, nameof(Width));
            return value;
        }
    }

    public int Height
    {
        get
        {
            CnaResult result = Native.cna_video_get_height(NativeHandle, out int value);
            CnaException.ThrowIfFailed(result, nameof(Height));
            return value;
        }
    }

    public float FramesPerSecond
    {
        get
        {
            CnaResult result = Native.cna_video_get_frames_per_second(NativeHandle, out float value);
            CnaException.ThrowIfFailed(result, nameof(FramesPerSecond));
            return value;
        }
    }

    public TimeSpan Duration
    {
        get
        {
            CnaResult result = Native.cna_video_get_duration(NativeHandle, out long ticks);
            CnaException.ThrowIfFailed(result, nameof(Duration));
            return TimeSpan.FromTicks(ticks);
        }
    }

    public VideoSoundtrackType VideoSoundtrackType
    {
        get
        {
            CnaResult result = Native.cna_video_get_soundtrack_type(NativeHandle, out uint value);
            CnaException.ThrowIfFailed(result, nameof(VideoSoundtrackType));
            return (VideoSoundtrackType)value;
        }
    }

    /// <summary>The source file this video was created from. Not part of real XNA's <c>Video</c>
    /// surface, but the C API reports it and there is no other way for a caller to identify a
    /// video instance.</summary>
    public unsafe string FileName => NativeStringReader.Read(
        Native.cna_video_get_file_name_size, Native.cna_video_copy_file_name, NativeHandle, nameof(FileName));

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
