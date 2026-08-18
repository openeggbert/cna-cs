namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>AlbumCollection</c>: a compat-typed view over <c>CNA.Media.AlbumCollection</c>. See
/// <see cref="ReadOnlyMediaCollection{TCompat,TBase}"/> for how the re-typing works.</summary>
public sealed class AlbumCollection : ReadOnlyMediaCollection<Album, CNA.Media.Album>
{
    internal AlbumCollection(CNA.Media.AlbumCollection inner)
        : base(inner, item => new Album(item))
    {
    }
}
