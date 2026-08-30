namespace Microsoft.Xna.Framework.Media;

/// <summary>Represents a video asset loaded for an XNA graphics device.</summary>
public sealed class Video
{
    private readonly CNA.Media.Video _video;
    private readonly Graphics.GraphicsDevice _graphicsDevice;

    /// <summary>Construction from a compiled asset's own metadata, for
    /// <c>Microsoft.Xna.Framework.Content.VideoContentReader</c>. Internal because XNA's
    /// <c>Video</c> has no public constructor: a video comes from content.</summary>
    internal Video(
        Graphics.GraphicsDevice graphicsDevice,
        string fileName,
        int durationMilliseconds,
        int width,
        int height,
        float framesPerSecond,
        VideoSoundtrackType soundtrackType)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        _graphicsDevice = graphicsDevice;
        _video = new CNA.Media.Video(
            graphicsDevice.Framework, fileName, durationMilliseconds, width, height, framesPerSecond,
            (CNA.Media.VideoSoundtrackType)(int)soundtrackType);
    }

    internal Video(Graphics.GraphicsDevice graphicsDevice, string fileName)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        _graphicsDevice = graphicsDevice;
        _video = new CNA.Media.Video(graphicsDevice.Framework, fileName);
    }

    internal CNA.Media.Video Framework => _video;

    internal Graphics.GraphicsDevice GraphicsDevice => _graphicsDevice;

    public TimeSpan Duration => _video.Duration;

    public int Width => _video.Width;

    public int Height => _video.Height;

    public float FramesPerSecond => _video.FramesPerSecond;

    public VideoSoundtrackType VideoSoundtrackType => (VideoSoundtrackType)(int)_video.VideoSoundtrackType;
}
