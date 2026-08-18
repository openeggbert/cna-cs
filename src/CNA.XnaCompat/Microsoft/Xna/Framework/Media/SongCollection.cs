namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// XNA 4.0-compatible <c>SongCollection</c>: a compat-typed view over
/// <c>CNA.Media.SongCollection</c>.
///
/// The public list-taking constructor exists because real XNA's own is content-pipeline-only and
/// this project has no content pipeline -- the same reasoning <c>CNA.Media.SongCollection</c>
/// records. It builds a real native collection, so one constructed here and one read out of a
/// <see cref="MediaLibrary"/> behave identically.
/// </summary>
public sealed class SongCollection : ReadOnlyMediaCollection<Song, CNA.Media.Song>
{
    public SongCollection(IReadOnlyList<Song> songs)
        : this(ToBase(songs))
    {
    }

    internal SongCollection(CNA.Media.SongCollection inner)
        : base(inner, song => new Song(song))
    {
    }

    /// <summary>Unwraps each compat song for the native collection. Not covariance: a compat
    /// <see cref="Song"/> is no longer a <c>CNA.Media.Song</c> -- see that type's own doc comment
    /// for why the whole family moved to composition.</summary>
    private static CNA.Media.SongCollection ToBase(IReadOnlyList<Song> songs)
    {
        ArgumentNullException.ThrowIfNull(songs);

        var inner = new CNA.Media.Song[songs.Count];
        for (int i = 0; i < inner.Length; i++)
        {
            inner[i] = songs[i].Inner;
        }

        return new CNA.Media.SongCollection(inner);
    }

    /// <summary>The underlying native collection, re-typed for the <c>CNA.Media</c> routes
    /// <see cref="MediaPlayer"/> forwards to.</summary>
    internal new CNA.Media.SongCollection Inner => (CNA.Media.SongCollection)base.Inner;
}
