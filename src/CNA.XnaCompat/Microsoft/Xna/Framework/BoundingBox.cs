using System.Linq;

namespace Microsoft.Xna.Framework;

public struct BoundingBox : IEquatable<BoundingBox>
{
    public const int CornerCount = 8;

    public Vector3 Min;
    public Vector3 Max;

    public BoundingBox(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public readonly Vector3[] GetCorners()
    {
        CNA.Vector3[] source = ((CNA.BoundingBox)this).GetCorners();
        var result = new Vector3[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = source[i];
        }

        return result;
    }

    public readonly bool Contains(Vector3 point) => ((CNA.BoundingBox)this).Contains(point);

    public readonly ContainmentType Contains(BoundingBox box) => ((CNA.BoundingBox)this).Contains(box).ToCompat();

    public readonly ContainmentType Contains(BoundingSphere sphere) => ((CNA.BoundingBox)this).Contains(sphere).ToCompat();

    public readonly bool Intersects(BoundingBox box) => ((CNA.BoundingBox)this).Intersects(box);

    public readonly bool Intersects(BoundingSphere sphere) => ((CNA.BoundingBox)this).Intersects(sphere);

    public readonly float? Intersects(Ray ray) => ((CNA.BoundingBox)this).Intersects(ray);

    public static BoundingBox CreateFromPoints(IEnumerable<Vector3> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        CNA.BoundingBox result = CNA.BoundingBox.CreateFromPoints(points.Select(p => (CNA.Vector3)p));
        return new BoundingBox(result.Min, result.Max);
    }

    public static BoundingBox CreateMerged(BoundingBox original, BoundingBox additional)
    {
        CNA.BoundingBox result = CNA.BoundingBox.CreateMerged(original, additional);
        return new BoundingBox(result.Min, result.Max);
    }

    public static BoundingBox CreateFromSphere(BoundingSphere sphere)
    {
        CNA.BoundingBox result = CNA.BoundingBox.CreateFromSphere(sphere);
        return new BoundingBox(result.Min, result.Max);
    }

    public static bool operator ==(BoundingBox a, BoundingBox b) => a.Equals(b);
    public static bool operator !=(BoundingBox a, BoundingBox b) => !a.Equals(b);

    public readonly bool Equals(BoundingBox other) => Min.Equals(other.Min) && Max.Equals(other.Max);
    public override readonly bool Equals(object? obj) => obj is BoundingBox other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(Min, Max);
    public override readonly string ToString() => $"{{Min:{Min} Max:{Max}}}";

    public static implicit operator CNA.BoundingBox(BoundingBox value) => new(value.Min, value.Max);
    public static implicit operator BoundingBox(CNA.BoundingBox value) => new(value.Min, value.Max);
}
