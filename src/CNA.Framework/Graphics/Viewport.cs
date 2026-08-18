using CNA.Interop;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>Viewport</c> surface. Mirrors the real, shipped
/// openeggbert/cna C API's own <c>CNA_Viewport</c> field-for-field (<c>graphics_device.h:59-77</c>);
/// only <see cref="X"/>/<see cref="Y"/>/<see cref="Width"/>/<see cref="Height"/>/
/// <see cref="MinDepth"/>/<see cref="MaxDepth"/> are implemented -- real XNA's derived helpers
/// (<c>AspectRatio</c>, <c>Bounds</c>, <c>TitleSafeArea</c>, <c>Project</c>/<c>Unproject</c>) all
/// have real native counterparts (<c>cna_viewport_get_aspect_ratio</c> etc.,
/// <c>graphics_device.h:86-231</c>) but no current caller in this project needs them.</summary>
public struct Viewport
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public float MinDepth;
    public float MaxDepth;

    public Viewport(int x, int y, int width, int height)
        : this(x, y, width, height, 0f, 1f)
    {
    }

    public Viewport(int x, int y, int width, int height, float minDepth, float maxDepth)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        MinDepth = minDepth;
        MaxDepth = maxDepth;
    }

    internal readonly CnaViewport ToNative() => new()
    {
        X = X,
        Y = Y,
        Width = Width,
        Height = Height,
        MinDepth = MinDepth,
        MaxDepth = MaxDepth,
    };

    internal static Viewport FromNative(CnaViewport native) =>
        new(native.X, native.Y, native.Width, native.Height, native.MinDepth, native.MaxDepth);
}
