namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.GraphicsDeviceStatus; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="SpriteEffects"/>.</summary>
public enum GraphicsDeviceStatus
{
    Normal = 0,
    Lost = 1,
    NotReset = 2,
}
