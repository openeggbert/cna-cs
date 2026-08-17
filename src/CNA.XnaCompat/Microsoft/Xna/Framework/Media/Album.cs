namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Album</c>. Extends <c>CNA.Media.Album</c> directly.
/// <see cref="Artist"/>/<see cref="Genre"/>'s downcast-pass-through getters are safe for the same
/// reason as <c>MediaLibrary.MediaSource</c>'s own (see that type's doc comment): nothing in this
/// project ever constructs a real <c>Album</c> at all (no <c>MediaLibrary</c> scan exists), so
/// there is no reachable code path where <c>base.Artist</c>/<c>base.Genre</c> could actually hold
/// a non-compat-typed instance.</summary>
public class Album : CNA.Media.Album
{
    internal Album(
        string name,
        CNA.Media.Artist? artist,
        CNA.Media.Genre? genre,
        TimeSpan duration,
        CNA.Media.SongCollection songs)
        : base(name, artist, genre, duration, songs)
    {
    }

    public new Artist? Artist => (Artist?)base.Artist;

    public new Genre? Genre => (Genre?)base.Genre;

    public new SongCollection Songs { get; } = new([]);
}
