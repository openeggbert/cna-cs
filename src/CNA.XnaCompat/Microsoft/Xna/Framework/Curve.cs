namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>Curve</c>. <c>Evaluate</c>/<c>ComputeTangents</c>/<c>Keys</c>/
/// <c>IsConstant</c> are inherited unchanged from <see cref="CNA.Curve"/> -- all of that is pure
/// math with no namespace-divergent types in its signature. Only
/// <see cref="PreLoop"/>/<see cref="PostLoop"/> and <see cref="Clone"/> need re-typing.</summary>
public class Curve : CNA.Curve
{
    public new CurveLoopType PreLoop
    {
        get => (CurveLoopType)(int)base.PreLoop;
        set => base.PreLoop = (CNA.CurveLoopType)(int)value;
    }

    public new CurveLoopType PostLoop
    {
        get => (CurveLoopType)(int)base.PostLoop;
        set => base.PostLoop = (CNA.CurveLoopType)(int)value;
    }

    public new Curve Clone()
    {
        var clone = new Curve { PreLoop = PreLoop, PostLoop = PostLoop };
        foreach (CNA.CurveKey key in Keys)
        {
            clone.Keys.Add(key.Clone());
        }

        return clone;
    }

    public void ComputeTangents(CurveTangent tangentType) =>
        base.ComputeTangents((CNA.CurveTangent)(int)tangentType);

    public void ComputeTangents(CurveTangent tangentInType, CurveTangent tangentOutType) =>
        base.ComputeTangents((CNA.CurveTangent)(int)tangentInType, (CNA.CurveTangent)(int)tangentOutType);

    public void ComputeTangent(int keyIndex, CurveTangent tangentType) =>
        base.ComputeTangent(keyIndex, (CNA.CurveTangent)(int)tangentType);

    public void ComputeTangent(int keyIndex, CurveTangent tangentInType, CurveTangent tangentOutType) =>
        base.ComputeTangent(keyIndex, (CNA.CurveTangent)(int)tangentInType, (CNA.CurveTangent)(int)tangentOutType);
}
