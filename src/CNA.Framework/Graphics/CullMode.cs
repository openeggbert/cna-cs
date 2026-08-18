namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>CullMode</c> values exactly -- also confirmed against the real,
/// shipped openeggbert/cna C API's own <c>CNA_CULL_*</c> constants
/// (<c>graphics_state.h:118-124</c>).</summary>
public enum CullMode
{
    None = 0,
    CullClockwiseFace = 1,
    CullCounterClockwiseFace = 2,
}
