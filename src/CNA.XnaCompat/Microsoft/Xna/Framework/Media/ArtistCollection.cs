using System.Collections;

namespace Microsoft.Xna.Framework.Media;

public sealed class ArtistCollection : IEnumerable<Artist>, IDisposable
{
    private readonly MediaCollectionAdapter<Artist, CNA.Media.Artist> _collection;

    internal ArtistCollection(CNA.Media.ArtistCollection inner)
    {
        _collection = new(inner, item => new Artist(item));
    }

    ~ArtistCollection() => _collection?.Dispose();

    public int Count => _collection.Count;

    public bool IsDisposed => _collection.IsDisposed;

    public Artist this[int index] => _collection.GetItem(index);

    public void Dispose()
    {
        _collection.Dispose();
        GC.SuppressFinalize(this);
    }

    public IEnumerator<Artist> GetEnumerator() => _collection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _collection.GetNonGenericEnumerator();
}
