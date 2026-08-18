using Xunit;
using XnaSong = Microsoft.Xna.Framework.Media.Song;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// <see cref="XnaSong"/> extends <c>CNA.Media.Song</c> directly (see that compat type's own doc
/// comment). Song construction now requires a real native <c>cna_song_create</c> call and a
/// running game (step 10 of the native-ABI migration; see <c>NEXT.md</c>) -- unlike when this file
/// was first written, before any real ABI existed, when it was pure managed logic with no native
/// dependency at all. Only <see cref="Constructor_FileDoesNotExist_ThrowsFileNotFoundException"/>
/// survives that change: its file-existence check runs (and throws) before the constructor ever
/// reaches native code, the same "validation-failure paths are testable even when the type as a
/// whole can't be" pattern this migration already established elsewhere (see
/// <c>MediaPlayerTests.cs</c>). The rest of this file's tests were removed, matching the same
/// precedent as <c>SongTests.cs</c>'s full deletion on the base <c>CNA.Media.Song</c> side.
/// </summary>
public class SongCompatTests
{
    [Fact]
    public void Constructor_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp3");

        Assert.Throws<FileNotFoundException>(() => new XnaSong(missingPath));
    }
}
