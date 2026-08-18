namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.SurfaceFormat; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary" pattern
/// as <see cref="SpriteEffects"/>. Values 0-19 are real XNA's own; 20-26 are CNA extensions
/// beyond XNA, carried here for the same round-tripping reason the CNA.Graphics original gives.</summary>
public enum SurfaceFormat
{
    Color = 0,
    Bgr565 = 1,
    Bgra5551 = 2,
    Bgra4444 = 3,
    Dxt1 = 4,
    Dxt3 = 5,
    Dxt5 = 6,
    NormalizedByte2 = 7,
    NormalizedByte4 = 8,
    Rgba1010102 = 9,
    Rg32 = 10,
    Rgba64 = 11,
    Alpha8 = 12,
    Single = 13,
    Vector2 = 14,
    Vector4 = 15,
    HalfSingle = 16,
    HalfVector2 = 17,
    HalfVector4 = 18,
    HdrBlendable = 19,
    ColorBgraExt = 20,
    ColorSrgbExt = 21,
    Dxt5SrgbExt = 22,
    Bc7Ext = 23,
    Bc7SrgbExt = 24,
    ByteExt = 25,
    UShortExt = 26,
}
