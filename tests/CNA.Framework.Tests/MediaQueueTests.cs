using CNA.Media;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="MediaQueue"/>'s constructor and <c>Add</c>/<c>Clear</c> are <c>internal</c> (same
/// real-XNA-matching encapsulation as real XNA's own type -- see <see cref="MediaQueue"/>'s own
/// doc comment), reachable here via <c>InternalsVisibleTo</c>. Tested against fresh, isolated
/// instances rather than the shared static <c>MediaPlayer.Queue</c> deliberately: populating
/// <c>MediaPlayer.Queue</c> at all requires a successful (or partially-executed) <c>Play</c> call,
/// and <c>MediaPlayer</c> is a process-global static, so mutating it here would leak into every
/// other test in this assembly for the rest of the run. A fresh <see cref="MediaQueue"/> has none
/// of that risk.
/// </summary>
public class MediaQueueTests : IDisposable
{
    private readonly List<string> _tempPaths = [];

    private Song CreateSong(string name = "song")
    {
        string path = Path.GetTempFileName();
        _tempPaths.Add(path);
        return new Song(path, name);
    }

    public void Dispose()
    {
        foreach (string path in _tempPaths)
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Constructor_StartsEmptyWithNoActiveSong()
    {
        var queue = new MediaQueue();

        Assert.Equal(0, queue.Count);
        Assert.Null(queue.ActiveSong);
        Assert.Equal(-1, queue.ActiveSongIndex);
    }

    [Fact]
    public void Add_IncreasesCount()
    {
        var queue = new MediaQueue();
        Song song = CreateSong();

        queue.Add(song);

        Assert.Equal(1, queue.Count);
        Assert.Same(song, queue[0]);
    }

    [Fact]
    public void ActiveSong_IndexWithinBounds_ReturnsThatSong()
    {
        var queue = new MediaQueue();
        Song songA = CreateSong("a");
        Song songB = CreateSong("b");
        queue.Add(songA);
        queue.Add(songB);

        queue.ActiveSongIndex = 1;

        Assert.Same(songB, queue.ActiveSong);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void ActiveSong_IndexOutOfBounds_ReturnsNull(int index)
    {
        var queue = new MediaQueue();
        queue.Add(CreateSong());
        queue.Add(CreateSong());

        queue.ActiveSongIndex = index;

        Assert.Null(queue.ActiveSong);
    }

    [Fact]
    public void Clear_ResetsCountAndActiveSongIndex()
    {
        var queue = new MediaQueue();
        queue.Add(CreateSong());
        queue.ActiveSongIndex = 0;

        queue.Clear();

        Assert.Equal(0, queue.Count);
        Assert.Equal(-1, queue.ActiveSongIndex);
        Assert.Null(queue.ActiveSong);
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        var queue = new MediaQueue();
        queue.Add(CreateSong());

        Assert.Throws<ArgumentOutOfRangeException>(() => queue[5]);
    }

    [Fact]
    public void Enumeration_YieldsSongsInOrder()
    {
        var queue = new MediaQueue();
        Song songA = CreateSong("a");
        Song songB = CreateSong("b");
        queue.Add(songA);
        queue.Add(songB);

        Assert.Equal([songA, songB], queue);
    }
}
