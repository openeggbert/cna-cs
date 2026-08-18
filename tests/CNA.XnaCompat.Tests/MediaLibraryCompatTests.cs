using Microsoft.Xna.Framework.Media;
using Xunit;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// The compat half of what <c>CNA.Tests.MediaLibraryTests</c> documents: after the media-library
/// rebinding, only the argument validation that runs before the first native call is testable
/// without <c>cna-native</c> loaded.
/// </summary>
public class MediaLibraryCompatTests
{
    [Fact]
    public void Constructor_NullMediaSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MediaLibrary(null!));
    }
}
