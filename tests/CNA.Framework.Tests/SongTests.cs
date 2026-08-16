using CNA.Media;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="Song"/>'s construction is pure managed logic (a file-existence check, no native
/// call) -- real and testable today against real temporary files, unlike almost every other
/// native-backed type this session has added. See <see cref="Song"/>'s own doc comment.
/// </summary>
public class SongTests
{
    [Fact]
    public void Constructor_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp3");

        Assert.Throws<FileNotFoundException>(() => new Song(missingPath));
    }

    [Fact]
    public void Constructor_FileExists_StoresNameAsGiven()
    {
        string path = Path.GetTempFileName();
        try
        {
            var song = new Song(path, "My Song");

            Assert.Equal("My Song", song.Name);
            Assert.Equal(TimeSpan.Zero, song.Duration);
            Assert.False(song.IsDisposed);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Constructor_EmptyNameNotDefaulted_MatchesVerifiedRealBehavior()
    {
        // The real C++ engine's own Song.hpp doc comment claims an empty name "defaults to the
        // file name", but its actual Song.cpp constructor body just stores the given name as-is,
        // even when empty -- a real doc/code mismatch upstream. Reproduces the verified *behavior*
        // (from the .cpp), not the inaccurate doc comment.
        string path = Path.GetTempFileName();
        try
        {
            var song = new Song(path);

            Assert.Equal(string.Empty, song.Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Constructor_WithDurationMS_SetsDuration()
    {
        string path = Path.GetTempFileName();
        try
        {
            var song = new Song(path, "asset", 1500);

            Assert.Equal(TimeSpan.FromMilliseconds(1500), song.Duration);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Dispose_SetsIsDisposed()
    {
        string path = Path.GetTempFileName();
        try
        {
            var song = new Song(path);

            song.Dispose();

            Assert.True(song.IsDisposed);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Equals_SamePath_AreEqual()
    {
        string path = Path.GetTempFileName();
        try
        {
            var songA = new Song(path, "A");
            var songB = new Song(path, "B");

            Assert.True(songA.Equals(songB));
            Assert.True(songA == songB);
            Assert.Equal(songA.GetHashCode(), songB.GetHashCode());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Equals_DifferentPaths_AreNotEqual()
    {
        string pathA = Path.GetTempFileName();
        string pathB = Path.GetTempFileName();
        try
        {
            var songA = new Song(pathA, "A");
            var songB = new Song(pathB, "A");

            Assert.False(songA.Equals(songB));
            Assert.True(songA != songB);
        }
        finally
        {
            File.Delete(pathA);
            File.Delete(pathB);
        }
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        string path = Path.GetTempFileName();
        try
        {
            var song = new Song(path, "Track Name");

            Assert.Equal("Track Name", song.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromUri_PlainPath_UsesPathDirectly()
    {
        string path = Path.GetTempFileName();
        try
        {
            var song = Song.FromUri("name", path);

            Assert.Equal(path, song.Handle);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromUri_FileUri_ResolvesToLocalPath()
    {
        string path = Path.GetTempFileName();
        try
        {
            var uri = new Uri(path).AbsoluteUri;

            var song = Song.FromUri("name", uri);

            Assert.Equal(path, song.Handle);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromUri_NonFileScheme_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Song.FromUri("name", "http://example.com/song.mp3"));
    }
}
