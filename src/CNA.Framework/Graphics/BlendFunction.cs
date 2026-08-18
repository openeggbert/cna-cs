namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>BlendFunction</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_BLEND_FUNCTION_*</c> constants
/// (<c>graphics_state.h:52-62</c>).</summary>
public enum BlendFunction
{
    Add = 0,
    Subtract = 1,
    ReverseSubtract = 2,
    Max = 3,
    Min = 4,
}
