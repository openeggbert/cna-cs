namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>Viewport</c>. See <see cref="Color"/>'s own doc comment for why
/// this duplicates <see cref="CNA.Graphics.Viewport"/> rather than subclassing it (structs cannot
/// inherit). The derived members delegate rather than repeating the arithmetic, so there is one
/// definition of each -- and the compat <c>Vector3</c>/<c>Matrix</c>/<c>Rectangle</c> convert
/// implicitly at the boundary.</summary>
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

    /// <summary>Width over height, or zero when either is zero.</summary>
    public readonly float AspectRatio => ToNative().AspectRatio;

    public Rectangle Bounds
    {
        readonly get => new(X, Y, Width, Height);
        set
        {
            X = value.X;
            Y = value.Y;
            Width = value.Width;
            Height = value.Height;
        }
    }

    /// <summary>Equal to <see cref="Bounds"/> -- see
    /// <see cref="CNA.Graphics.Viewport.TitleSafeArea"/> for why that is the canonical answer rather
    /// than XNA's Xbox-era overscan inset.</summary>
    public readonly Rectangle TitleSafeArea => Bounds;

    /// <summary>Projects a world-space point into screen space. Delegates rather than repeating the
    /// arithmetic, so there is one definition -- the compat matrices and vectors convert
    /// implicitly.</summary>
    public readonly Vector3 Project(Vector3 source, Matrix projection, Matrix view, Matrix world) =>
        ToNative().Project(source, projection, view, world);

    /// <summary>Unprojects a screen-space point back into world space. See
    /// <see cref="Project"/>.</summary>
    public readonly Vector3 Unproject(Vector3 source, Matrix projection, Matrix view, Matrix world) =>
        ToNative().Unproject(source, projection, view, world);

    public override readonly string ToString() =>
        $"{{X:{X} Y:{Y} Width:{Width} Height:{Height} MinDepth:{MinDepth} MaxDepth:{MaxDepth}}}";

    internal readonly CNA.Graphics.Viewport ToNative() => new(X, Y, Width, Height, MinDepth, MaxDepth);

    internal static Viewport FromNative(CNA.Graphics.Viewport native) =>
        new(native.X, native.Y, native.Width, native.Height, native.MinDepth, native.MaxDepth);
}
