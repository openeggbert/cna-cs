using System.Collections;

namespace Microsoft.Xna.Framework.Media;

public sealed class PlaylistCollection : IEnumerable<Playlist>, IDisposable
{
    private readonly MediaCollectionAdapter<Playlist, CNA.Media.Playlist> _collection;

    internal PlaylistCollection(CNA.Media.PlaylistCollection inner)
    {
        _collection = new(inner, item => new Playlist(item));
    }

    ~PlaylistCollection() => _collection?.Dispose();

    public int Count => _collection.Count;

    public bool IsDisposed => _collection.IsDisposed;

    public Playlist this[int index] => _collection.GetItem(index);

    public void Dispose()
    {
        _collection.Dispose();
        GC.SuppressFinalize(this);
    }

    public IEnumerator<Playlist> GetEnumerator() => _collection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _collection.GetNonGenericEnumerator();
}
