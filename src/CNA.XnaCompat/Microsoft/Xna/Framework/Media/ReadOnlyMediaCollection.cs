using System.Collections;

namespace Microsoft.Xna.Framework.Media;

/// <summary>Internal composition helper shared by the concrete XNA media collections.</summary>
internal sealed class MediaCollectionAdapter<TCompat, TBase>
    where TCompat : class
    where TBase : class
{
    private readonly CNA.Media.ReadOnlyMediaCollection<TBase> _inner;
    private readonly Func<TBase, TCompat> _wrap;
    private readonly Dictionary<int, TCompat> _cache = [];

    internal MediaCollectionAdapter(
        CNA.Media.ReadOnlyMediaCollection<TBase> inner,
        Func<TBase, TCompat> wrap)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(wrap);
        _inner = inner;
        _wrap = wrap;
    }

    internal CNA.Media.ReadOnlyMediaCollection<TBase> Inner => _inner;

    internal int Count => _inner.Count;

    internal bool IsDisposed => _inner.IsDisposed;

    internal TCompat GetItem(int index)
    {
        if (_cache.TryGetValue(index, out TCompat? cached))
        {
            return cached;
        }

        TCompat wrapped = _wrap(_inner[index]);
        _cache[index] = wrapped;
        return wrapped;
    }

    internal void Dispose()
    {
        _cache.Clear();
        _inner.Dispose();
    }

    internal IEnumerator<TCompat> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return GetItem(i);
        }
    }

    internal IEnumerator GetNonGenericEnumerator() => GetEnumerator();
}
