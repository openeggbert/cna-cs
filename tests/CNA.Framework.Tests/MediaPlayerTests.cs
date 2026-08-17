using CNA.Media;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="MediaPlayer"/> is a process-global static class, so these tests rely on
/// <see cref="MediaPlayer.State"/> reliably staying <see cref="MediaState.Stopped"/> and
/// <see cref="MediaPlayer.Queue"/> reliably staying empty throughout this whole test run: no test
/// anywhere in this assembly ever calls <c>MediaPlayer.Play</c> with a real, non-null,
/// non-disposed <see cref="Song"/> -- doing so would mutate <see cref="MediaPlayer.Queue"/> (via
/// <c>LoadSong</c>) *before* the native call that ultimately throws, leaking state into every
/// later test in the run. This class also no longer covers any test that needs to construct a real
/// <see cref="Song"/> at all (since step 10 of the native-ABI migration made that a real
/// <c>cna_song_create</c> call -- see <c>NEXT.md</c>) -- the former dedicated queue-mutation test
/// class against fresh, isolated <c>MediaQueue</c> instances was removed for the same reason.
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

    // Play_DisposedSong_ThrowsObjectDisposedException was removed here -- needed to construct a
    // real Song, which now requires a native cna_song_create call (step 10 of the native-ABI
    // migration; see NEXT.md). No longer testable without a real cna-native and a running game.

    [Fact]
    public void Pause_WhenNotPlaying_IsNoOpAndDoesNotThrow()
    {
        // Pause()'s own guard (State != Playing) returns before ever reaching the native call, so
        // this doesn't need a real cna-native to exercise -- matching the "validation-failure
        // paths are testable even when the type as a whole can't be" pattern established for
        // VertexBuffer/IndexBuffer's constructors.
        MediaPlayer.Pause();
    }

    [Fact]
    public void Resume_WhenNotPaused_IsNoOpAndDoesNotThrow()
    {
        MediaPlayer.Resume();
    }

    [Fact]
    public void Stop_WhenAlreadyStopped_IsNoOpAndDoesNotThrow()
    {
        MediaPlayer.Stop();
    }

    [Fact]
    public void PlayPosition_WithNothingEverPlayed_IsZero()
    {
        Assert.Equal(TimeSpan.Zero, MediaPlayer.PlayPosition);
    }

    [Fact]
    public void IsRepeating_RoundTrips()
    {
        bool original = MediaPlayer.IsRepeating;
        try
        {
            MediaPlayer.IsRepeating = !original;
            Assert.Equal(!original, MediaPlayer.IsRepeating);
        }
        finally
        {
            MediaPlayer.IsRepeating = original;
        }
    }

    [Fact]
    public void IsShuffled_RoundTrips()
    {
        bool original = MediaPlayer.IsShuffled;
        try
        {
            MediaPlayer.IsShuffled = !original;
            Assert.Equal(!original, MediaPlayer.IsShuffled);
        }
        finally
        {
            MediaPlayer.IsShuffled = original;
        }
    }

    [Fact]
    public void Queue_WithNothingEverPlayed_IsEmpty()
    {
        Assert.Equal(0, MediaPlayer.Queue.Count);
        Assert.Null(MediaPlayer.Queue.ActiveSong);
    }

    [Fact]
    public void MoveNext_WithEmptyQueue_IsNoOpAndDoesNotThrow()
    {
        // NextSong() calls Stop() first (a no-op here, State is already Stopped), then returns
        // early because the queue is empty -- neither path reaches native code.
        MediaPlayer.MoveNext();
    }

    [Fact]
    public void MovePrevious_WithEmptyQueue_IsNoOpAndDoesNotThrow()
    {
        MediaPlayer.MovePrevious();
    }

    [Fact]
    public void Update_WithEmptyQueue_IsNoOpAndDoesNotThrow()
    {
        // Update() returns immediately because Queue.ActiveSong is null -- never reaches
        // DetectSongEndedByElapsedTime or any native call.
        MediaPlayer.Update();
    }

    [Fact]
    public void DetectSongEndedByElapsedTime_NullSong_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => MediaPlayer.DetectSongEndedByElapsedTime(null!, TimeSpan.Zero));
    }

    // DetectSongEndedByElapsedTime_ZeroDuration_NeverReportsEnded/_ElapsedBeforeDuration_ReturnsFalse/
    // _ElapsedAtOrPastDuration_ReturnsTrue were removed here -- all three needed to construct a real
    // Song, which now requires a native cna_song_create call (step 10 of the native-ABI migration;
    // see NEXT.md). No longer testable without a real cna-native and a running game.

    [Fact]
    public void IsVisualizationEnabled_DefaultsFalse()
    {
        // The getter is a pure C# field read, no native call -- and since the setter (which does
        // make a native call) can never succeed without a real cna-native, this can only ever
        // observe the untouched default across this whole test run.
        Assert.False(MediaPlayer.IsVisualizationEnabled);
    }

    [Fact]
    public void GetVisualizationData_NullData_ThrowsArgumentNullException()
    {
        // Validated before the native call, same "validation-failure paths are testable even when
        // the type as a whole can't be" pattern as the rest of this file.
        Assert.Throws<ArgumentNullException>(() => MediaPlayer.GetVisualizationData(null!));
    }
}
