namespace CNA.Content.Cnj;

/// <summary>
/// Validates that a <c>.cnj</c> document's own file-supplied sidecar path (<c>"vertices"</c>,
/// <c>"indices"</c>, <c>"texture"</c>) stays inside its authorized content root before it is ever
/// opened -- every such path is untrusted, file-supplied input, exactly like any other
/// externally-controlled path this project already guards against (see
/// <c>CNA.XnaCompat.Media.SavedPictureStore.SanitizePictureName</c>, which solves the analogous
/// "don't let file-supplied input escape a directory" problem for a bare filename rather than a
/// relative path that legitimately contains subdirectories -- a genuinely different shape, so this
/// is new, dedicated infrastructure rather than a reuse of that helper).
///
/// A direct port of the real openeggbert/cna C++ engine's own <c>PathContainment.hpp</c>
/// (<c>ResolveContainedPathFromBase</c>/<c>ValidateContainedPath</c>), always called here with
/// <c>rootDir == baseDir</c> (matching this project's own Model <c>.cnj</c> reader's one and only
/// call shape: every sidecar field resolves relative to <see cref="ContentManager.RootDirectory"/>
/// itself, never relative to the <c>.cnj</c> file's own directory). One honest fidelity gap from the
/// C++ original, documented rather than silently accepted: <see cref="Path.GetFullPath(string)"/> performs
/// only *lexical* normalization (collapses <c>.</c>/<c>..</c> segments), not the C++ side's
/// <c>weakly_canonical</c>, which additionally resolves any *existing* symlink components -- a
/// symlink planted inside a game's own content root that points outside it would not be caught here.
/// This is the same risk tier this project's own <c>xnb-model-spec.md</c>-driven work already
/// accepted for not hardening <c>.xnb</c>'s allocation-size checks against a hostile actor: a local,
/// single-player game's own asset pipeline is not the threat model this guards against so much as
/// "a corrupt or careless content file fails cleanly instead of reading outside its content root."
/// </summary>
internal static class CnjPathContainment
{
    /// <summary>Resolves <paramref name="relativeOrAbsolute"/> against <paramref name="rootDir"/>
    /// and returns <see langword="true"/> with the resulting, lexically-normalized absolute path in
    /// <paramref name="resolvedPath"/> only if it stays contained within <paramref name="rootDir"/>.
    /// Returns <see langword="false"/> (never throws) for an empty input, any absolute-path form
    /// (POSIX-absolute, a Windows drive letter, or a UNC path -- checked explicitly since a
    /// file-supplied string is data, not a path built by the trusted host OS, so a POSIX build must
    /// still refuse a Windows-style absolute path embedded in someone else's <c>.cnj</c> file), or a
    /// path that escapes <paramref name="rootDir"/> by any number of <c>..</c> segments. Callers
    /// decide how to report a <see langword="false"/> result -- <see cref="CnjModelReader"/> turns
    /// it into a <see cref="ContentLoadException"/> naming the offending manifest field.</summary>
    internal static bool TryResolve(string rootDir, string relativeOrAbsolute, out string resolvedPath)
    {
        resolvedPath = "";
        if (string.IsNullOrEmpty(relativeOrAbsolute))
        {
            return false;
        }

        string normalized = relativeOrAbsolute.Replace('\\', '/');
        if (Path.IsPathRooted(normalized) || IsWindowsDriveOrUnc(normalized))
        {
            return false;
        }

        string root = string.IsNullOrEmpty(rootDir) ? "." : rootDir;
        string canonicalRoot = Path.GetFullPath(root);
        string joined = Path.GetFullPath(Path.Combine(canonicalRoot, normalized));

        // Component-wise containment (via GetRelativePath), matching the real engine's own
        // lexically_relative check -- deliberately *not* a bare StartsWith(canonicalRoot), which
        // would wrongly accept a sibling directory like "root-evil" as contained within "root".
        string relative = Path.GetRelativePath(canonicalRoot, joined);
        if (relative == ".")
        {
            return false;
        }

        // Reject only when the *first path component* is exactly ".." -- a code-review finding
        // caught this previously being a bare StartsWith("..") on the whole relative string, which
        // wrongly rejected a legitimate, fully-contained directory that merely starts with two dots
        // (e.g. "..backup/file.bin") as if it were a parent-traversal escape. The real engine's own
        // check (`*rel.begin() == ".."` in PathContainment.hpp) compares the exact first component
        // for the same reason.
        int separatorIndex = relative.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
        string firstComponent = separatorIndex < 0 ? relative : relative[..separatorIndex];
        if (firstComponent == "..")
        {
            return false;
        }

        resolvedPath = joined;
        return true;
    }

    /// <summary>Catches the two absolute-path forms <see cref="Path.IsPathRooted(string?)"/> alone would miss
    /// when this code runs on a non-Windows platform: a Windows drive letter (<c>C:/...</c>) and a
    /// UNC path (<c>//server/share/...</c>). <paramref name="normalized"/> must already have its
    /// backslashes converted to forward slashes.</summary>
    private static bool IsWindowsDriveOrUnc(string normalized)
    {
        if (normalized.Length >= 2 && char.IsAsciiLetter(normalized[0]) && normalized[1] == ':')
        {
            return true;
        }

        return normalized.StartsWith("//", StringComparison.Ordinal);
    }
}
