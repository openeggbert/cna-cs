namespace Microsoft.Xna.Framework;

/// <summary>See CNA.CurveLoopType; values kept numerically identical to it. A distinct enum type,
/// not a type alias -- same "cast by value across the CNA/XnaCompat boundary" pattern as
/// <see cref="Graphics.SpriteEffects"/>.</summary>
public enum CurveLoopType
{
    Constant = 0,
    Cycle = 1,
    CycleOffset = 2,
    Oscillate = 3,
    Linear = 4,
}
