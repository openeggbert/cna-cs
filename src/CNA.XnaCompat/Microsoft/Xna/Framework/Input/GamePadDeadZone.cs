namespace Microsoft.Xna.Framework.Input;

/// <summary>See CNA.Input.GamePadDeadZone; values kept numerically identical to it. A distinct
/// enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary" pattern
/// already used for <see cref="Keys"/> and <see cref="Buttons"/>.</summary>
public enum GamePadDeadZone
{
    None = 0,
    IndependentAxes = 1,
    Circular = 2,
}
