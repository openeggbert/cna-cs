namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>FillMode</c> values exactly -- also confirmed against the real,
/// shipped openeggbert/cna C API's own <c>CNA_FILL_*</c> constants
/// (<c>graphics_state.h:127-131</c>).</summary>
public enum FillMode
{
    Solid = 0,
    WireFrame = 1,
}
