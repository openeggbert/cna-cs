using System.Collections;

namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// Shared base for this namespace's read-only media collections: a compat-typed view over the
/// <c>CNA.Media</c> collection that actually holds the native handle.
///
/// A wrapper, not a subclass, and no longer a second copy of a managed list. The <c>CNA.Media</c>
/// collections became native-backed with the media-library rebinding, so duplicating them here
/// would mean two objects each owning handles to the same library. Wrapping keeps exactly one
/// native collection and re-types what comes out of it -- the same composition trade-off the compat
/// <c>Effect</c> family already makes, and for the same reason: C# single inheritance cannot give a
/// compat collection an indexer typed to the compat element while it inherits one typed to the
/// base element.
///
/// Compat wrappers are cached per index so repeated reads return the same object, matching the
/// caching the underlying collection does for the same reason.
/// </summary>
/// <typeparam name="TCompat">The compat element type callers see.</typeparam>
/// <typeparam name="TBase">The <c>CNA.Media</c> element type the underlying collection holds.</typeparam>
public class ReadOnlyMediaCollection<TCompat, TBase> : IDisposable, IEnumerable<TCompat>
    where TCompat : class
    where TBase : class
{
    private readonly CNA.Media.ReadOnlyMediaCollection<TBase> _inner;
    private readonly Func<TBase, TCompat> _wrap;
    private readonly Dictionary<int, TCompat> _cache = [];

    private protected ReadOnlyMediaCollection(CNA.Media.ReadOnlyMediaCollection<TBase> inner, Func<TBase, TCompat> wrap)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
        _wrap = wrap;
    }

    /// <summary>The underlying collection, for the compat members that have to hand it back to a
    /// <c>CNA.Media</c> route (<c>MediaPlayer.Play</c>).</summary>
    internal CNA.Media.ReadOnlyMediaCollection<TBase> Inner => _inner;

    public int Count => _inner.Count;

    public bool IsDisposed => _inner.IsDisposed;

    public TCompat this[int index]
    {
        get
        {
            if (_cache.TryGetValue(index, out TCompat? cached))
            {
                return cached;
            }

            TCompat wrapped = _wrap(_inner[index]);
            _cache[index] = wrapped;
            return wrapped;
        }
    }

    /// <summary>Disposes the underlying collection, which is what releases the element handles.
    /// The compat wrappers hold no handles of their own, so dropping the cache is all they
    /// need.</summary>
    public void Dispose()
    {
        _cache.Clear();
        _inner.Dispose();
        GC.SuppressFinalize(this);
    }

    public IEnumerator<TCompat> GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
