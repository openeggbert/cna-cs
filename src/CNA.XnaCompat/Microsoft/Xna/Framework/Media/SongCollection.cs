namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// XNA 4.0-compatible <c>SongCollection</c>. An independent implementation, not a subclass of
/// <c>CNA.Media.SongCollection</c> -- extending it directly would inherit an indexer/enumerator
/// typed to <c>CNA.Media.Song</c>, not this namespace's own <see cref="Song"/>, defeating the
/// point of a compat-typed collection. Same shape as <c>VertexDeclaration</c>'s own
/// wrap-don't-subclass choice, just duplicated rather than composed since there's no underlying
/// native/framework instance to wrap here.
/// </summary>
public sealed class SongCollection : ReadOnlyMediaCollection<Song>
{
    public SongCollection(IReadOnlyList<Song> songs)
        : base(songs)
    {
    }
}
