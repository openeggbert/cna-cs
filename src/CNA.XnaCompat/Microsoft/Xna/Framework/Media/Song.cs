namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// XNA 4.0-compatible <c>Song</c>. Extends <c>CNA.Media.Song</c> directly rather than wrapping it
/// -- unlike most native-backed compat types, <c>Song</c> has no native handle to worry about
/// double-wrapping (construction is pure managed logic, see <c>CNA.Media.Song</c>'s own doc
/// comment), so there's no reason not to inherit unchanged. Sealed here, matching real XNA's own
/// <c>sealed class Song</c> exactly -- <c>CNA.Media.Song</c> itself is deliberately left unsealed
/// so this class can extend it.
/// </summary>
public sealed class Song : CNA.Media.Song
{
    public Song(string fileName, string name = "")
        : base(fileName, name)
    {
    }

    public Song(string fileName, string assetName, int durationMS)
        : base(fileName, assetName, durationMS)
    {
    }

    /// <summary>
    /// Resolves <paramref name="uri"/> the same way <c>CNA.Media.Song.FromUri</c> does, but
    /// constructs this namespace's own <see cref="Song"/> directly instead of delegating and then
    /// re-wrapping -- delegating would construct (and file-existence-check) a throwaway
    /// <c>CNA.Media.Song</c> only to immediately discard it for this one, checking the same file
    /// twice for no benefit.
    /// </summary>
    public static new Song FromUri(string name, string uri)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(uri);

        if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out Uri? parsed) || !parsed.IsAbsoluteUri)
        {
            return new Song(uri, name);
        }

        if (parsed.Scheme != Uri.UriSchemeFile)
        {
            throw new InvalidOperationException("Only local file URIs are supported for now.");
        }

        return new Song(parsed.LocalPath, name);
    }
}
