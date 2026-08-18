namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.SetDataOptions; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
public enum SetDataOptions
{
    None = 0,
    Discard = 1,
    NoOverwrite = 2,
}
