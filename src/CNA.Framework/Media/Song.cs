using CNA.Interop;

namespace CNA.Media;

/// <summary>
/// A song that can be played through <see cref="MediaPlayer"/>. Real XNA's own <c>Song</c> has no
/// public constructor at all (content-pipeline-only); this project has no content pipeline, so
/// this one takes a file path directly, matching the real openeggbert/cna C API's own
/// <c>cna_song_create</c>/<c>cna_song_create_with_duration</c> exactly (the second, explicit
/// -duration constructor already matched real XNA's own 3-argument constructor's shape before this
/// migration reached this file -- see <c>NEXT.md</c>'s native-ABI-migration entry, step 10). A real
/// native object now (<c>CNA_SongHandle</c>), unlike before this migration, when constructing one
/// needed no native call at all.
///
/// <see cref="FileName"/> is kept client-side, alongside the new native handle, specifically for
/// <see cref="MediaPlayer"/>'s own defensive-copy pattern (<c>LoadSong</c> builds an independent
/// native song sharing the same file but tracking its own <see cref="PlayCount"/>, by constructing
/// a brand new <see cref="Song"/> from the same file path) -- an extra native round trip
/// (<c>cna_song_get_handle_text_size_ext</c>/<c>_copy_handle_text_ext</c>) would answer the same
/// text this project already has from its own constructor argument.
///
/// Not sealed here (unlike real XNA's actual <c>sealed class Song</c>) specifically so
/// <c>Microsoft.Xna.Framework.Media.Song</c> can extend this directly, the same "preserve the real
/// logic's lineage over namespace purity" trade-off <c>RenderTarget2D</c>/<c>BasicEffect</c>
/// already made -- the compat type itself is sealed, matching real XNA.
///
/// <see cref="Album"/>/<see cref="Artist"/>/<see cref="Genre"/> stay <c>null</c> forever in this
/// project (their setters are <c>internal</c>, reserved for a real <see cref="MediaLibrary"/> scan
/// this project doesn't implement -- see that type's own doc comment for why): correct for any
/// song that wasn't produced by a library scan, matching real XNA's own documented behavior for a
/// standalone-constructed <c>Song</c>, not an unimplemented stub. <see cref="IsProtected"/>/
/// <see cref="IsRated"/>/<see cref="Rating"/>/<see cref="TrackNumber"/> are real native getters now
/// (<c>cna_song_get_is_protected</c>/etc.) -- confirmed to report the same "nothing scanned this"
/// defaults this project already hardcoded, since CNA itself has no library-scan infrastructure
/// either, but sourced from native now rather than assumed in C#.
/// </summary>
public class Song : IDisposable, IEquatable<Song>
{
    private readonly CnaHandle _handle;

    public Song(string fileName, string name = "")
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(name);

        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"Could not find file '{fileName}'.", fileName);
        }

        FileName = fileName;
        // Matches the real ABI's own documented behavior exactly (verified against cna_song_create's
        // own doc comment, which explicitly calls out that its header's "defaults to the file name"
        // claim does not match what the constructor actually does): name is stored as given, even
        // if empty.
        Name = name;

        CnaHandle handle = CnaHandle.Zero;
        CnaResult result = CnaStringMarshal.WithStringView(
            fileName, fileNameView => CnaStringMarshal.WithStringView(
                name, nameView => Native.cna_song_create(CnaAmbientGame.Current, fileNameView, nameView, out handle)));
        CnaException.ThrowIfFailed(result, nameof(Song));
        _handle = handle;
    }

    public Song(string fileName, string assetName, int durationMS)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(assetName);

        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"Could not find file '{fileName}'.", fileName);
        }

        FileName = fileName;
        Name = assetName;

        CnaHandle handle = CnaHandle.Zero;
        CnaResult result = CnaStringMarshal.WithStringView(
            fileName, fileNameView => CnaStringMarshal.WithStringView(
                assetName, assetNameView => Native.cna_song_create_with_duration(
                    CnaAmbientGame.Current, fileNameView, assetNameView, durationMS, out handle)));
        CnaException.ThrowIfFailed(result, nameof(Song));
        _handle = handle;
    }

    public string Name { get; }

    public Album? Album { get; internal set; }

    public Artist? Artist { get; internal set; }

    public Genre? Genre { get; internal set; }

    public TimeSpan Duration
    {
        get
        {
            CnaResult result = Native.cna_song_get_duration(_handle, out long ticks);
            CnaException.ThrowIfFailed(result, nameof(Duration));
            return TimeSpan.FromTicks(ticks);
        }
        set
        {
            CnaResult result = Native.cna_song_set_duration(_handle, value.Ticks);
            CnaException.ThrowIfFailed(result, nameof(Duration));
        }
    }

    public bool IsProtected
    {
        get
        {
            CnaResult result = Native.cna_song_get_is_protected(_handle, out byte value);
            CnaException.ThrowIfFailed(result, nameof(IsProtected));
            return value != 0;
        }
    }

    public bool IsRated
    {
        get
        {
            CnaResult result = Native.cna_song_get_is_rated(_handle, out byte value);
            CnaException.ThrowIfFailed(result, nameof(IsRated));
            return value != 0;
        }
    }

    public int PlayCount
    {
        get
        {
            CnaResult result = Native.cna_song_get_play_count(_handle, out int value);
            CnaException.ThrowIfFailed(result, nameof(PlayCount));
            return value;
        }
        set
        {
            CnaResult result = Native.cna_song_set_play_count(_handle, value);
            CnaException.ThrowIfFailed(result, nameof(PlayCount));
        }
    }

    public int Rating
    {
        get
        {
            CnaResult result = Native.cna_song_get_rating(_handle, out int value);
            CnaException.ThrowIfFailed(result, nameof(Rating));
            return value;
        }
    }

    public int TrackNumber
    {
        get
        {
            CnaResult result = Native.cna_song_get_track_number(_handle, out int value);
            CnaException.ThrowIfFailed(result, nameof(TrackNumber));
            return value;
        }
    }

    public bool IsDisposed { get; private set; }

    /// <summary>The file path this song plays from -- kept client-side, see this class's own doc
    /// comment. Also this type's own equality/hash basis, matching the real ABI's own documented
    /// behavior for the equivalent native comparison (ordinal, case-sensitive).</summary>
    internal string FileName { get; }

    /// <summary>The real native handle <see cref="MediaPlayer"/> passes to
    /// <c>cna_media_player_play_song</c>.</summary>
    internal CnaHandle NativeHandle => _handle;

    /// <summary>Does not check the native call's own result -- <see cref="Dispose"/> must not
    /// throw, matching the same reasoning already established for <c>Game.Dispose</c>/
    /// <c>BasicEffect.Dispose</c> elsewhere in this migration.</summary>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        Native.cna_song_destroy(_handle);
    }

    /// <summary>
    /// Ordinal, case-sensitive comparison of <see cref="FileName"/> -- matches the real ABI's own
    /// documented equality behavior exactly (a plain string comparison, no case-folding). This
    /// means two paths that differ only in case but name the same file on a case-insensitive
    /// filesystem (Windows, default macOS) compare unequal -- a real, known limitation, but
    /// reproducing it is deliberate: the "correct" case-insensitive comparison is
    /// platform-dependent, and nothing in the real ABI's own documentation specifies one either.
    /// </summary>
    public bool Equals(Song? other) => other is not null && FileName == other.FileName;

    public override bool Equals(object? obj) => Equals(obj as Song);

    public override int GetHashCode() => FileName.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(Song? left, Song? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Song? left, Song? right) => !(left == right);

    /// <summary>
    /// Resolves a file URI (or plain path) to a <see cref="Song"/>. Uses <see cref="Uri"/> for the
    /// actual parsing rather than the real ABI's own <c>cna_song_create_from_uri</c> route (design
    /// invariant #7: use the real BCL for non-CNA-specific concepts, never reinvent one) -- kept
    /// exactly as this project had it before this migration, since this logic never needed native
    /// to be correct in the first place and switching it now would only add a native round trip
    /// this method's own contract doesn't need.
    /// </summary>
    public static Song FromUri(string name, string uri)
    {
        ArgumentNullException.ThrowIfNull(name);

        return new Song(ResolvePathFromUri(uri), name);
    }

    /// <summary>
    /// Shared by this type's own <see cref="FromUri"/> and
    /// <c>Microsoft.Xna.Framework.Media.Song.FromUri</c>'s compat override -- extracted
    /// specifically so the two can't silently drift apart the way a future fix to this logic
    /// applied to only one of them would otherwise risk.
    /// </summary>
    internal static string ResolvePathFromUri(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out Uri? parsed) || !parsed.IsAbsoluteUri)
        {
            return uri;
        }

        if (parsed.Scheme != Uri.UriSchemeFile)
        {
            throw new InvalidOperationException("Only local file URIs are supported for now.");
        }

        return parsed.LocalPath;
    }
}
