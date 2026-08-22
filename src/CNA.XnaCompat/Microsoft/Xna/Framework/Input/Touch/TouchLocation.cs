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
        if (_previousState == TouchLocationState.Invalid)
        {
            previousLocation = new TouchLocation(-1, TouchLocationState.Invalid, Vector2.Zero);
            return false;
        }

        previousLocation = new TouchLocation(Id, _previousState, _previousPosition);
        return true;
    }

    internal static TouchLocation FromFramework(CNA.Input.Touch.TouchLocation source)
    {
        bool hasPrevious = source.TryGetPreviousLocation(out CNA.Input.Touch.TouchLocation previous);
        return new TouchLocation(
            source.Id,
            (TouchLocationState)(int)source.State,
            source.Position.ToCompat(),
            hasPrevious ? (TouchLocationState)(int)previous.State : TouchLocationState.Invalid,
            hasPrevious ? previous.Position.ToCompat() : Vector2.Zero);
    }

    public bool Equals(TouchLocation other) =>
        Id == other.Id &&
        Position.X == other.Position.X && Position.Y == other.Position.Y &&
        _previousPosition.X == other._previousPosition.X && _previousPosition.Y == other._previousPosition.Y;

    public override bool Equals(object? obj) => obj is TouchLocation other && Equals(other);

    public override int GetHashCode() => Id.GetHashCode() + Position.X.GetHashCode() + Position.Y.GetHashCode();

    public static bool operator ==(TouchLocation value1, TouchLocation value2) =>
        value1.Id == value2.Id && value1.State == value2.State &&
        value1.Position.X == value2.Position.X && value1.Position.Y == value2.Position.Y &&
        value1._previousState == value2._previousState &&
        value1._previousPosition.X == value2._previousPosition.X &&
        value1._previousPosition.Y == value2._previousPosition.Y;

    public static bool operator !=(TouchLocation value1, TouchLocation value2) => !(value1 == value2);

    public override string ToString() => $"{{Position:{Position}}}";
}
