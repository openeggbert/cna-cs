using System.Collections;

namespace Microsoft.Xna.Framework.Media;

public sealed class PictureAlbumCollection : IEnumerable<PictureAlbum>, IDisposable
{
    private readonly MediaCollectionAdapter<PictureAlbum, CNA.Media.PictureAlbum> _collection;

    internal PictureAlbumCollection(CNA.Media.PictureAlbumCollection inner)
    {
        _collection = new(inner, item => new PictureAlbum(item));
    }

    ~PictureAlbumCollection() => _collection?.Dispose();

    public int Count => _collection.Count;

    public bool IsDisposed => _collection.IsDisposed;

    public PictureAlbum this[int index] => _collection.GetItem(index);

    public void Dispose()
    {
        _collection.Dispose();
        GC.SuppressFinalize(this);
    }

    public IEnumerator<PictureAlbum> GetEnumerator() => _collection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _collection.GetNonGenericEnumerator();
}
