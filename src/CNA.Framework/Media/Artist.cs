namespace CNA.Media;

/// <summary>
/// A music artist in a <see cref="MediaLibrary"/>. Real XNA's own constructor is
/// <c>MediaLibrary</c>-only, matching the real C++ engine's <c>private</c>, friended constructor
/// exactly -- kept <c>internal</c> here too (not a <c>CNAEXT</c> public deviation the way
/// <see cref="Song"/>'s own constructor needed to be): unlike <c>Song</c>, an <see cref="Artist"/>
/// only makes sense as part of a coherent library scan (cross-referenced with its
/// <see cref="Albums"/>/<see cref="Songs"/>), which this project doesn't implement (see
/// <see cref="MediaLibrary"/>'s own doc comment) -- so nothing here would ever have a real reason
/// to hand-build one.
/// </summary>
public class Artist : IDisposable, IEquatable<Artist>
{
    internal Artist(string name, AlbumCollection albums, SongCollection songs)
    {
        Name = name;
        Albums = albums;
        Songs = songs;
    }

    public AlbumCollection Albums { get; }

    public bool IsDisposed { get; private set; }

    public string Name { get; }

    public SongCollection Songs { get; }

    public void Dispose() => IsDisposed = true;

    /// <summary>By name only -- matches the real C++ engine's own <c>Artist::Equals</c>
    /// exactly.</summary>
    public bool Equals(Artist? other) => other is not null && Name == other.Name;

    public override bool Equals(object? obj) => Equals(obj as Artist);

    public override int GetHashCode() => Name.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(Artist? left, Artist? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Artist? left, Artist? right) => !(left == right);
}
