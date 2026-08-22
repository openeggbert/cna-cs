namespace Microsoft.Xna.Framework;

/// <summary>
/// Keeps implementation-value conversion available inside the compatibility assembly without
/// adding CNA-typed operators to the public XNA metadata contract.
/// </summary>
internal static class FrameworkValueConversions
{
    internal static BoundingBox ToCompat(this CNA.BoundingBox value) => BoundingBox.FromFramework(value);

    internal static BoundingSphere ToCompat(this CNA.BoundingSphere value) => BoundingSphere.FromFramework(value);

    internal static Color ToCompat(this CNA.Color value) => Color.FromFramework(value);

    internal static Matrix ToCompat(this CNA.Matrix value) => Matrix.FromFramework(value);

    internal static Plane ToCompat(this CNA.Plane value) => Plane.FromFramework(value);

    internal static Point ToCompat(this CNA.Point value) => Point.FromFramework(value);

    internal static Quaternion ToCompat(this CNA.Quaternion value) => Quaternion.FromFramework(value);

    internal static Ray ToCompat(this CNA.Ray value) => Ray.FromFramework(value);

    internal static Rectangle ToCompat(this CNA.Rectangle value) => Rectangle.FromFramework(value);

    internal static Vector2 ToCompat(this CNA.Vector2 value) => Vector2.FromFramework(value);

    internal static Vector3 ToCompat(this CNA.Vector3 value) => Vector3.FromFramework(value);

    internal static Vector4 ToCompat(this CNA.Vector4 value) => Vector4.FromFramework(value);

    internal static CNA.Matrix? ToFramework(this Matrix? value) => value?.ToFramework();

    internal static CNA.Rectangle? ToFramework(this Rectangle? value) => value?.ToFramework();
}
