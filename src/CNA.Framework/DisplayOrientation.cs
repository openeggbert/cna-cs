namespace CNA;

/// <summary>Matches real XNA's <c>DisplayOrientation</c> values exactly, including its deliberate
/// gap at 3 (the members are independent bits, not a dense sequence) -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_DISPLAY_ORIENTATION_*</c> constants
/// (<c>display.h:39-47</c>).</summary>
[Flags]
public enum DisplayOrientation
{
    Default = 0,
    LandscapeLeft = 1,
    LandscapeRight = 2,
    Portrait = 4,
}
