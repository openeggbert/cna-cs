namespace CNA.Content;

/// <summary>
/// The two path rules that decide what an XNA external reference names, transcribed from XNA 4.0's
/// own <c>ContentReader.GetPathToReference</c> and <c>TitleContainer.GetCleanPath</c>.
///
/// <b>Why this is not <see cref="Path"/>.</b> An external reference resolves to a *content asset
/// name*, not to a file-system path: it is the string handed back to
/// <c>ContentManager.Load&lt;T&gt;</c>, which is also the manager's cache key. Running it through
/// <see cref="Path.Combine(string, string)"/> makes the answer depend on the host, in two ways that
/// both change identity rather than only spelling:
///
/// <list type="bullet">
/// <item>the separator inserted between the two halves is the host's, so the same asset is
/// <c>Models/x</c> on Linux and <c>Models\x</c> on Windows;</item>
/// <item><see cref="Path.IsPathRooted(string)"/> answers differently -- a reference beginning
/// <c>\</c> is rooted on Windows (so XNA drops the directory and keeps the reference alone) and
/// not rooted on Linux (so the directory is kept). One reference, two different assets.</item>
/// </list>
///
/// XNA ran on Windows, so Windows' rules are the specification here and are spelled out rather
/// than delegated. <see cref="Resolve"/> then hands the result to
/// <see cref="TitleContainer.GetCleanPath"/>, which is where <c>.</c> and <c>..</c> collapse.
///
/// Shared by <c>CNA.Content.Xnb</c>'s reader and <c>CNA.XnaCompat</c>'s <c>ContentReader</c>
/// deliberately: two normalisations of one rule is how the two content paths would come to disagree
/// about which asset a model's effect reference names, and the disagreement would show up as a
/// missing file rather than as a difference.
/// </summary>
internal static class XnaContentPath
{
    /// <summary>
    /// Resolves the external reference <paramref name="reference"/> read while loading
    /// <paramref name="assetName"/> into the asset name it denotes.
    ///
    /// An empty or absent reference answers <see langword="null"/>: XNA reads the reference string
    /// first and returns <c>default(T)</c> for an empty one without consulting the content manager
    /// at all, so "no reference" and "a reference to nothing" are the same thing and neither is an
    /// error.
    /// </summary>
    internal static string? Resolve(string assetName, string? reference)
    {
        ArgumentNullException.ThrowIfNull(assetName);

        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        return TitleContainer.GetCleanPath(GetPathToReference(assetName, reference));
    }

    /// <summary>XNA's own <c>ContentReader.GetPathToReference</c>: the reference is relative to the
    /// directory of the asset that names it, where "directory" is everything before the last
    /// separator of the *asset name* -- not of any file-system path it resolved to.</summary>
    internal static string GetPathToReference(string assetName, string reference)
    {
        int separator = assetName.LastIndexOfAny(['\\', '/', Path.DirectorySeparatorChar]);
        string directory = separator < 0 ? string.Empty : assetName[..separator];
        return WindowsCombine(directory, reference);
    }

    /// <summary>
    /// .NET Framework's <c>Path.Combine</c> under Windows rules, which is what XNA's own
    /// <c>GetPathToReference</c> called. Written out because the same call on this host answers
    /// differently -- see this type's own doc comment.
    /// </summary>
    private static string WindowsCombine(string first, string second)
    {
        if (second.Length == 0)
        {
            return first;
        }

        if (first.Length == 0 || IsWindowsRooted(second))
        {
            return second;
        }

        char last = first[^1];
        return last is '\\' or '/' or ':' ? first + second : first + "\\" + second;
    }

    /// <summary>Windows' own <c>Path.IsPathRooted</c>: a leading separator, or a drive letter's
    /// colon in second position.</summary>
    private static bool IsWindowsRooted(string path) =>
        (path.Length >= 1 && path[0] is '\\' or '/') ||
        (path.Length >= 2 && path[1] == ':');

    /// <summary>
    /// The file this asset name and extension denote under <paramref name="rootDirectory"/>.
    ///
    /// This is the one boundary where a content asset name stops being an identity and becomes a
    /// path, and the separator changes here and nowhere else. An XNA asset name is spelled with
    /// backslashes -- <c>Textures\rock_diff</c> -- and on this host a backslash is an ordinary
    /// filename character, so combining without translating asks for a single file literally named
    /// <c>Textures\rock_diff.xnb</c>. Nothing loaded through it, and the error named a path that
    /// looked almost right, which is the worst kind.
    ///
    /// It stayed hidden while nothing produced such a name: a game normally writes its asset names
    /// with whatever separator it likes and this host accepts <c>/</c>. External references are what
    /// made it reachable, because they are *generated* -- <see cref="Resolve"/> answers in XNA's
    /// spelling by construction, which is correct for the identity and unusable as a path.
    ///
    /// The name is not rewritten, only the lookup: <c>ContentManager</c> still caches under the
    /// name XNA would use, and native still receives it as written (CNA's own loader normalises
    /// separators on both the root and the asset name, so it is indifferent).
    /// </summary>
    internal static string ToFilePath(string rootDirectory, string assetName, string extension)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);
        ArgumentNullException.ThrowIfNull(assetName);

        string relative = assetName
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        return Path.Combine(rootDirectory, relative + extension);
    }
}
