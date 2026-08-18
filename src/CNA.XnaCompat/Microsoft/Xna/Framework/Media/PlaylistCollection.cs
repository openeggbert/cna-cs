namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>PlaylistCollection</c>: a compat-typed view over <c>CNA.Media.PlaylistCollection</c>. See
/// <see cref="ReadOnlyMediaCollection{TCompat,TBase}"/> for how the re-typing works.</summary>
public sealed class PlaylistCollection : ReadOnlyMediaCollection<Playlist, CNA.Media.Playlist>
{
    internal PlaylistCollection(CNA.Media.PlaylistCollection inner)
        : base(inner, item => new Playlist(item))
    {
    }
}
