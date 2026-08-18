using System.Collections;

namespace Microsoft.Xna.Framework.Input.Touch;

/// <summary>XNA 4.0-compatible <c>TouchCollection</c>. Duplicates rather than wraps
/// <see cref="CNA.Input.Touch.TouchCollection"/> because its element type differs per namespace
/// (see <see cref="TouchLocation"/>) and both are structs. Reproduces the same
/// <c>ICollection&lt;T&gt;</c>-with-throwing-mutators shape real XNA has -- see the base
/// namespace's version for why that is kept rather than modernised.</summary>
public readonly struct TouchCollection : ICollection<TouchLocation>
{
    private readonly TouchLocation[]? _touches;

    public TouchCollection(TouchLocation[] touches)
    {
        ArgumentNullException.ThrowIfNull(touches);
        _touches = touches;
    }

    public int Count => _touches?.Length ?? 0;

    public bool IsReadOnly => true;

    public TouchLocation this[int index] =>
        _touches is null ? throw new ArgumentOutOfRangeException(nameof(index)) : _touches[index];

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

    public IEnumerator<TouchLocation> GetEnumerator() => ((IEnumerable<TouchLocation>)(_touches ?? [])).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<TouchLocation>.Add(TouchLocation item) => throw new NotSupportedException(ReadOnlyMessage);

    void ICollection<TouchLocation>.Clear() => throw new NotSupportedException(ReadOnlyMessage);

    bool ICollection<TouchLocation>.Remove(TouchLocation item) => throw new NotSupportedException(ReadOnlyMessage);

    private const string ReadOnlyMessage =
        "TouchCollection is an immutable snapshot of one TouchPanel.GetState() call and cannot be modified.";

    internal static TouchCollection FromFramework(CNA.Input.Touch.TouchCollection source)
    {
        var touches = new TouchLocation[source.Count];
        for (int i = 0; i < touches.Length; i++)
        {
            touches[i] = TouchLocation.FromFramework(source[i]);
        }

        return new TouchCollection(touches);
    }
}
