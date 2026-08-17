using Xunit;
using XnaMediaPlayer = Microsoft.Xna.Framework.Media.MediaPlayer;
using XnaSongCollection = Microsoft.Xna.Framework.Media.SongCollection;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// Runs in its own test process (separate from <c>CNA.Framework.Tests</c>, per <c>dotnet test</c>'s
/// own per-project test hosts), so the shared static <c>CNA.Media.MediaPlayer</c> state this
/// forwards to starts fresh here -- same "never call Play with a real Song/SongCollection"
/// caution as <c>CNA.Tests.MediaPlayerTests</c>, for the same reason (mutating the shared queue
/// before the native call throws would leak into every later test in this assembly).
/// </summary>
public class MediaPlayerCompatTests
{
    [Fact]
    public void Play_NullSongCollection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => XnaMediaPlayer.Play((XnaSongCollection)null!));
    }

    [Fact]
    public void Play_NullSongCollectionWithIndex_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => XnaMediaPlayer.Play(null!, 0));
    }

    [Fact]
    public void IsShuffled_RoundTrips()
    {
        bool original = XnaMediaPlayer.IsShuffled;
        try
        {
            XnaMediaPlayer.IsShuffled = !original;
            Assert.Equal(!original, XnaMediaPlayer.IsShuffled);
        }
        finally
        {
            XnaMediaPlayer.IsShuffled = original;
        }
    }

    [Fact]
    public void MoveNext_WithEmptyQueue_IsNoOpAndDoesNotThrow()
    {
        XnaMediaPlayer.MoveNext();
    }

    [Fact]
    public void MovePrevious_WithEmptyQueue_IsNoOpAndDoesNotThrow()
    {
        XnaMediaPlayer.MovePrevious();
    }

    [Fact]
    public void ActiveSongChanged_AddAndRemoveHandler_DoesNotThrow()
    {
        void Handler(object? sender, EventArgs e)
        {
        }

        XnaMediaPlayer.ActiveSongChanged += Handler;
        XnaMediaPlayer.ActiveSongChanged -= Handler;
    }
}
