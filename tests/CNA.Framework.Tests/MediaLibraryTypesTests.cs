using CNA.Media;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="Album"/>/<see cref="Artist"/>/<see cref="Genre"/>/<see cref="Playlist"/> and their
/// collections all have <c>internal</c> constructors (matching real XNA's own <c>MediaLibrary</c>-only
/// construction -- see each type's own doc comment), reachable here via <c>InternalsVisibleTo</c>.
/// Pure managed logic throughout, no native dependency.
/// </summary>
public class MediaLibraryTypesTests
{
    private static Artist CreateArtist(string name = "artist") => new(name, new AlbumCollection([]), new SongCollection([]));

    private static Genre CreateGenre(string name = "genre") => new(name, new AlbumCollection([]), new SongCollection([]));

    [Fact]
    public void Artist_Equals_SameName_AreEqual()
    {
        Artist a = CreateArtist("Queen");
        Artist b = CreateArtist("Queen");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Artist_Equals_DifferentName_AreNotEqual()
    {
        Artist a = CreateArtist("Queen");
        Artist b = CreateArtist("ABBA");

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Genre_Equals_SameName_AreEqual()
    {
        Genre a = CreateGenre("Rock");
        Genre b = CreateGenre("Rock");

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Playlist_Equals_SameName_AreEqual()
    {
        var a = new Playlist("Favorites", new SongCollection([]), TimeSpan.FromMinutes(30));
        var b = new Playlist("Favorites", new SongCollection([]), TimeSpan.FromMinutes(45));

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Album_Equals_SameNameAndArtist_AreEqual()
    {
        Artist artist = CreateArtist("Queen");
        var a = new Album("A Night at the Opera", artist, null, TimeSpan.Zero, new SongCollection([]));
        var b = new Album("A Night at the Opera", artist, null, TimeSpan.Zero, new SongCollection([]));

        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Album_Equals_SameNameDifferentArtist_AreNotEqual()
    {
        var a = new Album("Greatest Hits", CreateArtist("Queen"), null, TimeSpan.Zero, new SongCollection([]));
        var b = new Album("Greatest Hits", CreateArtist("ABBA"), null, TimeSpan.Zero, new SongCollection([]));

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Album_Equals_SameNameEquivalentArtist_AreEqual()
    {
        // Same artist *name* but distinct Artist instances -- Album.Equals delegates to
        // Artist.Equals (name-based), not reference equality, matching the real C++ engine.
        var a = new Album("Greatest Hits", CreateArtist("Queen"), null, TimeSpan.Zero, new SongCollection([]));
        var b = new Album("Greatest Hits", CreateArtist("Queen"), null, TimeSpan.Zero, new SongCollection([]));

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Album_Equals_BothNullArtist_AreEqual()
    {
        var a = new Album("Compilation", null, null, TimeSpan.Zero, new SongCollection([]));
        var b = new Album("Compilation", null, null, TimeSpan.Zero, new SongCollection([]));

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Album_HasArt_IsAlwaysFalse()
    {
        var album = new Album("Name", null, null, TimeSpan.Zero, new SongCollection([]));

        Assert.False(album.HasArt);
    }

    [Fact]
    public void Album_GetAlbumArt_Throws()
    {
        var album = new Album("Name", null, null, TimeSpan.Zero, new SongCollection([]));

        Assert.Throws<InvalidOperationException>(() => album.GetAlbumArt());
    }

    [Fact]
    public void Album_GetThumbnail_Throws()
    {
        var album = new Album("Name", null, null, TimeSpan.Zero, new SongCollection([]));

        Assert.Throws<InvalidOperationException>(() => album.GetThumbnail());
    }

    [Fact]
    public void AlbumCollection_IndexerAndEnumeration_MatchConstructorOrder()
    {
        var albumA = new Album("A", null, null, TimeSpan.Zero, new SongCollection([]));
        var albumB = new Album("B", null, null, TimeSpan.Zero, new SongCollection([]));
        var collection = new AlbumCollection([albumA, albumB]);

        Assert.Equal(2, collection.Count);
        Assert.Same(albumA, collection[0]);
        Assert.Same(albumB, collection[1]);
        Assert.Equal([albumA, albumB], collection);
    }

    [Fact]
    public void AlbumCollection_Dispose_SetsIsDisposed()
    {
        var collection = new AlbumCollection([]);

        collection.Dispose();

        Assert.True(collection.IsDisposed);
    }

    // Song_AlbumArtistGenre_DefaultToNull/_SettableInternally were removed here -- both needed to
    // construct a real Song, which now requires a native cna_song_create call (step 10 of the
    // native-ABI migration; see NEXT.md). No longer testable without a real cna-native and a
    // running game, the same reason GraphicsDevice/SpriteBatch/VertexBuffer/etc. already have no
    // unit tests.
}
