namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>VideoPlayer</c>. A pure subclass -- playback control and the
/// scalar properties are inherited unchanged; <see cref="GetTexture"/> and <see cref="Video"/>
/// need re-typing. Each borrowed CNA frame is wrapped in this namespace's
/// <see cref="Graphics.Texture2D"/> without adopting its native handle.</summary>
public class VideoPlayer : CNA.Media.VideoPlayer
{
    public new Video? Video => (Video?)base.Video;

    public new MediaState State => (MediaState)(int)base.State;

    /// <summary>Narrows the base's <c>Texture</c> back to XNA's own <c>Texture2D</c> -- see the
    /// base method's doc comment for why it cannot be typed that way itself.</summary>
    public new Graphics.Texture2D? GetTexture()
    {
        CNA.Graphics.Texture? frame = base.GetTexture();
        if (frame is not CNA.Graphics.Texture2D texture)
        {
            return null;
        }

        Graphics.GraphicsDevice device = Graphics.GraphicsDevice.FromFramework(texture.GraphicsDevice)
            ?? throw new InvalidOperationException("The video frame has no XNA facade graphics device.");
        return new Graphics.Texture2D(device, texture);
    }

    public void Play(Video video) => base.Play(video);

}
