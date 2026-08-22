namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// XNA 4.0-compatible <c>MediaQueue</c>: a compat-typed view over <c>CNA.Media.MediaQueue</c>.
///
/// A wrapper now, rather than the subclass it used to be -- compat <see cref="Song"/> moved to
/// composition with the media-library rebinding, so its elements have to be re-typed rather than
/// downcast. That also closes the gap the compat <see cref="MediaPlayer"/> used to document as a
/// structural blocker: <c>CNA.Media.MediaPlayer.LoadSong</c> always builds a base-typed defensive
/// copy, so a downcast queue could only ever have failed. Wrapping does not care what the base
/// queue holds.
/// </summary>
public sealed class MediaQueue
{
    private readonly CNA.Media.MediaQueue _inner;

    internal MediaQueue(CNA.Media.MediaQueue inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public int Count => _inner.Count;

    public int ActiveSongIndex
    {
        get => _inner.ActiveSongIndex;
        set => _inner.ActiveSongIndex = value;
    }

    public Song? ActiveSong => _inner.ActiveSong is { } song ? new Song(song) : null;

    public Song this[int index] => new(_inner[index]);

}
