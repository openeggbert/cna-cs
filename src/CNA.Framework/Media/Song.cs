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
/// <see cref="Name"/> and <see cref="FileName"/> are read from native rather than kept from the
/// constructor arguments, since the media-library rebinding. A song reached through
/// <see cref="MediaLibrary.Songs"/> or an <see cref="Album"/> was never constructed here, so there
/// are no arguments to have kept -- and the ABI answers both for every song, however it was made.
///
/// Not sealed here (unlike real XNA's actual <c>sealed class Song</c>) specifically so
/// <c>Microsoft.Xna.Framework.Media.Song</c> can extend this directly, the same "preserve the real
/// logic's lineage over namespace purity" trade-off <c>RenderTarget2D</c>/<c>BasicEffect</c>
/// already made -- the compat type itself is sealed, matching real XNA.
///
/// Every native call below pairs its handle read with <see cref="GC.KeepAlive(object)"/>. That
/// became load-bearing when this type's handle moved into a
/// <see cref="NativeResourceHandle"/>: before that it was a bare field with no finalizer, so an
/// unreachable <see cref="Song"/> could not release itself mid-call. Now it can -- see
/// <c>plan.md</c> WP17.
///
/// <see cref="Album"/>/<see cref="Artist"/>/<see cref="Genre"/> are real native reads over
/// <c>cna_song_get_album</c>/<c>_artist</c>/<c>_genre</c>. They answer <see langword="null"/> for a
/// song constructed directly from a file -- correct, and matching real XNA for a
/// standalone-constructed <c>Song</c> -- and a real object for one that came from a library scan,
/// which is what changed: the previous "<c>null</c> forever, since nothing scans" note rested on
/// this project's <see cref="MediaLibrary"/> being an always-empty object model, which it no longer
/// is. <see cref="IsProtected"/>/
/// <see cref="IsRated"/>/<see cref="Rating"/>/<see cref="TrackNumber"/> are real native getters now
/// (<c>cna_song_get_is_protected</c>/etc.) -- confirmed to report the same "nothing scanned this"
/// defaults this project already hardcoded, since CNA itself has no library-scan infrastructure
/// either, but sourced from native now rather than assumed in C#.
/// </summary>
public class Song : IDisposable, IEquatable<Song>
{
    private readonly NativeResourceHandle _handle;

    /// <summary>Wraps a song handle taken out of a library-owned collection. The song belongs to
    /// the library; this handle is the caller's to release, which
    /// <see cref="Dispose"/> does.</summary>
    internal Song(CnaHandle handle)
    {
        _handle = new NativeResourceHandle(handle.AsNint, h => Native.cna_song_destroy(new CnaHandle(h)).IsSuccess());
    }

    public Song(string fileName, string name = "")
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(name);

        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"Could not find file '{fileName}'.", fileName);
        }

        CnaHandle handle = CnaHandle.Zero;
        CnaResult result = CnaStringMarshal.WithStringView(
            fileName, fileNameView => CnaStringMarshal.WithStringView(
                name, nameView => Native.cna_song_create(CnaAmbientGame.Current, fileNameView, nameView, out handle)));
        CnaException.ThrowIfFailed(result, nameof(Song));
        _handle = new NativeResourceHandle(handle.AsNint, h => Native.cna_song_destroy(new CnaHandle(h)).IsSuccess());
    }

    public Song(string fileName, string assetName, int durationMS)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(assetName);

        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"Could not find file '{fileName}'.", fileName);
        }

        CnaHandle handle = CnaHandle.Zero;
        CnaResult result = CnaStringMarshal.WithStringView(
            fileName, fileNameView => CnaStringMarshal.WithStringView(
                assetName, assetNameView => Native.cna_song_create_with_duration(
                    CnaAmbientGame.Current, fileNameView, assetNameView, durationMS, out handle)));
        CnaException.ThrowIfFailed(result, nameof(Song));
        _handle = new NativeResourceHandle(handle.AsNint, h => Native.cna_song_destroy(new CnaHandle(h)).IsSuccess());
    }

    /// <summary>The song's display name -- stored as given, even when empty. (Verified against
    /// <c>cna_song_create</c>'s own doc comment, which calls out that its header's "defaults to the
    /// file name" claim does not match what the constructor actually does.)</summary>
    public unsafe string Name => ReadString(Native.cna_song_get_name_size, Native.cna_song_copy_name, nameof(Name));

    public Album? Album => ReadOptional(Native.cna_song_get_album, h => new Album(h), nameof(Album));

    public Artist? Artist => ReadOptional(Native.cna_song_get_artist, h => new Artist(h), nameof(Artist));

    public Genre? Genre => ReadOptional(Native.cna_song_get_genre, h => new Genre(h), nameof(Genre));

    public TimeSpan Duration
    {
        get
        {
            CnaResult result = Native.cna_song_get_duration(NativeHandle, out long ticks);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Duration));
            return TimeSpan.FromTicks(ticks);
        }
        set
        {
            CnaResult result = Native.cna_song_set_duration(NativeHandle, value.Ticks);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Duration));
        }
    }

    public bool IsProtected
    {
        get
        {
            CnaResult result = Native.cna_song_get_is_protected(NativeHandle, out byte value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(IsProtected));
            return value != 0;
        }
    }

    public bool IsRated
    {
        get
        {
            CnaResult result = Native.cna_song_get_is_rated(NativeHandle, out byte value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(IsRated));
            return value != 0;
        }
    }

    public int PlayCount
    {
        get
        {
            CnaResult result = Native.cna_song_get_play_count(NativeHandle, out int value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(PlayCount));
            return value;
        }
        set
        {
            CnaResult result = Native.cna_song_set_play_count(NativeHandle, value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(PlayCount));
        }
    }

    public int Rating
    {
        get
        {
            CnaResult result = Native.cna_song_get_rating(NativeHandle, out int value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Rating));
            return value;
        }
    }

    public int TrackNumber
    {
        get
        {
            CnaResult result = Native.cna_song_get_track_number(NativeHandle, out int value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(TrackNumber));
            return value;
        }
    }

    /// <summary>Reports the canonical disposed flag, read from native rather than tracked here, so
    /// it stays true when something else disposed the song. Answers <see langword="true"/> once the
    /// handle has been released too, since past that point the question cannot be asked.</summary>
    public bool IsDisposed
    {
        get
        {
            if (_handle.IsClosed || _handle.IsInvalid)
            {
                return true;
            }

            CnaResult result = Native.cna_song_get_is_disposed(NativeHandle, out byte disposed);

            GC.KeepAlive(this);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(IsDisposed));
            return disposed != 0;
        }
    }

    /// <summary>The file path this song plays from -- "the string song equality and hashing are
    /// computed from, not the display name" (<c>media.h:319</c>). Read from native rather than kept
    /// from the constructor, so a library-sourced song answers it too.</summary>
    internal unsafe string FileName => ReadString(
        Native.cna_song_get_handle_text_size_ext, Native.cna_song_copy_handle_text_ext, nameof(FileName));

    /// <summary>The real native handle <see cref="MediaPlayer"/> passes to
    /// <c>cna_media_player_play_song</c>. Read out of the owning
    /// <see cref="NativeResourceHandle"/>, so every use pairs it with
    /// <see cref="GC.KeepAlive(object)"/> -- see <c>plan.md</c> WP17.</summary>
    internal CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    /// <summary>Canonical disposal first (a flag), then the handle release -- the same two-step
    /// <see cref="MediaLibraryObject"/> documents, and for the same reason. Neither result is
    /// checked: <see cref="Dispose"/> must not throw, matching <c>Game.Dispose</c>'s established
    /// reasoning elsewhere in this migration.</summary>
    public void Dispose()
    {
        if (_handle.IsClosed || _handle.IsInvalid)
        {
            return;
        }

        Native.cna_song_dispose(NativeHandle);
        GC.KeepAlive(this);
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases this wrapper's handle without the canonical disposal
    /// <see cref="Dispose"/> performs first -- see
    /// <see cref="MediaLibraryObject.ReleaseHandleOnly"/> for why a collection needs exactly
    /// that. A song is doubly exposed to the difference: <c>cna_song_collection_get_at</c>
    /// documents its result as "the same song the collection holds, not a copy", so a canonical
    /// dispose through one handle is observed through every other.</summary>
    internal void ReleaseHandleOnly()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }

    private unsafe string ReadString(NativeStringReader.SizeFunc size, NativeStringReader.CopyFunc copy, string context)
    {
        string value = NativeStringReader.Read(size, copy, NativeHandle, context);
        GC.KeepAlive(this);
        return value;
    }

    private TResult? ReadOptional<TResult>(OptionalFunc getter, Func<CnaHandle, TResult> wrap, string context)
        where TResult : class
    {
        CnaResult result = getter(NativeHandle, out CnaHandle value, out byte available);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, context);
        return available != 0 ? wrap(value) : null;
    }

    private delegate CnaResult OptionalFunc(CnaHandle handle, out CnaHandle outValue, out byte outAvailable);

    /// <summary>
    /// Delegated to <c>cna_song_equals</c> rather than reimplemented from <see cref="FileName"/>.
    /// The canonical rule is a file-path comparison, so two independently created songs over the
    /// same file compare equal -- including one from a library scan and one constructed here. It is
    /// ordinal and case-sensitive, which means two paths differing only in case on a
    /// case-insensitive filesystem compare unequal; that is a real limitation of the canonical
    /// rule, and reproducing it beats inventing a second, divergent definition managed-side.
    /// </summary>
    public bool Equals(Song? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        CnaResult result = Native.cna_song_equals(NativeHandle, other.NativeHandle, out byte equal);
        GC.KeepAlive(this);
        GC.KeepAlive(other);
        CnaException.ThrowIfFailed(result, nameof(Equals));
        return equal != 0;
    }

    public override bool Equals(object? obj) => Equals(obj as Song);

    /// <summary>Native's own hash of the file path, so it stays consistent with
    /// <see cref="Equals(Song)"/> by construction rather than by two definitions agreeing.</summary>
    public override int GetHashCode()
    {
        CnaResult result = Native.cna_song_get_hash_code(NativeHandle, out int hash);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetHashCode));
        return hash;
    }

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
