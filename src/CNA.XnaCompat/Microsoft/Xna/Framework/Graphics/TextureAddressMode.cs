namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.TextureAddressMode; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
public enum TextureAddressMode
{
    Wrap = 0,
    Clamp = 1,
    Mirror = 2,
}
