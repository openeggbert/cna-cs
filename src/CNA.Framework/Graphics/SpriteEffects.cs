namespace CNA.Graphics;

/// <summary>
/// Matches real XNA 4.0's <c>SpriteEffects</c> bit values. The analysis docs
/// (../../cnabinding/analysis_binding.md §52) only ever mention <c>FlipHorizontally</c> by name,
/// in a naming-parity example -- no bit values are given anywhere in the analysis docs, so these
/// are the real XNA values from memory, not sourced from this project's own analysis docs.
/// </summary>
[Flags]
public enum SpriteEffects
{
    None = 0,
    FlipHorizontally = 1,
    FlipVertically = 2,
}
