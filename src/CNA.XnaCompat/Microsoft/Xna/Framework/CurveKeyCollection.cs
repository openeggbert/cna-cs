using System.Collections;

namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible mutable collection of sorted curve keys.</summary>
public class CurveKeyCollection : ICollection<CurveKey>
{
    private List<CurveKey> _keys = [];
    private float _timeRange;
    private float _inverseTimeRange;
    private bool _isCacheAvailable = true;

    public int Count => _keys.Count;

    public bool IsReadOnly => false;

    public CurveKey this[int index]
    {
        get => _keys[index];
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException();
            }

            // Reading first preserves List<T>'s ArgumentOutOfRangeException for both invalid
            // directions. XNA only replaces in place when the positions are exactly equal.
            float oldPosition = _keys[index].Position;
            if (oldPosition == value.Position)
            {
                _keys[index] = value;
            }
            else
            {
                _keys.RemoveAt(index);
                Add(value);
            }
        }
    }

    public void Add(CurveKey item)
    {
        if (item is null)
        {
            throw new ArgumentNullException();
        }

        int index = _keys.BinarySearch(item);
        if (index >= 0)
        {
            while (index < _keys.Count && item.Position == _keys[index].Position)
            {
                index++;
            }
        }
        else
        {
            index = ~index;
        }

        _keys.Insert(index, item);
        _isCacheAvailable = false;
    }

    public void Clear()
    {
        _keys.Clear();
        _timeRange = 0f;
        _inverseTimeRange = 0f;
        _isCacheAvailable = false;
    }

    /// <summary>Clones the collection while retaining the same mutable key instances, matching
    /// XNA. This intentionally differs from <c>CNA.CurveKeyCollection.Clone</c>.</summary>
    public CurveKeyCollection Clone()
    {
        return new CurveKeyCollection
        {
            _keys = new List<CurveKey>(_keys),
            _inverseTimeRange = _inverseTimeRange,
            _timeRange = _timeRange,
            // XNA marks the copied cache valid even if the source cache was dirty.
            _isCacheAvailable = true,
        };
    }

    public bool Contains(CurveKey item) => _keys.Contains(item);

    public void CopyTo(CurveKey[] array, int arrayIndex)
    {
        _keys.CopyTo(array, arrayIndex);
        _isCacheAvailable = false;
    }

    public IEnumerator<CurveKey> GetEnumerator() => _keys.GetEnumerator();

    public int IndexOf(CurveKey item) => _keys.IndexOf(item);

    public bool Remove(CurveKey item)
    {
        _isCacheAvailable = false;
        return _keys.Remove(item);
    }

    public void RemoveAt(int index)
    {
        _keys.RemoveAt(index);
        _isCacheAvailable = false;
    }

    IEnumerator IEnumerable.GetEnumerator() => _keys.GetEnumerator();

    internal float TimeRange
    {
        get
        {
            EnsureCache();
            return _timeRange;
        }
    }

    internal float InverseTimeRange
    {
        get
        {
            EnsureCache();
            return _inverseTimeRange;
        }
    }

    private void EnsureCache()
    {
        if (_isCacheAvailable)
        {
            return;
        }

        _timeRange = 0f;
        _inverseTimeRange = 0f;
        if (_keys.Count > 1)
        {
            _timeRange = _keys[^1].Position - _keys[0].Position;
            if (_timeRange > float.Epsilon)
            {
                _inverseTimeRange = 1f / _timeRange;
            }
        }

        _isCacheAvailable = true;
    }
}
