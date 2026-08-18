namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.DepthFormat; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
public enum DepthFormat
{
    None = 0,
    Depth16 = 1,
    Depth24 = 2,
    Depth24Stencil8 = 3,
}
