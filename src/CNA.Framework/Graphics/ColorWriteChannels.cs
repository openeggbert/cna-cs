namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>ColorWriteChannels</c> values exactly -- also confirmed against
/// the real, shipped openeggbert/cna C API's own <c>CNA_COLOR_WRITE_*</c> constants
/// (<c>graphics_state.h:65-77</c>).</summary>
[Flags]
public enum ColorWriteChannels
{
    None = 0,
    Red = 1,
    Green = 2,
    Blue = 4,
    Alpha = 8,
    All = 15,
}
