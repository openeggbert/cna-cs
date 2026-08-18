namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.CullMode; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
public enum CullMode
{
    None = 0,
    CullClockwiseFace = 1,
    CullCounterClockwiseFace = 2,
}
