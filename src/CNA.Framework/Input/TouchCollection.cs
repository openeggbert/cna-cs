using System.Collections;
using CNA.Interop;

namespace CNA.Input.Touch;

/// <summary>
/// Matches real XNA's <c>TouchCollection</c>: an immutable snapshot of the touch points active at
/// the moment <see cref="TouchPanel.GetState"/> was called -- the same snapshot pattern
/// <see cref="KeyboardState"/>/<see cref="MouseState"/> use, not a live view.
///
/// Real XNA declares this as <c>ICollection&lt;TouchLocation&gt;</c> with every mutating member
/// throwing, because the interface predates read-only collection interfaces. That is reproduced
/// here rather than "fixed" to <c>IReadOnlyList&lt;T&gt;</c>: XNA source that assigns a
/// <c>TouchCollection</c> to an <c>ICollection&lt;TouchLocation&gt;</c> variable has to keep
/// compiling. <see cref="IsReadOnly"/> is <see langword="true"/>, which is the documented signal.
/// </summary>
public readonly struct TouchCollection : ICollection<TouchLocation>
{
    private readonly TouchLocation[]? _touches;

    public TouchCollection(TouchLocation[] touches)
    {
        ArgumentNullException.ThrowIfNull(touches);
        _touches = touches;
        IsConnected = touches.Length > 0;
    }

    private TouchCollection(TouchLocation[] touches, bool isConnected)
    {
        _touches = touches;
        IsConnected = isConnected;
    }

    /// <summary>Whether a touch device was known at snapshot time. Real XNA exposes this on
    /// <c>TouchPanelCapabilities</c> only; CNA's own <c>CNA_TouchState</c> reports it per snapshot
    /// too, and carrying it here means an empty collection can be told apart from an absent
    /// device.</summary>
    public bool IsConnected { get; }

    public int Count => _touches?.Length ?? 0;

    public bool IsReadOnly => true;

    public TouchLocation this[int index] =>
        _touches is null
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : _touches[index];

    /// <summary>Matches real XNA's <c>FindById</c>: locates a touch by its stable identifier
    /// across frames, since a touch's index within the collection is not stable.</summary>
    public bool FindById(int id, out TouchLocation touchLocation)
    {
        if (_touches is not null)
        {
            foreach (TouchLocation touch in _touches)
            {
                if (touch.Id == id)
                {
                    touchLocation = touch;
                    return true;
                }
            }
        }

        touchLocation = default;
        return false;
    }

    public bool Contains(TouchLocation item) => IndexOf(item) >= 0;

    public int IndexOf(TouchLocation item) => _touches is null ? -1 : Array.IndexOf(_touches, item);

    public void CopyTo(TouchLocation[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        _touches?.CopyTo(array, arrayIndex);
    }

    public IEnumerator<TouchLocation> GetEnumerator() =>
        ((IEnumerable<TouchLocation>)(_touches ?? [])).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // -- Mutating ICollection members: present so the interface is satisfied and XNA source that
    // -- names them still compiles; all throw, matching real XNA's own read-only snapshot.

    void ICollection<TouchLocation>.Add(TouchLocation item) => throw new NotSupportedException(ReadOnlyMessage);

    void ICollection<TouchLocation>.Clear() => throw new NotSupportedException(ReadOnlyMessage);

    bool ICollection<TouchLocation>.Remove(TouchLocation item) => throw new NotSupportedException(ReadOnlyMessage);

    private const string ReadOnlyMessage =
        "TouchCollection is an immutable snapshot of one TouchPanel.GetState() call and cannot be modified.";

    internal static TouchCollection FromNative(in CnaTouchState native)
    {
        int count = (int)Math.Min(native.TouchCount, (uint)CnaTouchState.MaxTouches);
        var touches = new TouchLocation[count];
        for (int i = 0; i < count; i++)
        {
            touches[i] = TouchLocation.FromNative(native.Touches[i]);
        }

        return new TouchCollection(touches, native.IsConnected != 0);
    }
}
