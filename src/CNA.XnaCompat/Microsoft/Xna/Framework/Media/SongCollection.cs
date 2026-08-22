using System.Collections;

namespace Microsoft.Xna.Framework.Media;

public sealed class SongCollection : IEnumerable<Song>, IDisposable
{
    private readonly MediaCollectionAdapter<Song, CNA.Media.Song> _collection;

    internal SongCollection(CNA.Media.SongCollection inner)
    {
        _collection = new(inner, item => new Song(item));
    }

    ~SongCollection() => _collection?.Dispose();

    internal CNA.Media.SongCollection Inner => (CNA.Media.SongCollection)_collection.Inner;

    public int Count => _collection.Count;

    public bool IsDisposed => _collection.IsDisposed;

    public Song this[int index] => _collection.GetItem(index);

    public void Dispose()
    {
        _collection.Dispose();
        GC.SuppressFinalize(this);
    }

    public IEnumerator<Song> GetEnumerator() => _collection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _collection.GetNonGenericEnumerator();
}
