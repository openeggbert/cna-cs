using System.Collections;

namespace Microsoft.Xna.Framework.Media;

public sealed class AlbumCollection : IEnumerable<Album>, IDisposable
{
    private readonly MediaCollectionAdapter<Album, CNA.Media.Album> _collection;

    internal AlbumCollection(CNA.Media.AlbumCollection inner)
    {
        _collection = new(inner, item => new Album(item));
    }

    ~AlbumCollection() => _collection?.Dispose();

    public int Count => _collection.Count;

    public bool IsDisposed => _collection.IsDisposed;

    public Album this[int index] => _collection.GetItem(index);

    public void Dispose()
    {
        _collection.Dispose();
        GC.SuppressFinalize(this);
    }

    public IEnumerator<Album> GetEnumerator() => _collection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _collection.GetNonGenericEnumerator();
}
