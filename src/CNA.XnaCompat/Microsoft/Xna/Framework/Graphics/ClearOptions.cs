namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.ClearOptions; bit values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary" pattern
/// as <see cref="SpriteEffects"/>.</summary>
[Flags]
public enum ClearOptions
{
    Target = 1,
    DepthBuffer = 2,
    Stencil = 4,
}
