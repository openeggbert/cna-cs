namespace Microsoft.Xna.Framework.Graphics;

public class DisplayMode
{
    private readonly int _width;
    private readonly int _height;
    private readonly SurfaceFormat _format;

    internal DisplayMode(int width, int height, SurfaceFormat format)
    {
        _width = width;
        _height = height;
        _format = format;
    }

    public SurfaceFormat Format => _format;

    public int Height => _height;

    public int Width => _width;

    public float AspectRatio => _height == 0 || _width == 0 ? 0f : (float)_width / _height;

    public Rectangle TitleSafeArea =>
        new(_width / 10, _height / 10, _width - (_width / 5), _height - (_height / 5));

    internal static DisplayMode FromFramework(CNA.Graphics.DisplayMode source) =>
        new(source.Width, source.Height, (SurfaceFormat)(int)source.Format);

    public override string ToString() =>
        $"{{Width:{Width} Height:{Height} Format:{Format} AspectRatio:{AspectRatio}}}";
}
