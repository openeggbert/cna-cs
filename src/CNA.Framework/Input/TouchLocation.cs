using CNA.Interop;

namespace CNA.Input.Touch;

/// <summary>
/// Matches real XNA's <c>TouchLocation</c>: one touch point in a <see cref="TouchCollection"/>
/// snapshot.
///
/// The native <c>CNA_TouchLocation</c> also carries a <c>pressure</c> field, a CNA extension with
/// no real-XNA counterpart. It is deliberately not surfaced here: this type's whole purpose is
/// XNA-shape fidelity, and adding a member real XNA never had would make code written against it
/// silently non-portable. The value is still there in <c>CNA.Interop.CnaTouchLocation</c> if a
/// future CNA-specific surface wants it.
/// </summary>
public readonly struct TouchLocation : IEquatable<TouchLocation>
{
    public TouchLocation(int id, TouchLocationState state, Vector2 position)
    {
        Id = id;
        State = state;
        Position = position;
        _previousState = TouchLocationState.Invalid;
        _previousPosition = Vector2.Zero;
    }

    public TouchLocation(int id, TouchLocationState state, Vector2 position, TouchLocationState previousState, Vector2 previousPosition)
    {
        Id = id;
        State = state;
        Position = position;
        _previousState = previousState;
        _previousPosition = previousPosition;
    }

    private readonly TouchLocationState _previousState;
    private readonly Vector2 _previousPosition;

    public int Id { get; }

    public TouchLocationState State { get; }

    public Vector2 Position { get; }

    /// <summary>Matches real XNA's <c>TryGetPreviousLocation</c>: returns <see langword="false"/>
    /// (and an <see cref="TouchLocationState.Invalid"/>-stated location) when this touch has no
    /// previous sample, rather than exposing the previous state as a plain property.</summary>
    public bool TryGetPreviousLocation(out TouchLocation previousLocation)
    {
        previousLocation = new TouchLocation(Id, _previousState, _previousPosition);
        return _previousState != TouchLocationState.Invalid;
    }

    internal static TouchLocation FromNative(CnaTouchLocation native) =>
        new(native.Id,
            (TouchLocationState)native.State,
            Vector2.FromNative(native.Position),
            (TouchLocationState)native.PreviousState,
            Vector2.FromNative(native.PreviousPosition));

    public bool Equals(TouchLocation other) =>
        Id == other.Id && State == other.State && Position.Equals(other.Position);

    public override bool Equals(object? obj) => obj is TouchLocation other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, State, Position);

    public static bool operator ==(TouchLocation a, TouchLocation b) => a.Equals(b);

    public static bool operator !=(TouchLocation a, TouchLocation b) => !a.Equals(b);

    public override string ToString() => $"{{Id:{Id} State:{State} Position:{Position}}}";
}
