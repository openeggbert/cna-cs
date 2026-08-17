using CNA.Media;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="MediaLibrary"/>'s construction and validation are pure managed logic -- no native
/// dependency at all, fully testable, unlike almost everything else native-adjacent this session
/// has added. See <see cref="MediaLibrary"/>'s own doc comment for why every *music* collection it
/// exposes is always empty (a deliberate scope decision, not something these tests are working
/// around) -- the *picture* side is genuinely real (see that type's own doc comment), but nothing
/// here calls <see cref="MediaLibrary.SavePicture(string,byte[])"/>: its real picture root
/// resolves to the actual current user's real Pictures folder in this environment
/// (<c>Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)</c>), and no automated test
/// should ever write there. <c>SavedPictureStoreTests</c> covers the actual file-writing logic
/// against a throwaway temp directory instead.
/// </summary>
public class MediaLibraryTests
{
    [Fact]
    public void Constructor_Default_UsesLocalDeviceMediaSource()
    {
        using var library = new MediaLibrary();

        Assert.Equal(MediaSourceType.LocalDevice, library.MediaSource.MediaSourceType);
        Assert.Equal("Local Device", library.MediaSource.Name);
    }

    [Fact]
    public void Constructor_Default_EveryCollectionStartsEmpty()
    {
        using var library = new MediaLibrary();

        Assert.Equal(0, library.Songs.Count);
        Assert.Equal(0, library.Albums.Count);
        Assert.Equal(0, library.Artists.Count);
        Assert.Equal(0, library.Genres.Count);
        Assert.Equal(0, library.Playlists.Count);
    }

    [Fact]
    public void Constructor_NullMediaSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MediaLibrary(null!));
    }

    [Fact]
    public void Constructor_NonLocalDeviceMediaSource_ThrowsNotSupportedException()
    {
        // MediaSource's constructor is internal (reachable here via InternalsVisibleTo), matching
        // real XNA's own MediaLibrary-only construction -- see MediaSource's own doc comment.
        var windowsMediaConnectSource = new MediaSource(MediaSourceType.WindowsMediaConnect, "Remote");

        Assert.Throws<NotSupportedException>(() => new MediaLibrary(windowsMediaConnectSource));
    }

    [Fact]
    public void Constructor_LocalDeviceMediaSource_Succeeds()
    {
        var source = MediaSource.GetAvailableMediaSources()[0];

        using var library = new MediaLibrary(source);

        Assert.Same(source, library.MediaSource);
    }

    [Fact]
    public void Dispose_SetsIsDisposed()
    {
        var library = new MediaLibrary();

        library.Dispose();

        Assert.True(library.IsDisposed);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var library = new MediaLibrary();

        library.Dispose();
        library.Dispose();
    }

    [Fact]
    public void MediaSource_GetAvailableMediaSources_ReturnsExactlyLocalDevice()
    {
        IReadOnlyList<MediaSource> sources = MediaSource.GetAvailableMediaSources();

        Assert.Single(sources);
        Assert.Equal(MediaSourceType.LocalDevice, sources[0].MediaSourceType);
    }

    [Fact]
    public void Constructor_Default_RootPictureAlbumIsNeverNull()
    {
        using var library = new MediaLibrary();

        Assert.NotNull(library.RootPictureAlbum);
        Assert.Null(library.RootPictureAlbum.Parent);
        Assert.Equal(0, library.RootPictureAlbum.Albums.Count);
        Assert.Equal(0, library.RootPictureAlbum.Pictures.Count);
    }

    [Fact]
    public void Constructor_Default_PicturesAndSavedPicturesStartEmpty()
    {
        using var library = new MediaLibrary();

        Assert.Equal(0, library.Pictures.Count);
        Assert.Equal(0, library.SavedPictures.Count);
    }

    [Fact]
    public void GetPictureFromToken_UnknownToken_ReturnsNull()
    {
        using var library = new MediaLibrary();

        Assert.Null(library.GetPictureFromToken("nonexistent-token"));
    }

    [Fact]
    public void GetPictureFromToken_NullToken_ThrowsArgumentNullException()
    {
        using var library = new MediaLibrary();

        Assert.Throws<ArgumentNullException>(() => library.GetPictureFromToken(null!));
    }

    [Fact]
    public void SavePicture_NullName_ThrowsArgumentNullException()
    {
        // Both overloads validate before ever reaching SavedPictureStore (and thus before
        // touching the real filesystem at all) -- testable without touching the real Pictures
        // folder, same "validation-failure paths are testable even when the type as a whole can't
        // be" pattern established for VertexBuffer/IndexBuffer's constructors.
        using var library = new MediaLibrary();

        Assert.Throws<ArgumentNullException>(() => library.SavePicture(null!, []));
        Assert.Throws<ArgumentNullException>(() => library.SavePicture(null!, Stream.Null));
    }

    [Fact]
    public void SavePicture_NullImageBuffer_ThrowsArgumentNullException()
    {
        using var library = new MediaLibrary();

        Assert.Throws<ArgumentNullException>(() => library.SavePicture("name", (byte[])null!));
    }

    [Fact]
    public void SavePicture_NullStream_ThrowsArgumentNullException()
    {
        using var library = new MediaLibrary();

        Assert.Throws<ArgumentNullException>(() => library.SavePicture("name", (Stream)null!));
    }
}
