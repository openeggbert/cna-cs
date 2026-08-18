namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>Blend</c> values exactly -- also confirmed against the real,
/// shipped openeggbert/cna C API's own <c>CNA_BLEND_*</c> constants
/// (<c>graphics_state.h:23-49</c>).</summary>
public enum Blend
{
    One = 0,
    Zero = 1,
    SourceColor = 2,
    InverseSourceColor = 3,
    SourceAlpha = 4,
    InverseSourceAlpha = 5,
    DestinationColor = 6,
    InverseDestinationColor = 7,
    DestinationAlpha = 8,
    InverseDestinationAlpha = 9,
    BlendFactor = 10,
    InverseBlendFactor = 11,
    SourceAlphaSaturation = 12,
}
