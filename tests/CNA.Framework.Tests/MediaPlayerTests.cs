using CNA.Media;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// What survives after <see cref="MediaPlayer"/> was rebound onto the full
/// <c>media_player.h</c> surface, and why so little does.
///
/// This file used to hold 15 tests. They passed with no native library present because the class
/// kept its own managed queue, shuffle order, state machine and playback timer -- so they were
/// exercising a second implementation of the engine's behaviour rather than the player. That
/// duplicate is gone: state, position, queue and transport are all one native round trip each now,
/// which is the correction, and it means those tests have nothing to run against here.
///
/// What is left is the argument validation that happens before the first native call.
/// <c>CNA.Tests.MediaLibraryTests</c> records the same thing for the same reason.
/// </summary>
public class MediaPlayerTests
{
    [Fact]
    public void Play_NullSong_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MediaPlayer.Play((Song)null!));
    }

    [Fact]
    public void Play_NullSongCollection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MediaPlayer.Play((SongCollection)null!));
    }

    [Fact]
    public void Play_NullSongCollectionWithIndex_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MediaPlayer.Play((SongCollection)null!, 0));
    }

    [Fact]
    public void DetectSongEndedByElapsedTime_NullSong_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => MediaPlayer.DetectSongEndedByElapsedTime(null!, TimeSpan.Zero));
    }

    [Fact]
    public void GetVisualizationData_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MediaPlayer.GetVisualizationData(null!));
    }
}
