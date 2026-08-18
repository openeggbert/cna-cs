namespace CNA;

/// <summary>
/// Matches real XNA's <c>CurveKey</c>: one control point of a <see cref="Curve"/>.
///
/// A reference type, exactly as in real XNA (despite the value-ish name) -- <see cref="Curve"/>
/// hands out live references and <see cref="Curve.ComputeTangents(CurveTangent)"/> mutates keys in
/// place, both of which a struct would silently break.
///
/// <see cref="Position"/> is immutable after construction because
/// <see cref="CurveKeyCollection"/> keeps its keys sorted by it; letting it change under the
/// collection would corrupt that ordering with no way for the collection to notice. Real XNA makes
/// the same choice.
/// </summary>
public class CurveKey : IEquatable<CurveKey>, IComparable<CurveKey>
{
    public CurveKey(float position, float value)
        : this(position, value, 0f, 0f, CurveContinuity.Smooth)
    {
    }

    public CurveKey(float position, float value, float tangentIn, float tangentOut)
        : this(position, value, tangentIn, tangentOut, CurveContinuity.Smooth)
    {
    }

    public CurveKey(float position, float value, float tangentIn, float tangentOut, CurveContinuity continuity)
    {
        Position = position;
        Value = value;
        TangentIn = tangentIn;
        TangentOut = tangentOut;
        Continuity = continuity;
    }

    public float Position { get; }

    public float Value { get; set; }

    public float TangentIn { get; set; }

    public float TangentOut { get; set; }

    public CurveContinuity Continuity { get; set; }

    public CurveKey Clone() => new(Position, Value, TangentIn, TangentOut, Continuity);

    /// <summary>Orders by <see cref="Position"/> alone -- what
    /// <see cref="CurveKeyCollection"/> sorts on. Deliberately inconsistent with
    /// <see cref="Equals(CurveKey)"/>, which compares every field: real XNA has the same
    /// asymmetry, and matching it matters more here than satisfying the usual
    /// "compare-equal implies equals" guideline, since curve data round-tripped through XNA
    /// tooling depends on this ordering.</summary>
    public int CompareTo(CurveKey? other)
    {
        if (other is null)
        {
            return 1;
        }

        return Position.CompareTo(other.Position);
    }

    public bool Equals(CurveKey? other) =>
        other is not null
        && Position == other.Position
        && Value == other.Value
        && TangentIn == other.TangentIn
        && TangentOut == other.TangentOut
        && Continuity == other.Continuity;

    public override bool Equals(object? obj) => obj is CurveKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Position, Value, TangentIn, TangentOut, Continuity);

    public static bool operator ==(CurveKey? a, CurveKey? b) => a is null ? b is null : a.Equals(b);

    public static bool operator !=(CurveKey? a, CurveKey? b) => !(a == b);

    public static bool operator <(CurveKey? a, CurveKey? b) => Compare(a, b) < 0;

    public static bool operator >(CurveKey? a, CurveKey? b) => Compare(a, b) > 0;

    public static bool operator <=(CurveKey? a, CurveKey? b) => Compare(a, b) <= 0;

    public static bool operator >=(CurveKey? a, CurveKey? b) => Compare(a, b) >= 0;

    private static int Compare(CurveKey? a, CurveKey? b) => a is null ? (b is null ? 0 : -1) : a.CompareTo(b);
}
