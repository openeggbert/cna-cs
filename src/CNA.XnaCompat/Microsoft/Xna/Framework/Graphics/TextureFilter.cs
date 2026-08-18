namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.TextureFilter; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
public enum TextureFilter
{
    Linear = 0,
    Point = 1,
    Anisotropic = 2,
    LinearMipPoint = 3,
    PointMipLinear = 4,
    MinLinearMagPointMipLinear = 5,
    MinLinearMagPointMipPoint = 6,
    MinPointMagLinearMipLinear = 7,
    MinPointMagLinearMipPoint = 8,
}
