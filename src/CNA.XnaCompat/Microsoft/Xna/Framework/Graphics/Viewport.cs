namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>Viewport</c>. See <see cref="Color"/>'s own doc comment for why
/// this duplicates <see cref="CNA.Graphics.Viewport"/> rather than subclassing it (structs cannot
/// inherit). The projection methods intentionally use the strict facade's matrix and vector
/// arithmetic because XNA's fixed matrix inversion and reciprocal-first vector division have
/// observable IEEE behavior that differs from the CNA implementation API.</summary>
public struct Viewport
{
    public int X { readonly get; set; }
    public int Y { readonly get; set; }
    public int Width { readonly get; set; }
    public int Height { readonly get; set; }
    public float MinDepth { readonly get; set; }
    public float MaxDepth { readonly get; set; }

    public Viewport(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        MinDepth = 0f;
        MaxDepth = 1f;
    }

    public Viewport(Rectangle bounds)
        : this(bounds.X, bounds.Y, bounds.Width, bounds.Height)
    {
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

    /// <summary>Projects a world-space point into screen space.</summary>
    public readonly Vector3 Project(Vector3 source, Matrix projection, Matrix view, Matrix world)
    {
        Matrix matrix = Matrix.Multiply(world, view);
        matrix = Matrix.Multiply(matrix, projection);
        Vector3 result = Vector3.Transform(source, matrix);
        float w = (source.X * matrix.M14) +
            (source.Y * matrix.M24) +
            (source.Z * matrix.M34) +
            matrix.M44;
        if (!WithinEpsilon(w, 1f))
        {
            result /= w;
        }

        result.X = ((result.X + 1f) * 0.5f * Width) + X;
        result.Y = ((-result.Y + 1f) * 0.5f * Height) + Y;
        result.Z = (result.Z * (MaxDepth - MinDepth)) + MinDepth;
        return result;
    }

    /// <summary>Unprojects a screen-space point back into world space.</summary>
    public readonly Vector3 Unproject(Vector3 source, Matrix projection, Matrix view, Matrix world)
    {
        Matrix matrix = Matrix.Multiply(world, view);
        matrix = Matrix.Multiply(matrix, projection);
        matrix = Matrix.Invert(matrix);
        source.X = ((source.X - X) / Width * 2f) - 1f;
        source.Y = -(((source.Y - Y) / Height * 2f) - 1f);
        source.Z = (source.Z - MinDepth) / (MaxDepth - MinDepth);
        Vector3 result = Vector3.Transform(source, matrix);
        float w = (source.X * matrix.M14) +
            (source.Y * matrix.M24) +
            (source.Z * matrix.M34) +
            matrix.M44;
        if (!WithinEpsilon(w, 1f))
        {
            result /= w;
        }

        return result;
    }

    public override readonly string ToString() =>
        $"{{X:{X} Y:{Y} Width:{Width} Height:{Height} MinDepth:{MinDepth} MaxDepth:{MaxDepth}}}";

    internal readonly CNA.Graphics.Viewport ToNative() => new(X, Y, Width, Height, MinDepth, MaxDepth);

    internal static Viewport FromNative(CNA.Graphics.Viewport native) => new(native.X, native.Y, native.Width, native.Height)
    {
        MinDepth = native.MinDepth,
        MaxDepth = native.MaxDepth,
    };

    private static bool WithinEpsilon(float value1, float value2)
    {
        float difference = value1 - value2;
        return -float.Epsilon <= difference && difference <= float.Epsilon;
    }
}
