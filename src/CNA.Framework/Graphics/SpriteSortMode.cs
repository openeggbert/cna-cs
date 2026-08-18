namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>SpriteSortMode</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_SPRITE_SORT_MODE_*</c> constants
/// (<c>graphics.h:289-300</c>).</summary>
public enum SpriteSortMode
{
    Deferred = 0,
    Immediate = 1,
    Texture = 2,
    BackToFront = 3,
    FrontToBack = 4,
}
