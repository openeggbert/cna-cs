using System.Collections;

namespace Microsoft.Xna.Framework.Input.Touch;

/// <summary>XNA 4.0-compatible <c>TouchCollection</c>. Duplicates rather than wraps
/// <see cref="CNA.Input.Touch.TouchCollection"/> because its element type differs per namespace
/// (see <see cref="TouchLocation"/>) and both are structs. Reproduces the same
/// <c>IList&lt;T&gt;</c>-with-throwing-mutators shape real XNA has -- see the base
/// namespace's version for why that is kept rather than modernised.</summary>
public struct TouchCollection : IList<TouchLocation>
{
    private readonly TouchLocation[]? _touches;
    private readonly bool _isConnected;

    public TouchCollection(TouchLocation[] touches)
    {
        ArgumentNullException.ThrowIfNull(touches);
        if (touches.Length > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(touches));
        }

        _touches = new TouchLocation[touches.Length];
        for (int i = 0; i < touches.Length; i++)
        {
            TouchLocation touch = touches[i];
            _touches[i] = touch.TryGetPreviousLocation(out TouchLocation previous)
                ? new TouchLocation(touch.Id, touch.State, touch.Position, previous.State, previous.Position)
                : new TouchLocation(touch.Id, touch.State, touch.Position);
        }
        _isConnected = true;
    }

    private TouchCollection(TouchLocation[] touches, bool isConnected)
    {
        _touches = touches;
        _isConnected = isConnected;
    }

    public bool IsConnected => _isConnected;

    public int Count => _touches?.Length ?? 0;

    public bool IsReadOnly => true;

    public TouchLocation this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _touches![index];
        }
        set => throw new NotSupportedException();
    }

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

    public int IndexOf(TouchLocation item)
    {
        for (int i = 0; i < Count; i++)
        {
            if (this[i] == item)
            {
                return i;
            }
        }

        return -1;
    }

    public void CopyTo(TouchLocation[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        if (arrayIndex < 0 || (long)arrayIndex + Count > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        for (int i = 0; i < Count; i++)
        {
            array[arrayIndex + i] = this[i];
        }
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<TouchLocation> IEnumerable<TouchLocation>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(TouchLocation item) => throw new NotSupportedException();

    public void Clear() => throw new NotSupportedException();

    public void Insert(int index, TouchLocation item) => throw new NotSupportedException();

    public bool Remove(TouchLocation item) => throw new NotSupportedException();

    public void RemoveAt(int index) => throw new NotSupportedException();

    public struct Enumerator : IEnumerator<TouchLocation>
    {
        private readonly TouchCollection _collection;
        private int _position;

        internal Enumerator(TouchCollection collection)
        {
            _collection = collection;
            _position = -1;
        }

        public readonly TouchLocation Current => _collection[_position];

        readonly object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _position++;
            if (_position < _collection.Count)
            {
                return true;
            }

            _position = _collection.Count;
            return false;
        }

        void IEnumerator.Reset() => _position = -1;

        public readonly void Dispose()
        {
        }
    }

    internal static TouchCollection FromFramework(CNA.Input.Touch.TouchCollection source)
    {
        var touches = new TouchLocation[source.Count];
        for (int i = 0; i < touches.Length; i++)
        {
            touches[i] = TouchLocation.FromFramework(source[i]);
        }

        return new TouchCollection(touches, source.IsConnected);
    }
}
