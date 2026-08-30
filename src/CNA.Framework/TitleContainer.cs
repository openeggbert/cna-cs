namespace CNA;

/// <summary>
/// Matches real XNA's <c>TitleContainer</c>: opens a file shipped alongside the game, relative to
/// the title's own location.
///
/// Pure BCL, deliberately -- design invariant #7. There is a native counterpart in the C++ engine,
/// but what it does is path normalization plus a file open, and reimplementing
/// <see cref="File.OpenRead(string)"/> across the ABI would add a round trip and a second set of
/// path-handling bugs to answer a question <see cref="System.IO"/> already answers.
///
/// Was missing until the WP16 re-audit.
/// </summary>
public static class TitleContainer
{
    /// <summary>
    /// Opens a title file for reading.
    ///
    /// <paramref name="name"/> is relative to the application's base directory. An absolute path is
    /// rejected rather than honoured: real XNA's own contract is title-relative, and silently
    /// accepting an absolute path would let content code reach outside the title's own files.
    /// Separators are normalized so a manifest written with the other platform's slash still opens.
    /// </summary>
    public static Stream OpenStream(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        string normalized = name.Replace('\\', Path.DirectorySeparatorChar)
                                .Replace('/', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalized))
        {
            throw new ArgumentException(
                "TitleContainer.OpenStream takes a path relative to the title, not an absolute one.", nameof(name));
        }

        string full = Path.Combine(AppContext.BaseDirectory, normalized);

        try
        {
            return File.OpenRead(full);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // Matches real XNA, which reports a missing title file as FileNotFoundException naming
            // the path asked for rather than the resolved one -- the caller wrote the former.
            throw new FileNotFoundException($"Could not find title file '{name}'.", name, ex);
        }
    }

    /// <summary>
    /// XNA's own <c>TitleContainer.GetCleanPath</c>, transcribed from the decompiled 4.0 method
    /// rather than approximated.
    ///
    /// This is the function that decides what a content *asset identity* is, so it has to be exact
    /// and it has to be host-independent. <see cref="Path.GetFullPath(string)"/> is the wrong tool
    /// twice over: it would splice in the process working directory, and it would answer with the
    /// host's separator, turning one asset name into two different names on two operating systems.
    ///
    /// Two behaviours here look like bugs and are not, so they are transcribed rather than
    /// improved. A repeated separator is *kept* (<c>a\b</c> stays <c>a\b</c>), because XNA
    /// rewrites <c>\.\</c>, leading <c>.\</c>, trailing <c>\.</c> and <c>\..\</c> and nothing else.
    /// And a leading <c>..\</c> is *kept*, because the collapse loop starts at index 1 and so never
    /// matches a <c>..</c> in the first segment -- XNA's own <c>IsCleanPathAbsolute</c> then treats
    /// exactly that residue as "this escaped the title", which is only coherent if the residue
    /// survives.
    ///
    /// Every comparison is ordinal. .NET Framework's parameterless <c>StartsWith</c>/
    /// <c>EndsWith</c>/<c>IndexOf(string, int)</c> are culture-sensitive, which for these ASCII
    /// patterns differs only where a collation-ignorable character sits next to a separator; an
    /// asset identity must not depend on the current culture.
    /// </summary>
    internal static string GetCleanPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        path = path.Replace('/', '\\');
        path = path.Replace("\\.\\", "\\", StringComparison.Ordinal);

        while (path.StartsWith(".\\", StringComparison.Ordinal))
        {
            path = path[2..];
        }

        while (path.EndsWith("\\.", StringComparison.Ordinal))
        {
            path = path.Length <= 2 ? "\\" : path[..^2];
        }

        for (int index = 1; index < path.Length; index = CollapseParentDirectory(ref path, index, "\\..\\".Length))
        {
            index = path.IndexOf("\\..\\", index, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }
        }

        if (path.EndsWith("\\..", StringComparison.Ordinal))
        {
            int trailing = path.Length - "\\..".Length;
            if (trailing > 0)
            {
                CollapseParentDirectory(ref path, trailing, "\\..".Length);
            }
        }

        if (path == ".")
        {
            path = string.Empty;
        }

        return path;
    }

    /// <summary>
    /// XNA's own <c>TitleContainer.IsPathAbsolute</c>: whether a path leaves the title's own tree.
    ///
    /// It is not <see cref="Path.IsPathRooted(string)"/> and the difference matters. XNA calls a
    /// path absolute if it holds any of <c>: * ? " &lt; &gt; |</c>, begins with a separator, or
    /// escapes upward with <c>..</c> -- so <c>..\elsewhere</c> is "absolute" here, meaning "outside
    /// the title", which is the question actually being asked. It is also host-independent, because
    /// <see cref="GetCleanPath"/> has already turned every separator into a backslash: a POSIX
    /// <c>/rv/tmp</c> becomes <c>\rv\tmp</c> and is recognised.
    /// </summary>
    internal static bool IsPathAbsolute(string path) => IsCleanPathAbsolute(GetCleanPath(path));

    private static bool IsCleanPathAbsolute(string path) =>
        path.IndexOfAny([':', '*', '?', '"', '<', '>', '|']) >= 0 ||
        path.StartsWith('\\') ||
        path.StartsWith("..\\", StringComparison.Ordinal) ||
        path.Contains("\\..\\", StringComparison.Ordinal) ||
        path.EndsWith("\\..", StringComparison.Ordinal) ||
        path == "..";

    /// <summary>XNA's own <c>CollapseParentDirectory</c>: removes the segment before
    /// <paramref name="position"/> along with the <c>..</c> that cancelled it, and answers where
    /// the caller's scan resumes.</summary>
    private static int CollapseParentDirectory(ref string path, int position, int removeLength)
    {
        int start = path.LastIndexOf('\\', position - 1) + 1;
        path = path.Remove(start, position - start + removeLength);
        return Math.Max(start - 1, 1);
    }
}
