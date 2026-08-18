namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>PictureAlbumCollection</c>: a compat-typed view over <c>CNA.Media.PictureAlbumCollection</c>. See
/// <see cref="ReadOnlyMediaCollection{TCompat,TBase}"/> for how the re-typing works.</summary>
public sealed class PictureAlbumCollection : ReadOnlyMediaCollection<PictureAlbum, CNA.Media.PictureAlbum>
{
    internal PictureAlbumCollection(CNA.Media.PictureAlbumCollection inner)
        : base(inner, item => new PictureAlbum(item))
    {
    }
}
