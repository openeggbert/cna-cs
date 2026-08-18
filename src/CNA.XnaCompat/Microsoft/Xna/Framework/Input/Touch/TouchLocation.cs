namespace Microsoft.Xna.Framework.Input.Touch;

/// <summary>XNA 4.0-compatible <c>TouchLocation</c>. See
/// <see cref="Microsoft.Xna.Framework.Color"/>'s own doc comment for why this duplicates
/// <see cref="CNA.Input.Touch.TouchLocation"/> rather than subclassing it (structs cannot
/// inherit).</summary>
public readonly struct TouchLocation : IEquatable<TouchLocation>
{
    public TouchLocation(int id, TouchLocationState state, Vector2 position)
        : this(id, state, position, TouchLocationState.Invalid, Vector2.Zero)
    {
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

    public bool TryGetPreviousLocation(out TouchLocation previousLocation)
    {
        previousLocation = new TouchLocation(Id, _previousState, _previousPosition);
        return _previousState != TouchLocationState.Invalid;
    }

    internal static TouchLocation FromFramework(CNA.Input.Touch.TouchLocation source)
    {
        source.TryGetPreviousLocation(out CNA.Input.Touch.TouchLocation previous);
        return new TouchLocation(
            source.Id,
            (TouchLocationState)(int)source.State,
            source.Position,
            (TouchLocationState)(int)previous.State,
            previous.Position);
    }

    public bool Equals(TouchLocation other) =>
        Id == other.Id && State == other.State && Position.Equals(other.Position);

    public override bool Equals(object? obj) => obj is TouchLocation other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, State, Position);

    public static bool operator ==(TouchLocation a, TouchLocation b) => a.Equals(b);

    public static bool operator !=(TouchLocation a, TouchLocation b) => !a.Equals(b);

    public override string ToString() => $"{{Id:{Id} State:{State} Position:{Position}}}";
}
