namespace Microsoft.Xna.Framework.Input.Touch;

/// <summary>XNA 4.0-compatible <c>GestureSample</c>. See <see cref="TouchLocation"/> for why this
/// duplicates rather than subclasses.</summary>
public readonly struct GestureSample
{
    internal GestureSample(
        GestureType gestureType, TimeSpan timestamp, Vector2 position, Vector2 position2, Vector2 delta, Vector2 delta2)
    {
        GestureType = gestureType;
        Timestamp = timestamp;
        Position = position;
        Position2 = position2;
        Delta = delta;
        Delta2 = delta2;
    }

    public GestureType GestureType { get; }

    public TimeSpan Timestamp { get; }

    public Vector2 Position { get; }

    public Vector2 Position2 { get; }

    public Vector2 Delta { get; }

    public Vector2 Delta2 { get; }

    internal static GestureSample FromFramework(CNA.Input.Touch.GestureSample source) =>
        new((GestureType)(int)source.GestureType,
            source.Timestamp,
            source.Position,
            source.Position2,
            source.Delta,
            source.Delta2);
}
