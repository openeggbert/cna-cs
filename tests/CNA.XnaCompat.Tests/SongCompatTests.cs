using Xunit;
using XnaSong = Microsoft.Xna.Framework.Media.Song;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// <see cref="XnaSong"/> extends <c>CNA.Media.Song</c> directly (see that compat type's own doc
/// comment) -- exercised here directly, unlike this session's other native-backed compat types,
/// since <c>Song</c> construction is pure managed logic with no native dependency at all.
/// </summary>
public class SongCompatTests
{
    [Fact]
    public void Constructor_FileExists_Succeeds()
    {
        string path = Path.GetTempFileName();
        try
        {
            var song = new XnaSong(path, "My Song");

            Assert.Equal("My Song", song.Name);
            Assert.False(song.IsDisposed);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Constructor_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp3");

        Assert.Throws<FileNotFoundException>(() => new XnaSong(missingPath));
    }

    [Fact]
    public void AlbumArtistGenre_DefaultToNull()
    {
        string path = Path.GetTempFileName();
        try
        {
            var song = new XnaSong(path);

            Assert.Null(song.Album);
            Assert.Null(song.Artist);
            Assert.Null(song.Genre);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromUri_PlainPath_ReturnsFrameworkNamespacedSong()
    {
        string path = Path.GetTempFileName();
        try
        {
            XnaSong song = XnaSong.FromUri("name", path);

            Assert.IsType<XnaSong>(song);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
