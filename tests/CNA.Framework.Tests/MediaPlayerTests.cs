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
/// later test in the run. <see cref="MediaQueueTests"/> covers the queue-mutation logic itself
/// against fresh, isolated <c>MediaQueue</c> instances instead, precisely to avoid this trap.
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
    public void Play_DisposedSong_ThrowsObjectDisposedException()
    {
        string path = Path.GetTempFileName();
        try
        {
            var song = new Song(path);
            song.Dispose();

            // Both checks run before the native call, so this is testable without a real
            // cna-native, same as the null check above.
            Assert.Throws<ObjectDisposedException>(() => MediaPlayer.Play(song));
        }
        finally
        {
            File.Delete(path);
        }
    }

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

    [Fact]
    public void DetectSongEndedByElapsedTime_ZeroDuration_NeverReportsEnded()
    {
        string path = Path.GetTempFileName();
        try
        {
            var song = new Song(path); // Duration defaults to TimeSpan.Zero.

            Assert.False(MediaPlayer.DetectSongEndedByElapsedTime(song, TimeSpan.Zero));
            Assert.False(MediaPlayer.DetectSongEndedByElapsedTime(song, TimeSpan.FromHours(1)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DetectSongEndedByElapsedTime_ElapsedBeforeDuration_ReturnsFalse()
    {
        string path = Path.GetTempFileName();
        try
        {
            var song = new Song(path, "name", durationMS: 5000);

            Assert.False(MediaPlayer.DetectSongEndedByElapsedTime(song, TimeSpan.FromSeconds(4)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DetectSongEndedByElapsedTime_ElapsedAtOrPastDuration_ReturnsTrue()
    {
        string path = Path.GetTempFileName();
        try
        {
            var song = new Song(path, "name", durationMS: 5000);

            Assert.True(MediaPlayer.DetectSongEndedByElapsedTime(song, TimeSpan.FromSeconds(5)));
            Assert.True(MediaPlayer.DetectSongEndedByElapsedTime(song, TimeSpan.FromSeconds(6)));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
