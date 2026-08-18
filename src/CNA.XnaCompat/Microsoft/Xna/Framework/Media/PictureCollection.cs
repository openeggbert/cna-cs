namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>PictureCollection</c>: a compat-typed view over <c>CNA.Media.PictureCollection</c>. See
/// <see cref="ReadOnlyMediaCollection{TCompat,TBase}"/> for how the re-typing works.</summary>
public sealed class PictureCollection : ReadOnlyMediaCollection<Picture, CNA.Media.Picture>
{
    internal PictureCollection(CNA.Media.PictureCollection inner)
        : base(inner, item => new Picture(item))
    {
    }
}
