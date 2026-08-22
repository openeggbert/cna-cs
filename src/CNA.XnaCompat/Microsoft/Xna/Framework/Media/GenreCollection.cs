using System.Collections;

namespace Microsoft.Xna.Framework.Media;

public sealed class GenreCollection : IEnumerable<Genre>, IDisposable
{
    private readonly MediaCollectionAdapter<Genre, CNA.Media.Genre> _collection;

    internal GenreCollection(CNA.Media.GenreCollection inner)
    {
        _collection = new(inner, item => new Genre(item));
    }

    ~GenreCollection() => _collection?.Dispose();

    public int Count => _collection.Count;

    public bool IsDisposed => _collection.IsDisposed;

    public Genre this[int index] => _collection.GetItem(index);

    public void Dispose()
    {
        _collection.Dispose();
        GC.SuppressFinalize(this);
    }

    public IEnumerator<Genre> GetEnumerator() => _collection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _collection.GetNonGenericEnumerator();
}
