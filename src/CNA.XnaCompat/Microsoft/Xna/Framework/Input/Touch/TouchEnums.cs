namespace Microsoft.Xna.Framework.Input.Touch;

/// <summary>See CNA.Input.Touch.TouchLocationState; values kept numerically identical to it. A
/// distinct enum type, not a type alias -- same "cast by value across the CNA/XnaCompat boundary"
/// pattern as <see cref="Microsoft.Xna.Framework.Graphics.SpriteEffects"/>.</summary>
public enum TouchLocationState
{
    Invalid = 0,
    Released = 1,
    Pressed = 2,
    Moved = 3,
}

/// <summary>See CNA.Input.Touch.GestureType; bit values kept numerically identical to it.</summary>
[Flags]
public enum GestureType
{
    None = 0,
    Tap = 1,
    DoubleTap = 2,
    Hold = 4,
    HorizontalDrag = 8,
    VerticalDrag = 16,
    FreeDrag = 32,
    Pinch = 64,
    Flick = 128,
    DragComplete = 256,
    PinchComplete = 512,
}
