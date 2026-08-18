namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>GraphicsProfile</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_GRAPHICS_PROFILE_*</c> constants
/// (<c>display.h:21-25</c>).</summary>
public enum GraphicsProfile
{
    Reach = 0,
    HiDef = 1,
}
