namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>StencilOperation</c> values exactly -- also confirmed against
/// the real, shipped openeggbert/cna C API's own <c>CNA_STENCIL_*</c> constants
/// (<c>graphics_state.h:99-115</c>).</summary>
public enum StencilOperation
{
    Keep = 0,
    Zero = 1,
    Replace = 2,
    Increment = 3,
    Decrement = 4,
    IncrementSaturation = 5,
    DecrementSaturation = 6,
    Invert = 7,
}
