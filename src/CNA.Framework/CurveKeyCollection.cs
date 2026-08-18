using System.Collections;

namespace CNA;

/// <summary>
/// Matches real XNA's <c>CurveKeyCollection</c>: the <see cref="CurveKey"/> list of a
/// <see cref="Curve"/>, kept sorted by <see cref="CurveKey.Position"/> at all times.
///
/// Sorting on insert (rather than on read) is what lets <see cref="Curve.Evaluate"/> binary-search
/// and lets <see cref="Curve.ComputeTangents(CurveTangent)"/> look at neighbours by index. Real XNA
/// behaves identically, including the two sharp edges reproduced here: <see cref="Add"/> rejects a
/// duplicate <see cref="CurveKey.Position"/>, and the indexer's setter refuses to move a key to a
/// different position (which would break the ordering the collection is built on).
/// </summary>
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
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _keys.Count);

            if (_keys[index].Position != value.Position)
            {
                throw new InvalidOperationException(
                    "Cannot replace a CurveKey with one at a different Position -- CurveKeyCollection is kept sorted by " +
                    "Position. Remove the existing key and Add the new one instead.");
            }

            _keys[index] = value;
        }
    }

    /// <summary>Inserts <paramref name="item"/> at the position that keeps the collection sorted.
    /// Throws when a key already occupies that exact <see cref="CurveKey.Position"/>, matching real
    /// XNA -- two keys at one position would make <see cref="Curve.Evaluate"/> ambiguous.</summary>
    public void Add(CurveKey item)
    {
        ArgumentNullException.ThrowIfNull(item);

        int index = _keys.BinarySearch(item);
        if (index >= 0)
        {
            throw new InvalidOperationException($"A CurveKey already exists at Position {item.Position}.");
        }

        _keys.Insert(~index, item);
    }

    public void Clear() => _keys.Clear();

    public bool Contains(CurveKey item) => _keys.Contains(item);

    public void CopyTo(CurveKey[] array, int arrayIndex) => _keys.CopyTo(array, arrayIndex);

    public int IndexOf(CurveKey item) => _keys.IndexOf(item);

    public bool Remove(CurveKey item) => _keys.Remove(item);

    public void RemoveAt(int index) => _keys.RemoveAt(index);

    /// <summary>A deep copy: every <see cref="CurveKey"/> is cloned too, so mutating a key of the
    /// clone cannot reach back into the original. Matches real XNA, and matters because
    /// <see cref="Curve.ComputeTangents(CurveTangent)"/> mutates keys in place.</summary>
    public CurveKeyCollection Clone()
    {
        var clone = new CurveKeyCollection();
        foreach (CurveKey key in _keys)
        {
            clone._keys.Add(key.Clone());
        }

        return clone;
    }

    public IEnumerator<CurveKey> GetEnumerator() => _keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
