using System.Collections;

namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible mutable collection of sorted curve keys.</summary>
public class CurveKeyCollection : ICollection<CurveKey>
{
    private readonly List<CurveKey> _keys = [];

    public int Count => _keys.Count;

    public bool IsReadOnly => false;

    public CurveKey this[int index]
    {
        get => _keys[index];
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (index >= _keys.Count)
            {
                throw new IndexOutOfRangeException();
            }

            if (CNA.MathHelper.WithinEpsilon(_keys[index].Position, value.Position))
            {
                _keys[index] = value;
            }
            else
            {
                // XNA removes the old key and appends the replacement. This slightly surprising
                // behavior is retained for behavioral compatibility.
                _keys.RemoveAt(index);
                _keys.Add(value);
            }
        }
    }

    public void Add(CurveKey item)
    {
        ArgumentNullException.ThrowIfNull(item);

        int index = 0;
        while (index < _keys.Count && item.Position >= _keys[index].Position)
        {
            index++;
        }

        _keys.Insert(index, item);
    }

    public void Clear() => _keys.Clear();

    /// <summary>Clones the collection while retaining the same mutable key instances, matching
    /// XNA. This intentionally differs from <c>CNA.CurveKeyCollection.Clone</c>.</summary>
    public CurveKeyCollection Clone()
    {
        var clone = new CurveKeyCollection();
        foreach (CurveKey key in _keys)
        {
            clone.Add(key);
        }

        return clone;
    }

    public bool Contains(CurveKey item) => _keys.Contains(item);

    public void CopyTo(CurveKey[] array, int arrayIndex) => _keys.CopyTo(array, arrayIndex);

    public IEnumerator<CurveKey> GetEnumerator() => _keys.GetEnumerator();

    public int IndexOf(CurveKey item) => _keys.IndexOf(item);

    public bool Remove(CurveKey item) => _keys.Remove(item);

    public void RemoveAt(int index) => _keys.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() => _keys.GetEnumerator();
}
