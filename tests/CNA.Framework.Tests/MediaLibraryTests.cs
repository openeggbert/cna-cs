using CNA.Media;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="MediaLibrary"/>'s construction and validation are pure managed logic -- no native
/// dependency at all, fully testable, unlike almost everything else native-adjacent this session
/// has added. See <see cref="MediaLibrary"/>'s own doc comment for why every collection it exposes
/// is always empty (a deliberate scope decision, not something these tests are working around).
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
}
