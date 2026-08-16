using Xunit;
using XnaMediaPlayer = Microsoft.Xna.Framework.Media.MediaPlayer;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// Runs in its own test process (separate from <c>CNA.Framework.Tests</c>, per <c>dotnet test</c>'s
/// own per-project test hosts), so the shared static <c>CNA.Media.MediaPlayer</c> state this
/// forwards to starts fresh here -- same "never call Play with a real Song" caution as
/// <c>CNA.Tests.MediaPlayerTests</c>, for the same reason.
/// </summary>
public class MediaPlayerCompatTests
{
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
