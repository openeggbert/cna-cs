namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>ArtistCollection</c>: a compat-typed view over <c>CNA.Media.ArtistCollection</c>. See
/// <see cref="ReadOnlyMediaCollection{TCompat,TBase}"/> for how the re-typing works.</summary>
public sealed class ArtistCollection : ReadOnlyMediaCollection<Artist, CNA.Media.Artist>
{
    internal ArtistCollection(CNA.Media.ArtistCollection inner)
        : base(inner, item => new Artist(item))
    {
    }
}
