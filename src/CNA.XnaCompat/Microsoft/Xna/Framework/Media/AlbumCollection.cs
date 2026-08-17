namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>AlbumCollection</c>. Independent implementation, same
/// reasoning as <see cref="SongCollection"/>'s own doc comment. <c>internal</c> constructor,
/// matching real XNA's own <c>MediaLibrary</c>-only construction (unlike <see cref="SongCollection"/>,
/// which needs a public one for <c>MediaPlayer.Play</c>).</summary>
public sealed class AlbumCollection : ReadOnlyMediaCollection<Album>
{
    internal AlbumCollection(IReadOnlyList<Album> albums)
        : base(albums)
    {
    }
}
