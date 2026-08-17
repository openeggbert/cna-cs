namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>Album</c>. Extends <c>CNA.Media.Album</c> directly. The
/// constructor takes compat-typed <see cref="Artist"/>/<see cref="Genre"/>/<see cref="SongCollection"/>
/// directly (not <c>CNA.Media</c>-namespaced ones) -- this makes <see cref="Artist"/>/<see cref="Genre"/>'s
/// downcast-pass-through getters provably safe by construction, not just safe in practice because
/// nothing currently calls this constructor with real data (a real code-review finding: the
/// previous version accepted base-typed parameters, which type-checked but undermined the safety
/// argument its own doc comment made).</summary>
public class Album : CNA.Media.Album
{
    internal Album(string name, Artist? artist, Genre? genre, TimeSpan duration, SongCollection songs)
        : base(name, artist, genre, duration, MediaCollectionConversion.ToBase(songs))
    {
        Songs = songs;
    }

    public new Artist? Artist => (Artist?)base.Artist;

    public new Genre? Genre => (Genre?)base.Genre;

    public new SongCollection Songs { get; }
}
