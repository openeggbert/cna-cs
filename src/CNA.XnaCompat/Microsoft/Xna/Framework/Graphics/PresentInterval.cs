namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.PresentInterval; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
public enum PresentInterval
{
    Default = 0,
    One = 1,
    Two = 2,
    Immediate = 3,
}
