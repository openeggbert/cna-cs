using System.Collections;
using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// Shared indexer/<c>Count</c>/<c>Dispose</c>/enumerator implementation for this namespace's
/// read-only media collections (<see cref="SongCollection"/>/<see cref="AlbumCollection"/>/
/// <see cref="ArtistCollection"/>/<see cref="GenreCollection"/>/<see cref="PlaylistCollection"/>/
/// <see cref="PictureCollection"/>/<see cref="PictureAlbumCollection"/>).
///
/// Native-backed since the media-library rebinding: it holds the collection's own
/// <c>CNA_*CollectionHandle</c> and asks native for the count and for each element, instead of the
/// managed <c>List&lt;T&gt;</c> it used to wrap. Every one of these collections has the same three
/// ABI routes -- <c>_get_count</c>, <c>_get_at</c>, <c>_destroy</c> -- so they are passed in rather
/// than reimplemented seven times.
///
/// <b>Element wrappers are cached per index.</b> Two reasons, both load-bearing. Each
/// <c>_get_at</c> hands back a fresh handle the caller must release, so re-reading the same index
/// in a loop would leak one handle per read until finalization; and XNA callers compare collection
/// elements by reference (<c>if (song == library.Songs[0])</c>), which a fresh wrapper per read
/// would break. The cache is what makes both work, and disposing the collection is what releases
/// the element handles it accumulated.
///
/// Necessarily <c>public</c> (a <c>public sealed class SongCollection</c> cannot derive from an
/// <c>internal</c> base -- C# CS0060), in the same spirit as the BCL's own
/// <c>System.Collections.ObjectModel.ReadOnlyCollection&lt;T&gt;</c>: the named collection types
/// above are the real public API surface real XNA specifies, and this is their shared
/// implementation detail.
/// </summary>
public class ReadOnlyMediaCollection<T> : IDisposable, IEnumerable<T>
    where T : class
{
    private readonly NativeResourceHandle _handle;
    private readonly CountFunc _getCount;
    private readonly ElementFunc _getAt;
    private readonly Func<CnaHandle, T> _wrap;
    private readonly Dictionary<int, T> _cache = [];

    private protected ReadOnlyMediaCollection(
        CnaHandle handle,
        CountFunc getCount,
        ElementFunc getAt,
        Action<CnaHandle> destroy,
        Func<CnaHandle, T> wrap)
    {
        _getCount = getCount;
        _getAt = getAt;
        _wrap = wrap;
        _handle = new NativeResourceHandle(handle.AsNint, h => destroy(new CnaHandle(h)));
    }

    private protected delegate CnaResult CountFunc(CnaHandle collection, out int outCount);

    private protected delegate CnaResult ElementFunc(CnaHandle collection, int index, out CnaHandle outElement);

    /// <summary>See <see cref="MediaLibrary"/> for why every handle read is paired with
    /// <see cref="GC.KeepAlive(object)"/>. <c>internal</c> rather than <c>private</c> so
    /// <see cref="MediaPlayer"/> can hand a <see cref="SongCollection"/> straight to
    /// <c>cna_media_player_play_songs</c> -- the alternative is rebuilding a native collection that
    /// already exists.</summary>
    internal CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public int Count
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            CnaResult result = _getCount(NativeHandle, out int count);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Count));
            return count;
        }
    }

    public bool IsDisposed { get; private set; }

    public T this[int index]
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

            if (_cache.TryGetValue(index, out T? cached))
            {
                return cached;
            }

            CnaResult result = _getAt(NativeHandle, index, out CnaHandle element);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, "MediaCollection indexer");

            T wrapped = _wrap(element);
            _cache[index] = wrapped;
            return wrapped;
        }
    }

    /// <summary>Releases every element handle this collection handed out, then its own. Element
    /// wrappers are released here rather than left to their finalizers because they are this
    /// collection's to account for -- it is the only thing that knows the full set, and each one
    /// holds the library alive until released.</summary>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;

        foreach (T element in _cache.Values)
        {
            (element as IDisposable)?.Dispose();
        }

        _cache.Clear();
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }

    public IEnumerator<T> GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
