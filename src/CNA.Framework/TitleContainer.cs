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
}
