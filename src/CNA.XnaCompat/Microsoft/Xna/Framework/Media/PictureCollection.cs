using System.Collections;

namespace Microsoft.Xna.Framework.Media;

public sealed class PictureCollection : IEnumerable<Picture>, IDisposable
{
    private readonly MediaCollectionAdapter<Picture, CNA.Media.Picture> _collection;

    internal PictureCollection(CNA.Media.PictureCollection inner)
    {
        _collection = new(inner, item => new Picture(item));
    }

    ~PictureCollection() => _collection?.Dispose();

    public int Count => _collection.Count;

    public bool IsDisposed => _collection.IsDisposed;

    public Picture this[int index] => _collection.GetItem(index);

    public void Dispose()
    {
        _collection.Dispose();
        GC.SuppressFinalize(this);
    }

    public IEnumerator<Picture> GetEnumerator() => _collection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _collection.GetNonGenericEnumerator();
}
