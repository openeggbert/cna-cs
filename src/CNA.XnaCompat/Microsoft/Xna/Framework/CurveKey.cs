namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>CurveKey</c>. A pure subclass -- <c>Position</c>/<c>Value</c>/
/// <c>TangentIn</c>/<c>TangentOut</c>/comparison/equality are all inherited unchanged from
/// <see cref="CNA.CurveKey"/>; only <see cref="Continuity"/> needs re-typing, since
/// <see cref="CurveContinuity"/> is duplicated per namespace (see that enum's own doc
/// comment).</summary>
public class CurveKey : CNA.CurveKey
{
    public CurveKey(float position, float value)
        : base(position, value)
    {
    }

    public CurveKey(float position, float value, float tangentIn, float tangentOut)
        : base(position, value, tangentIn, tangentOut)
    {
    }

    public CurveKey(float position, float value, float tangentIn, float tangentOut, CurveContinuity continuity)
        : base(position, value, tangentIn, tangentOut, (CNA.CurveContinuity)(int)continuity)
    {
    }

    public new CurveContinuity Continuity
    {
        get => (CurveContinuity)(int)base.Continuity;
        set => base.Continuity = (CNA.CurveContinuity)(int)value;
    }

    public new CurveKey Clone() => new(Position, Value, TangentIn, TangentOut, Continuity);
}
