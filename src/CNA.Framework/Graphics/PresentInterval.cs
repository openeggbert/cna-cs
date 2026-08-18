namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>PresentInterval</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_PRESENT_INTERVAL_*</c> constants
/// (<c>display.h:28-36</c>).</summary>
public enum PresentInterval
{
    Default = 0,
    One = 1,
    Two = 2,
    Immediate = 3,
}
