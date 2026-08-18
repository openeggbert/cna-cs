using CNA.Interop;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>Viewport</c> surface. Mirrors the real, shipped
/// openeggbert/cna C API's own <c>CNA_Viewport</c> field-for-field (<c>graphics_device.h:59-77</c>);
/// only <see cref="X"/>/<see cref="Y"/>/<see cref="Width"/>/<see cref="Height"/>/
/// <see cref="MinDepth"/>/<see cref="MaxDepth"/> cross the ABI.
///
/// The derived members are managed, per design invariant #3: they are arithmetic over those six
/// fields, and the ABI does expose them (<c>cna_viewport_project</c> and friends,
/// <c>graphics_device.h:86-231</c>) but crossing the boundary for a matrix multiply costs more than
/// the multiply. Each is ported from the engine's own <c>Viewport.cpp</c> rather than reconstructed,
/// which matters most for <see cref="TitleSafeArea"/> -- see its own note.
///
/// They were absent until a sweep of unbound header functions found them, under a doc comment
/// saying "no current caller in this project needs them". That is precisely the reasoning the
/// complete-XNA-4.0 mandate retired.</summary>
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

    /// <summary>Width divided by height, or zero when either is zero -- the engine returns zero
    /// rather than dividing, and a caller feeding this into a projection matrix wants the same
    /// answer.</summary>
    public readonly float AspectRatio => Height != 0 && Width != 0 ? (float)Width / Height : 0f;

    /// <summary>The viewport as a rectangle. The setter preserves
    /// <see cref="MinDepth"/>/<see cref="MaxDepth"/>, matching the engine.</summary>
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

    /// <summary>
    /// The subset guaranteed visible on lower-quality displays.
    ///
    /// Equal to <see cref="Bounds"/> here, which is worth stating because it is not what XNA's
    /// Xbox-era implementation did -- that inset each edge by 5% for CRT overscan. This engine
    /// returns the full bounds (<c>Viewport::getTitleSafeAreaProperty</c> is literally
    /// <c>getBoundsProperty()</c>, and its own test asserts the two are equal), and reproducing the
    /// canonical behaviour beats reproducing a remembered one.
    /// </summary>
    public readonly Rectangle TitleSafeArea => Bounds;

    /// <summary>
    /// Projects a world-space point into screen space.
    ///
    /// The perspective divide is applied only when <c>w</c> is not already 1, which is what makes an
    /// orthographic projection pass through unchanged -- and it divides by the <c>w</c> computed
    /// from the *source* point against the combined matrix, not from the transformed result.
    /// </summary>
    public readonly Vector3 Project(Vector3 source, Matrix projection, Matrix view, Matrix world)
    {
        Matrix matrix = world * view * projection;
        Vector3 vector = Vector3.Transform(source, matrix);

        float w = (source.X * matrix.M14) + (source.Y * matrix.M24) + (source.Z * matrix.M34) + matrix.M44;
        if (!MathHelper.WithinEpsilon(w, 1f))
        {
            vector /= w;
        }

        return new Vector3(
            ((vector.X + 1f) * 0.5f * Width) + X,
            ((-vector.Y + 1f) * 0.5f * Height) + Y,
            (vector.Z * (MaxDepth - MinDepth)) + MinDepth);
    }

    /// <summary>
    /// Unprojects a screen-space point back into world space -- the inverse of
    /// <see cref="Project"/>, and how a game turns a cursor position into a pick ray.
    ///
    /// The <c>w</c> divide uses the *rescaled* source, not the original argument, which is the one
    /// place this is easy to get subtly wrong: the rescale happens before the transform and the
    /// divisor is computed from the rescaled values.
    /// </summary>
    public readonly Vector3 Unproject(Vector3 source, Matrix projection, Matrix view, Matrix world)
    {
        Matrix matrix = Matrix.Invert(world * view * projection);

        var rescaled = new Vector3(
            ((source.X - X) / Width * 2f) - 1f,
            -(((source.Y - Y) / Height * 2f) - 1f),
            (source.Z - MinDepth) / (MaxDepth - MinDepth));

        Vector3 vector = Vector3.Transform(rescaled, matrix);

        float w = (rescaled.X * matrix.M14) + (rescaled.Y * matrix.M24) + (rescaled.Z * matrix.M34) + matrix.M44;
        if (!MathHelper.WithinEpsilon(w, 1f))
        {
            vector /= w;
        }

        return vector;
    }

    public override readonly string ToString() =>
        $"{{X:{X} Y:{Y} Width:{Width} Height:{Height} MinDepth:{MinDepth} MaxDepth:{MaxDepth}}}";

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
