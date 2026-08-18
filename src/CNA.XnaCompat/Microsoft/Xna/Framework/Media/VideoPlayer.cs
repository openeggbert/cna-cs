namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>VideoPlayer</c>. A pure subclass -- playback control and the
/// scalar properties are inherited unchanged; <see cref="GetTexture"/> and <see cref="Video"/>
/// need re-typing, and the frame-texture factory is overridden so each frame comes back as this
/// namespace's <see cref="Graphics.Texture2D"/>.</summary>
public class VideoPlayer : CNA.Media.VideoPlayer
{
    public new Video? Video => (Video?)base.Video;

    public new MediaState State => (MediaState)(int)base.State;

    /// <summary>Narrows the base's <c>Texture</c> back to XNA's own <c>Texture2D</c> -- see the
    /// base method's doc comment for why it cannot be typed that way itself. The cast is safe
    /// because <see cref="CreateFrameTexture"/> below is the only thing that builds it.</summary>
    public new Graphics.Texture2D? GetTexture() => (Graphics.Texture2D?)base.GetTexture();

    public void Play(Video video) => base.Play(video);

    protected override CNA.Graphics.Texture CreateFrameTexture(CNA.Graphics.GraphicsDevice graphicsDevice, nint nativeHandleValue) =>
        new Graphics.Texture2D((Graphics.GraphicsDevice)graphicsDevice, nativeHandleValue);
}
