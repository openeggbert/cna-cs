namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// Shared "convert a compat-typed collection into the equivalent base-typed one for the
/// <c>base(...)</c> call site" helper for <see cref="Artist"/>/<see cref="Genre"/>/<see cref="Album"/>/
/// <see cref="Playlist"/>'s constructors. Extracted after a code review pointed out the irony of
/// this exact ~1-line-times-2 conversion being duplicated across all four files in the same commit
/// that extracted <see cref="ReadOnlyMediaCollection{T}"/> to eliminate an equivalent duplication
/// elsewhere.
/// </summary>
internal static class MediaCollectionConversion
{
    internal static CNA.Media.AlbumCollection ToBase(AlbumCollection albums) => new(albums.ToList());

    internal static CNA.Media.SongCollection ToBase(SongCollection songs) => new(songs.ToList());
}
