using Xunit;
using XnaSong = Microsoft.Xna.Framework.Media.Song;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// Song is content-pipeline/static-factory constructed in XNA. The missing-file path is still
/// testable without loading the native library because validation happens first.
/// </summary>
public class SongCompatTests
{
    [Fact]
    public void FromUri_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp3");

        Assert.Throws<FileNotFoundException>(() => XnaSong.FromUri("missing", new Uri(missingPath)));
    }
}
