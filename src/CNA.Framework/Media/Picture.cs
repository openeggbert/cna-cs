namespace CNA.Media;

/// <summary>
/// A picture in a <see cref="MediaLibrary"/>. Real XNA's own constructor is
/// <c>MediaLibrary</c>-only, matching the real C++ engine's own <c>private</c>, friended
/// constructor exactly -- same reasoning as <c>Album</c>'s own doc comment.
///
/// <see cref="GetImage"/> is real, not a stub: unlike <see cref="MediaLibrary"/>'s music-library
/// scanning (irreducibly bound to native tag-parsing/FFmpeg infrastructure), opening a plain file
/// stream over an already-known image path needs nothing beyond the .NET BCL. <see cref="GetThumbnail"/>
/// always falls back to <see cref="GetImage"/> -- this matches the real C++ engine's own actual
/// fallback path (its <c>ThumbnailGenerator</c> falls back to returning the full-size image
/// whenever real PNG-downscaling thumbnail generation fails), reproduced here as the only path
/// taken rather than invented, since this project has no image-decoding library to generate a real
/// downscaled thumbnail with.
/// </summary>
public class Picture : IDisposable, IEquatable<Picture>
{
    internal Picture(string name, PictureAlbum? album, int width, int height, DateTime date, string path)
    {
        Name = name;
        Album = album;
        Width = width;
        Height = height;
        Date = date;
        Path = path;
    }

    public PictureAlbum? Album { get; }

    public DateTime Date { get; }

    public int Height { get; }

    public bool IsDisposed { get; private set; }

    public string Name { get; }

    public int Width { get; }

    /// <summary>This picture's resolved file path, usable as the token
    /// <see cref="MediaLibrary.GetPictureFromToken"/> accepts. <c>CNAEXT</c>: real XNA's own
    /// "opaque library token" has no real equivalent source on desktop (it historically came from
    /// a native Zune/Xbox picture-picker UI); the resolved file path is used as a simple, real,
    /// stable token instead, matching the real C++ engine's own <c>getTokenEXT()</c> exactly, so
    /// the token API is actually usable end-to-end rather than merely present.</summary>
    public string Token => Path;

    internal string Path { get; }

    public Stream GetImage() => File.OpenRead(Path);

    public Stream GetThumbnail() => GetImage();

    public void Dispose() => IsDisposed = true;

    /// <summary>By <see cref="Path"/> -- matches the real C++ engine's own <c>Picture::Equals</c>
    /// exactly.</summary>
    public bool Equals(Picture? other) => other is not null && Path == other.Path;

    public override bool Equals(object? obj) => Equals(obj as Picture);

    public override int GetHashCode() => Path.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(Picture? left, Picture? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Picture? left, Picture? right) => !(left == right);
}
