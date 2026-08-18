namespace Microsoft.Xna.Framework.Media;

/// <summary>
/// Shared base for this namespace's compat views over a <c>CNA.Media</c> library object
/// (<see cref="Album"/>/<see cref="Artist"/>/<see cref="Genre"/>/<see cref="Playlist"/>/
/// <see cref="Picture"/>/<see cref="PictureAlbum"/>).
///
/// Composition, not inheritance -- see <see cref="ReadOnlyMediaCollection{TCompat,TBase}"/> for
/// why. Equality, hashing and <c>ToString</c> forward to the wrapped object, so two compat wrappers
/// over the same library object compare equal even though they are distinct wrappers, which is what
/// an XNA caller means by comparing two albums.
/// </summary>
/// <typeparam name="TBase">The wrapped <c>CNA.Media</c> type.</typeparam>
public abstract class MediaLibraryObject<TBase> : IDisposable
    where TBase : CNA.Media.MediaLibraryObject
{
    private protected MediaLibraryObject(TBase inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        Inner = inner;
    }

    internal TBase Inner { get; }

    public bool IsDisposed => Inner.IsDisposed;

    public void Dispose()
    {
        Inner.Dispose();
        GC.SuppressFinalize(this);
    }

    public override bool Equals(object? obj) =>
        obj is MediaLibraryObject<TBase> other && Inner.Equals(other.Inner);

    public override int GetHashCode() => Inner.GetHashCode();

    public override string ToString() => Inner.ToString() ?? string.Empty;
}
