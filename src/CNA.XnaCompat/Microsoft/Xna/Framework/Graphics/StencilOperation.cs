namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.StencilOperation; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
public enum StencilOperation
{
    Keep = 0,
    Zero = 1,
    Replace = 2,
    Increment = 3,
    Decrement = 4,
    IncrementSaturation = 5,
    DecrementSaturation = 6,
    Invert = 7,
}
