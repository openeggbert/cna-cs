namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.SpriteSortMode; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
public enum SpriteSortMode
{
    Deferred = 0,
    Immediate = 1,
    Texture = 2,
    BackToFront = 3,
    FrontToBack = 4,
}
