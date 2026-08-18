namespace CNA.Media;

/// <summary>
/// A real "Saved Pictures" subfolder under the pictures root -- reproduced from the real
/// openeggbert/cna C++ engine's own
/// <c>CNA::Internal::Media::SavedPictureStore</c> exactly, including its security-relevant
/// filename sanitization (<see cref="SanitizePictureName"/> below): the picture name is
/// caller-supplied and untrusted, so it must be reduced to a single, safe path *segment* (no
/// directory traversal, no absolute-path escape) before it's ever used to build a real filesystem
/// path -- a caller passing <c>"../../etc/passwd"</c> or an absolute path must not be able to
/// write outside the Saved Pictures directory.
///
/// No longer backs <see cref="MediaLibrary.SavePicture(string,byte[])"/>, which goes to native
/// (<c>cna_media_library_save_picture</c>) since the media-library rebinding. It is kept because
/// its sanitization is used elsewhere -- <c>ContentManager</c> and the model builders reach for the
/// same "reduce an untrusted name to one safe path segment" rule, and that is exactly the kind of
/// logic that must exist once rather than be reimplemented per caller. <c>internal</c>: an
/// implementation detail, not standalone public API.
/// </summary>
internal static class SavedPictureStore
{
    /// <summary>Returns the "Saved Pictures" subfolder path under <paramref name="picturesRoot"/>,
    /// creating it if it doesn't already exist. Returns <see langword="null"/> if it couldn't be
    /// created (matches the real C++ engine's own <c>std::error_code</c>-based "best effort, no
    /// exception" contract -- catches broadly here for the same reason: any of several possible
    /// I/O failures should degrade the same way, not be distinguished).</summary>
    internal static string? GetSavedPicturesDirectory(string picturesRoot)
    {
        if (string.IsNullOrEmpty(picturesRoot))
        {
            return null;
        }

        string dir = Path.Combine(picturesRoot, "Saved Pictures");
        try
        {
            Directory.CreateDirectory(dir);
        }
        catch (Exception)
        {
            // Matches the real C++ engine's own check exactly: only a genuine failure (the
            // directory still doesn't exist afterward) is reported as null -- an exception thrown
            // despite the directory actually existing (e.g. a benign race with another creator)
            // is not treated as failure.
            if (!Directory.Exists(dir))
            {
                return null;
            }
        }

        return dir;
    }

    /// <summary>Writes <paramref name="data"/> to a new file named <c>"&lt;name&gt;.&lt;ext&gt;"</c>
    /// inside the Saved Pictures directory (extension sniffed from the image's own magic bytes;
    /// defaults to <c>".png"</c> if unrecognized), returning the full path written, or
    /// <see langword="null"/> on failure.</summary>
    internal static string? SavePicture(string picturesRoot, string name, byte[] data)
    {
        string? dir = GetSavedPicturesDirectory(picturesRoot);
        if (dir is null)
        {
            return null;
        }

        string outPath = Path.Combine(dir, SanitizePictureName(name) + SniffImageExtension(data));
        try
        {
            File.WriteAllBytes(outPath, data);
        }
        catch (Exception)
        {
            return null;
        }

        return outPath;
    }

    private static readonly byte[] PngMagic = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    private static string SniffImageExtension(byte[] data)
    {
        if (data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual(PngMagic))
        {
            return ".png";
        }

        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            return ".jpg";
        }

        if (data.Length >= 2 && data[0] == (byte)'B' && data[1] == (byte)'M')
        {
            return ".bmp";
        }

        return ".png"; // unrecognized -- default to a supported, rescan-visible extension
    }

    /// <summary>Reduces <paramref name="name"/> to a single safe path segment. Normalizes
    /// backslashes to forward slashes first (Windows-style separators are also a real traversal
    /// vector on Linux, where <see cref="Path.GetFileName(string)"/> only treats <c>/</c> as a
    /// separator), keeps only the last path segment, and rejects <c>"."</c>/<c>".."</c>/empty
    /// results in favor of a safe fallback name -- matches the real C++ engine's own
    /// <c>SanitizePictureName</c> exactly.</summary>
    private static string SanitizePictureName(string name)
    {
        string normalized = name.Replace('\\', '/');
        string segment = Path.GetFileName(normalized);

        if (segment.Length == 0 || segment is "." or "..")
        {
            return "picture";
        }

        return segment;
    }
}
