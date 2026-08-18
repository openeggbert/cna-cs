namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.CompareFunction; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
public enum CompareFunction
{
    Always = 0,
    Never = 1,
    Less = 2,
    LessEqual = 3,
    Equal = 4,
    GreaterEqual = 5,
    Greater = 6,
    NotEqual = 7,
}
