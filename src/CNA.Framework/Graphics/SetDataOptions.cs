namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>SetDataOptions</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_SET_DATA_*</c> constants
/// (<c>graphics3d.h:40-46</c>). Note the C API drops XNA's <c>OPTIONS</c> word from the constant
/// prefix (<c>CNA_SET_DATA_DISCARD</c>, not <c>CNA_SET_DATA_OPTIONS_DISCARD</c>).</summary>
public enum SetDataOptions
{
    None = 0,
    Discard = 1,
    NoOverwrite = 2,
}
