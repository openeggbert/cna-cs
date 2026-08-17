using CNA.Media;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// Tests <see cref="SavedPictureStore"/> directly against a throwaway temp directory, never
/// against <see cref="MediaLibrary"/>'s own real picture root
/// (<see cref="Environment.GetFolderPath(Environment.SpecialFolder)"/> with
/// <see cref="Environment.SpecialFolder.MyPictures"/>)
/// -- that resolves to the actual current user's real Pictures folder in this environment, and no
/// automated test should ever write to it, even transiently. <see cref="SavedPictureStore"/> is
/// <c>internal</c>, reachable here via <c>InternalsVisibleTo</c>.
/// </summary>
public class SavedPictureStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "cna-tests-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void GetSavedPicturesDirectory_CreatesDirectory()
    {
        string? dir = SavedPictureStore.GetSavedPicturesDirectory(_root);

        Assert.NotNull(dir);
        Assert.True(Directory.Exists(dir));
        Assert.Equal("Saved Pictures", Path.GetFileName(dir));
    }

    [Fact]
    public void GetSavedPicturesDirectory_EmptyRoot_ReturnsNull()
    {
        Assert.Null(SavedPictureStore.GetSavedPicturesDirectory(""));
    }

    [Fact]
    public void SavePicture_WritesFileAndReturnsPath()
    {
        byte[] data = [1, 2, 3, 4];

        string? path = SavedPictureStore.SavePicture(_root, "my-picture", data);

        Assert.NotNull(path);
        Assert.True(File.Exists(path));
        Assert.Equal(data, File.ReadAllBytes(path));
    }

    [Theory]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 }, ".png")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0x00 }, ".jpg")]
    [InlineData(new byte[] { 0x42, 0x4D, 0x00 }, ".bmp")]
    [InlineData(new byte[] { 0x00, 0x01, 0x02 }, ".png")]
    public void SavePicture_SniffsExtensionFromMagicBytes(byte[] data, string expectedExtension)
    {
        string? path = SavedPictureStore.SavePicture(_root, "photo", data);

        Assert.NotNull(path);
        Assert.Equal(expectedExtension, Path.GetExtension(path));
    }

    [Theory]
    [InlineData("../../etc/passwd", "passwd.png")]
    [InlineData("..\\..\\evil", "evil.png")]
    [InlineData("/etc/cron.d/evil", "evil.png")]
    [InlineData("..", "picture.png")]
    [InlineData(".", "picture.png")]
    [InlineData("", "picture.png")]
    [InlineData("normal-name", "normal-name.png")]
    public void SavePicture_SanitizesNameAgainstPathTraversal(string name, string expectedFileName)
    {
        string? path = SavedPictureStore.SavePicture(_root, name, [0x00]);

        Assert.NotNull(path);
        Assert.Equal(expectedFileName, Path.GetFileName(path));
        // The written file must actually be inside the Saved Pictures directory, not escape it.
        string savedPicturesDir = Path.Combine(_root, "Saved Pictures");
        Assert.Equal(savedPicturesDir, Path.GetDirectoryName(path));
    }
}
