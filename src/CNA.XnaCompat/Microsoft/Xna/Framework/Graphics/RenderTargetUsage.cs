namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.RenderTargetUsage; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
public enum RenderTargetUsage
{
    DiscardContents = 0,
    PreserveContents = 1,
    PlatformContents = 2,
}
