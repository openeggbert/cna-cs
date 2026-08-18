namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>MediaQueue</c>. A pure subclass -- <c>Count</c>/
/// <c>ActiveSongIndex</c>/enumeration are inherited unchanged from
/// <see cref="CNA.Media.MediaQueue"/>; only <see cref="ActiveSong"/> needs re-typing, since
/// <see cref="Song"/> is this namespace's own type.</summary>
public class MediaQueue : CNA.Media.MediaQueue
{
    public new Song? ActiveSong => (Song?)base.ActiveSong;
}
