namespace CNA;

/// <summary>Matches real XNA's <c>CurveContinuity</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_CURVE_CONTINUITY_*</c> constants
/// (<c>math_values.h:30-35</c>).</summary>
public enum CurveContinuity
{
    Smooth = 0,
    Step = 1,
}

/// <summary>Matches real XNA's <c>CurveLoopType</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_CURVE_LOOP_*</c> constants
/// (<c>math_values.h:37-48</c>).</summary>
public enum CurveLoopType
{
    Constant = 0,
    Cycle = 1,
    CycleOffset = 2,
    Oscillate = 3,
    Linear = 4,
}

/// <summary>Matches real XNA's <c>CurveTangent</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_CURVE_TANGENT_*</c> constants
/// (<c>math_values.h:50-57</c>).</summary>
public enum CurveTangent
{
    Flat = 0,
    Linear = 1,
    Smooth = 2,
}
