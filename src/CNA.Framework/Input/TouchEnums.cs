namespace CNA.Input.Touch;

/// <summary>Matches real XNA's <c>TouchLocationState</c> values exactly -- also confirmed against
/// the real, shipped openeggbert/cna C API's own <c>CNA_TOUCH_LOCATION_*</c> constants
/// (<c>input.h:520-530</c>).</summary>
public enum TouchLocationState
{
    Invalid = 0,
    Released = 1,
    Pressed = 2,
    Moved = 3,
}

/// <summary>Matches real XNA's <c>GestureType</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_GESTURE_TYPE_*</c> constants
/// (<c>input_touch.h</c>). A bit set: <see cref="TouchPanel.EnabledGestures"/> takes a
/// combination.</summary>
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
