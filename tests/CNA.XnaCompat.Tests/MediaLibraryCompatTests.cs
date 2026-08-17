using Xunit;
using XnaMediaLibrary = Microsoft.Xna.Framework.Media.MediaLibrary;
using XnaMediaSource = Microsoft.Xna.Framework.Media.MediaSource;
using XnaMediaSourceType = Microsoft.Xna.Framework.Media.MediaSourceType;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// Only <see cref="XnaMediaLibrary"/>'s two public constructors and
/// <see cref="XnaMediaSource.GetAvailableMediaSources"/> are reachable here -- every other type in
/// this feature (<c>Album</c>/<c>Artist</c>/<c>Genre</c>/<c>Playlist</c>/the collections) has an
/// <c>internal</c> constructor with no <c>InternalsVisibleTo</c> grant to this test project (same
/// limitation as every other <c>CNA.XnaCompat.Tests</c> file this session -- this project has no
/// <c>AssemblyInfo.cs</c> of its own). Since every collection is always empty anyway (see
/// <c>CNA.Media.MediaLibrary</c>'s own doc comment), the public surface tested here is also the
/// only part with anything real to verify.
///
/// <c>Picture</c>/<c>PictureAlbum</c> have the same <c>internal</c>-constructor limitation, so only
/// <c>RootPictureAlbum</c>/<c>Pictures</c>/<c>SavedPictures</c>/<c>GetPictureFromToken</c>'s
/// public surface (and <c>SavePicture</c>'s null-argument validation, which throws before ever
/// reaching <c>CNA.Media.SavedPictureStore</c>) can be exercised here. Nothing in this file calls
/// <c>SavePicture</c> with real data: its picture root resolves to the actual current user's real
/// Pictures folder in this environment -- see <c>CNA.Framework.Tests</c>'s own
/// <c>SavedPictureStoreTests</c> doc comment for the full reasoning, which applies identically
/// here.
/// </summary>
public class MediaLibraryCompatTests
{
    [Fact]
    public void Constructor_Default_UsesLocalDeviceMediaSource()
    {
        using var library = new XnaMediaLibrary();

        Assert.Equal(XnaMediaSourceType.LocalDevice, library.MediaSource.MediaSourceType);
        Assert.Equal("Local Device", library.MediaSource.Name);
    }

    [Fact]
    public void Constructor_Default_EveryCollectionStartsEmpty()
    {
        using var library = new XnaMediaLibrary();

        Assert.Equal(0, library.Songs.Count);
        Assert.Equal(0, library.Albums.Count);
        Assert.Equal(0, library.Artists.Count);
        Assert.Equal(0, library.Genres.Count);
        Assert.Equal(0, library.Playlists.Count);
    }

    [Fact]
    public void Constructor_NullMediaSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new XnaMediaLibrary(null!));
    }

    [Fact]
    public void Constructor_LocalDeviceMediaSource_Succeeds()
    {
        XnaMediaSource source = XnaMediaSource.GetAvailableMediaSources()[0];

        using var library = new XnaMediaLibrary(source);

        Assert.Same(source, library.MediaSource);
    }

    [Fact]
    public void Dispose_SetsIsDisposed()
    {
        var library = new XnaMediaLibrary();

        library.Dispose();

        Assert.True(library.IsDisposed);
    }

    [Fact]
    public void Albums_DisposingOneInstance_DoesNotAffectAnotherInstance()
    {
        // Regression test: an earlier version shared one static empty AlbumCollection across
        // every MediaLibrary instance to avoid a handful of small allocations, but
        // ReadOnlyMediaCollection<T>.Dispose() mutates a public IsDisposed flag -- sharing meant
        // disposing any one MediaLibrary's Albums silently marked every MediaLibrary's Albums
        // disposed, process-wide. A code review caught this before it shipped further.
        using var libraryA = new XnaMediaLibrary();
        using var libraryB = new XnaMediaLibrary();

        libraryA.Albums.Dispose();

        Assert.True(libraryA.Albums.IsDisposed);
        Assert.False(libraryB.Albums.IsDisposed);
    }

    [Fact]
    public void MediaSource_GetAvailableMediaSources_ReturnsExactlyLocalDevice()
    {
        IReadOnlyList<XnaMediaSource> sources = XnaMediaSource.GetAvailableMediaSources();

        Assert.Single(sources);
        Assert.Equal(XnaMediaSourceType.LocalDevice, sources[0].MediaSourceType);
    }

    [Fact]
    public void Constructor_Default_RootPictureAlbumIsNeverNull()
    {
        using var library = new XnaMediaLibrary();

        Assert.NotNull(library.RootPictureAlbum);
        Assert.Null(library.RootPictureAlbum.Parent);
        Assert.Equal(0, library.RootPictureAlbum.Albums.Count);
        Assert.Equal(0, library.RootPictureAlbum.Pictures.Count);
    }

    [Fact]
    public void Constructor_Default_PicturesAndSavedPicturesStartEmpty()
    {
        using var library = new XnaMediaLibrary();

        Assert.Equal(0, library.Pictures.Count);
        Assert.Equal(0, library.SavedPictures.Count);
    }

    [Fact]
    public void GetPictureFromToken_UnknownToken_ReturnsNull()
    {
        using var library = new XnaMediaLibrary();

        Assert.Null(library.GetPictureFromToken("nonexistent-token"));
    }

    [Fact]
    public void GetPictureFromToken_NullToken_ThrowsArgumentNullException()
    {
        using var library = new XnaMediaLibrary();

        Assert.Throws<ArgumentNullException>(() => library.GetPictureFromToken(null!));
    }

    [Fact]
    public void SavePicture_NullName_ThrowsArgumentNullException()
    {
        // Both overloads validate before ever reaching CNA.Media.SavedPictureStore -- safe to
        // exercise without touching the real Pictures folder, same reasoning as
        // CNA.Framework.Tests's MediaLibraryTests.
        using var library = new XnaMediaLibrary();

        Assert.Throws<ArgumentNullException>(() => library.SavePicture(null!, []));
        Assert.Throws<ArgumentNullException>(() => library.SavePicture(null!, Stream.Null));
    }

    [Fact]
    public void SavePicture_NullImageBuffer_ThrowsArgumentNullException()
    {
        using var library = new XnaMediaLibrary();

        Assert.Throws<ArgumentNullException>(() => library.SavePicture("name", (byte[])null!));
    }

    [Fact]
    public void SavePicture_NullStream_ThrowsArgumentNullException()
    {
        using var library = new XnaMediaLibrary();

        Assert.Throws<ArgumentNullException>(() => library.SavePicture("name", (Stream)null!));
    }

    [Fact]
    public void SavePicture_NullName_StreamOverload_DoesNotDrainStreamFirst()
    {
        // Regression test (code review finding), same reasoning as CNA.Framework.Tests's
        // MediaLibraryTests.
        using var library = new XnaMediaLibrary();
        using var stream = new MemoryStream([1, 2, 3]);

        Assert.Throws<ArgumentNullException>(() => library.SavePicture(null!, stream));
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void SavePicture_AfterDispose_ThrowsObjectDisposedException()
    {
        // Regression test (code review finding), same reasoning as CNA.Framework.Tests's
        // MediaLibraryTests -- safe to run against the real Pictures root since it fails before
        // ever reaching CNA.Media.SavedPictureStore.
        var library = new XnaMediaLibrary();
        library.Dispose();

        Assert.Throws<ObjectDisposedException>(() => library.SavePicture("name", []));
        Assert.Throws<ObjectDisposedException>(() => library.SavePicture("name", Stream.Null));
    }
}
