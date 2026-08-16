namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.SpriteEffects; bit values kept numerically identical to it. A
/// distinct enum type, not a type alias -- C# does not allow user-defined conversion operators on
/// enums, so callers crossing the CNA/XnaCompat boundary (e.g. <c>SpriteBatch.Draw</c>) cast by
/// value, the same pattern already used for <c>Keys</c>/<c>Buttons</c>.</summary>
[Flags]
public enum SpriteEffects
{
    None = 0,
    FlipHorizontally = 1,
    FlipVertically = 2,
}
