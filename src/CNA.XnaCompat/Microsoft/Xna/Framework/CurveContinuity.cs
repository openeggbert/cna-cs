namespace Microsoft.Xna.Framework;

/// <summary>See CNA.CurveContinuity; values kept numerically identical to it. A distinct enum type,
/// not a type alias -- same "cast by value across the CNA/XnaCompat boundary" pattern as
/// <see cref="Graphics.SpriteEffects"/>.</summary>
public enum CurveContinuity
{
    Smooth = 0,
    Step = 1,
}
