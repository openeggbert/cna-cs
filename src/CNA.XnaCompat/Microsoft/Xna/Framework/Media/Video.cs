namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Video</c>. A pure subclass -- <c>Width</c>/<c>Height</c>/
/// <c>FramesPerSecond</c>/<c>Duration</c>/<c>Dispose</c> are inherited unchanged from
/// <see cref="CNA.Media.Video"/>; only the members whose types differ per namespace are
/// re-typed.</summary>
public class Video : CNA.Media.Video
{
    public Video(Graphics.GraphicsDevice graphicsDevice, string fileName)
        : base(graphicsDevice, fileName)
    {
    }

    public new Graphics.GraphicsDevice GraphicsDevice => (Graphics.GraphicsDevice)base.GraphicsDevice;

    public new VideoSoundtrackType VideoSoundtrackType => (VideoSoundtrackType)(int)base.VideoSoundtrackType;
}
