namespace Microsoft.Xna.Framework;

/// <summary>See CNA.CurveTangent; values kept numerically identical to it. A distinct enum type,
/// not a type alias -- same "cast by value across the CNA/XnaCompat boundary" pattern as
/// <see cref="Graphics.SpriteEffects"/>.</summary>
public enum CurveTangent
{
    Flat = 0,
    Linear = 1,
    Smooth = 2,
}
