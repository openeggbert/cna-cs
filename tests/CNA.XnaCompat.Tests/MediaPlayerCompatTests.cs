using Microsoft.Xna.Framework.Media;
using Xunit;

namespace CNA.XnaCompat.Tests;

/// <summary>The compat half of what <c>CNA.Tests.MediaPlayerTests</c> records: after the rebinding,
/// only the argument validation that runs before the first native call is testable without
/// <c>cna-native</c> loaded.</summary>
public class MediaPlayerCompatTests
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
    public void GetVisualizationData_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MediaPlayer.GetVisualizationData(null!));
    }
}
