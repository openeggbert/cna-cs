namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>GenreCollection</c>: a compat-typed view over <c>CNA.Media.GenreCollection</c>. See
/// <see cref="ReadOnlyMediaCollection{TCompat,TBase}"/> for how the re-typing works.</summary>
public sealed class GenreCollection : ReadOnlyMediaCollection<Genre, CNA.Media.Genre>
{
    internal GenreCollection(CNA.Media.GenreCollection inner)
        : base(inner, item => new Genre(item))
    {
    }
}
