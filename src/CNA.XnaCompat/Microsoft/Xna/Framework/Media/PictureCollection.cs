namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>PictureCollection</c>. Independent reimplementation, not a
/// subclass of <c>CNA.Media.PictureCollection</c> -- same reasoning as <see cref="SongCollection"/>'s
/// own doc comment (extending directly would inherit an indexer/enumerator typed to
/// <c>CNA.Media.Picture</c>, not this namespace's <see cref="Picture"/>).</summary>
public sealed class PictureCollection : ReadOnlyMediaCollection<Picture>
{
    internal PictureCollection(IReadOnlyList<Picture> pictures)
        : base(pictures)
    {
    }
}
