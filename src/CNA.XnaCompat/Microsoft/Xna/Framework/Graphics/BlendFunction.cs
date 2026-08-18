namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.BlendFunction; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
public enum BlendFunction
{
    Add = 0,
    Subtract = 1,
    ReverseSubtract = 2,
    Max = 3,
    Min = 4,
}
