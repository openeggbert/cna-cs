namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>Viewport</c>. See <see cref="Color"/>'s own doc comment for why
/// this duplicates <see cref="CNA.Graphics.Viewport"/> rather than subclassing it (structs cannot
/// inherit).</summary>
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

    internal readonly CNA.Graphics.Viewport ToNative() => new(X, Y, Width, Height, MinDepth, MaxDepth);

    internal static Viewport FromNative(CNA.Graphics.Viewport native) =>
        new(native.X, native.Y, native.Width, native.Height, native.MinDepth, native.MaxDepth);
}
