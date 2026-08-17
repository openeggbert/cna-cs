using CNA.Media;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="Picture"/>/<see cref="PictureAlbum"/> have <c>internal</c> constructors (matching
/// real XNA's own <c>MediaLibrary</c>-only construction), reachable here via
/// <c>InternalsVisibleTo</c>. <see cref="Picture.GetImage"/>/<see cref="Picture.GetThumbnail"/> are
/// tested against real temporary files, never against a real picture library path -- see
/// <c>SavedPictureStoreTests</c>'s own doc comment for why nothing in this session touches the
/// real environment's actual Pictures folder.
/// </summary>
public class PictureLibraryTests
{
    private static Picture CreatePicture(string path, string name = "picture") =>
        new(name, album: null, width: 10, height: 20, DateTime.Now, path);

    [Fact]
    public void Picture_Equals_SamePath_AreEqual()
    {
        Picture a = CreatePicture("/some/path.png", "a");
        Picture b = CreatePicture("/some/path.png", "b");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Picture_Equals_DifferentPath_AreNotEqual()
    {
        Picture a = CreatePicture("/some/a.png");
        Picture b = CreatePicture("/some/b.png");

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Picture_Token_ReturnsPath()
    {
        Picture picture = CreatePicture("/some/path.png");

        Assert.Equal("/some/path.png", picture.Token);
    }

    [Fact]
    public void Picture_GetImage_OpensRealFileStream()
    {
        string path = Path.GetTempFileName();
        try
        {
            byte[] content = [1, 2, 3, 4, 5];
            File.WriteAllBytes(path, content);
            Picture picture = CreatePicture(path);

            using Stream stream = picture.GetImage();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);

            Assert.Equal(content, memory.ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Picture_GetThumbnail_FallsBackToFullImage()
    {
        string path = Path.GetTempFileName();
        try
        {
            byte[] content = [9, 8, 7];
            File.WriteAllBytes(path, content);
            Picture picture = CreatePicture(path);

            using Stream stream = picture.GetThumbnail();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);

            Assert.Equal(content, memory.ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Picture_Dispose_SetsIsDisposed()
    {
        Picture picture = CreatePicture("/some/path.png");

        picture.Dispose();

        Assert.True(picture.IsDisposed);
    }

    [Fact]
    public void PictureAlbum_Equals_SamePath_AreEqual()
    {
        var a = new PictureAlbum("Root", null, "/pictures");
        var b = new PictureAlbum("Root", null, "/pictures");

        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void PictureAlbum_Equals_DifferentPath_AreNotEqual()
    {
        var a = new PictureAlbum("A", null, "/pictures/a");
        var b = new PictureAlbum("B", null, "/pictures/b");

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void PictureAlbum_ParentChild_TracksRelationship()
    {
        var parent = new PictureAlbum("Root", null, "/pictures");
        var child = new PictureAlbum("Vacation", parent, "/pictures/vacation");

        Assert.Same(parent, child.Parent);
        Assert.Null(parent.Parent);
    }

    [Fact]
    public void PictureAlbum_SetChildAlbumsAndPictures_PopulatesEmptyCollections()
    {
        var album = new PictureAlbum("Root", null, "/pictures");

        album.SetChildAlbumsAndPictures();

        Assert.NotNull(album.Albums);
        Assert.Equal(0, album.Albums.Count);
        Assert.NotNull(album.Pictures);
        Assert.Equal(0, album.Pictures.Count);
    }

    [Fact]
    public void PictureCollection_Add_GrowsCollection()
    {
        var collection = new PictureCollection([]);
        Picture picture = CreatePicture("/some/path.png");

        collection.Add(picture);

        Assert.Equal(1, collection.Count);
        Assert.Same(picture, collection[0]);
    }

    [Fact]
    public void PictureAlbumCollection_Add_GrowsCollection()
    {
        var collection = new PictureAlbumCollection([]);
        var album = new PictureAlbum("Name", null, "/path");

        collection.Add(album);

        Assert.Equal(1, collection.Count);
        Assert.Same(album, collection[0]);
    }
}
