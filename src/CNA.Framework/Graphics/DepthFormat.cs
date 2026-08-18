namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>DepthFormat</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_DEPTH_FORMAT_*</c> constants
/// (<c>render_target.h:18-26</c>).</summary>
public enum DepthFormat
{
    None = 0,
    Depth16 = 1,
    Depth24 = 2,
    Depth24Stencil8 = 3,
}
