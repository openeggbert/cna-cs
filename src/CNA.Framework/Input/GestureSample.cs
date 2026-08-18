using CNA.Interop;

namespace CNA.Input.Touch;

/// <summary>
/// Matches real XNA's <c>GestureSample</c>: one gesture dequeued from
/// <see cref="TouchPanel.ReadGesture"/>.
///
/// The native <c>CNA_GestureSample</c> also carries <c>finger_id_ext</c>/<c>finger_id2_ext</c>,
/// CNA extensions with no real-XNA counterpart, deliberately not surfaced here for the same
/// fidelity reason <see cref="TouchLocation"/> hides <c>pressure</c>.
/// </summary>
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

    /// <summary>The second touch point, meaningful only for <see cref="GestureType.Pinch"/>.
    /// Zero otherwise, matching real XNA.</summary>
    public Vector2 Position2 { get; }

    public Vector2 Delta { get; }

    public Vector2 Delta2 { get; }

    internal static GestureSample FromNative(in CnaGestureSample native) =>
        new((GestureType)native.GestureType,
            TimeSpan.FromTicks(native.TimestampTicks),
            Vector2.FromNative(native.Position),
            Vector2.FromNative(native.Position2),
            Vector2.FromNative(native.Delta),
            Vector2.FromNative(native.Delta2));
}
