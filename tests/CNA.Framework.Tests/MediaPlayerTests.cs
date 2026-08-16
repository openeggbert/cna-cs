using CNA.Media;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="MediaPlayer"/> is a process-global static class, so these tests rely on
/// <see cref="MediaPlayer.State"/> reliably staying <see cref="MediaState.Stopped"/> throughout
/// this whole test run: <see cref="MediaPlayer.Play"/> always throws before ever assigning
/// <c>State</c> (no real <c>cna-native</c> in this environment), so nothing here can ever advance
/// it past its initial default -- see each test's own reasoning.
/// </summary>
public class MediaPlayerTests
{
    [Fact]
    public void Play_NullSong_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MediaPlayer.Play(null!));
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
}
