namespace CNA.Media;

/// <summary>
/// A song that can be played through <see cref="MediaPlayer"/>. Real XNA's own <c>Song</c> has no
/// public constructor at all (content-pipeline-only); this project has no content pipeline, so,
/// matching the real openeggbert/cna C++ engine's own <c>CNAEXT</c>-marked constructors exactly,
/// this one takes a file path directly. Construction is pure managed logic -- checks the file
/// exists via <see cref="File.Exists"/> and throws <see cref="FileNotFoundException"/> if not,
/// exactly like the real C++ constructor's own <c>std::filesystem::exists</c> check -- no native
/// call happens until <see cref="MediaPlayer.Play"/> actually streams it.
///
/// Not sealed here (unlike real XNA's actual <c>sealed class Song</c>) specifically so
/// <c>Microsoft.Xna.Framework.Media.Song</c> can extend this directly, the same "preserve the real
/// logic's lineage over namespace purity" trade-off <c>RenderTarget2D</c>/<c>BasicEffect</c>
/// already made -- the compat type itself is sealed, matching real XNA.
///
/// Deliberately omits <c>Album</c>/<c>Artist</c>/<c>Genre</c> and the whole <c>MediaLibrary</c>
/// scanning subsystem the real C++ engine also implements: those need a real media-library scan
/// (tag parsing, on-disk indexing) this project has no equivalent for and that real XNA games
/// overwhelmingly don't touch for simple background-music playback, the realistic first target
/// here (matching this project's own "simple games first" philosophy -- see
/// docs/xna-compatibility.md). <see cref="IsProtected"/>/<see cref="IsRated"/>/<see cref="Rating"/>/
/// <see cref="TrackNumber"/> are kept (real XNA public API a ported game might reference) but
/// always report their real "nothing scanned this" defaults, the same "this is the actually
/// correct answer, not an unimplemented stub" reasoning the real C++ engine's own
/// <c>IsProtected</c> doc comment already uses.
/// </summary>
public class Song : IDisposable, IEquatable<Song>
{
    public Song(string fileName, string name = "")
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(name);

        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"Could not find file '{fileName}'.", fileName);
        }

        Handle = fileName;
        // Matches the real C++ constructor's actual behavior exactly (verified against its .cpp,
        // not its .hpp doc comment): name is stored as given, even if empty -- the header's own
        // "defaults to the file name" doc comment does not match what the constructor body
        // actually does, so the real *behavior* was reproduced here, not the (inaccurate) doc.
        Name = name;
        Duration = TimeSpan.Zero;
    }

    public Song(string fileName, string assetName, int durationMS)
        : this(fileName, assetName)
    {
        Duration = TimeSpan.FromMilliseconds(durationMS);
    }

    public string Name { get; }

    public TimeSpan Duration { get; set; }

    public bool IsProtected => false;

    public bool IsRated => false;

    public int PlayCount { get; set; }

    public int Rating => 0;

    public int TrackNumber => 0;

    public bool IsDisposed { get; private set; }

    internal string Handle { get; }

    public void Dispose() => IsDisposed = true;

    /// <summary>
    /// Ordinal, case-sensitive comparison of <see cref="Handle"/> -- matches the real C++ engine's
    /// own <c>Song::Equals</c> exactly (a plain <c>std::string ==</c>, no case-folding there
    /// either), not a gap introduced here. This means two paths that differ only in case but name
    /// the same file on a case-insensitive filesystem (Windows, default macOS) compare unequal --
    /// a real, known limitation, but reproducing it is deliberate: the "correct" case-insensitive
    /// comparison is platform-dependent, and nothing in the analysis docs or the real engine's own
    /// implementation specifies one, so guessing at one here would be inventing behavior neither
    /// this project's own conventions nor its source of truth actually call for.
    /// </summary>
    public bool Equals(Song? other) => other is not null && Handle == other.Handle;

    public override bool Equals(object? obj) => Equals(obj as Song);

    public override int GetHashCode() => Handle.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(Song? left, Song? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Song? left, Song? right) => !(left == right);

    /// <summary>
    /// Resolves a file URI (or plain path) to a <see cref="Song"/>. Uses <see cref="Uri"/> for the
    /// actual parsing rather than porting the real C++ engine's own hand-rolled percent-decoding/
    /// scheme/UNC-path logic -- the .NET BCL already solves exactly this problem correctly (design
    /// invariant #7: use the real BCL for non-CNA-specific concepts, never reinvent one), so
    /// reproducing the C++ engine's manual parser here would just be duplicating what
    /// <see cref="Uri"/> already does, with more room for bugs.
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
