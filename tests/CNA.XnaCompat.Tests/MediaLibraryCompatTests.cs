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
    public void MediaSource_GetAvailableMediaSources_ReturnsExactlyLocalDevice()
    {
        IReadOnlyList<XnaMediaSource> sources = XnaMediaSource.GetAvailableMediaSources();

        Assert.Single(sources);
        Assert.Equal(XnaMediaSourceType.LocalDevice, sources[0].MediaSourceType);
    }
}
