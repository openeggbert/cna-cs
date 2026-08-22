namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0 curve control point. This independent implementation keeps the strict
/// generic interfaces typed on the Microsoft.Xna.Framework identity.</summary>
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

    public CurveKey(
        float position,
        float value,
        float tangentIn,
        float tangentOut,
        CurveContinuity continuity)
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

    public int CompareTo(CurveKey? other) =>
        other is null ? 1 : Position.CompareTo(other.Position);

    public bool Equals(CurveKey? other) =>
        other is not null &&
        Position == other.Position &&
        Value == other.Value &&
        TangentIn == other.TangentIn &&
        TangentOut == other.TangentOut &&
        Continuity == other.Continuity;

    public override bool Equals(object? obj) => obj is CurveKey other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Position, Value, TangentIn, TangentOut, Continuity);

    public static bool operator ==(CurveKey? a, CurveKey? b) =>
        a is null ? b is null : a.Equals(b);

    public static bool operator !=(CurveKey? a, CurveKey? b) => !(a == b);
}
