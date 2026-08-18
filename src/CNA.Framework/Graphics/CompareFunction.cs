namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>CompareFunction</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_COMPARE_*</c> constants
/// (<c>graphics_state.h:80-96</c>).</summary>
public enum CompareFunction
{
    Always = 0,
    Never = 1,
    Less = 2,
    LessEqual = 3,
    Equal = 4,
    GreaterEqual = 5,
    Greater = 6,
    NotEqual = 7,
}
