namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.ColorWriteChannels; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
[Flags]
public enum ColorWriteChannels
{
    None = 0,
    Red = 1,
    Green = 2,
    Blue = 4,
    Alpha = 8,
    All = 15,
}
