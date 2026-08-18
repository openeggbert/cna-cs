namespace Microsoft.Xna.Framework;

/// <summary>See CNA.DisplayOrientation; values kept numerically identical to it. A distinct enum
/// type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary" pattern as
/// <see cref="Graphics.SpriteEffects"/>.</summary>
[Flags]
public enum DisplayOrientation
{
    Default = 0,
    LandscapeLeft = 1,
    LandscapeRight = 2,
    Portrait = 4,
}
