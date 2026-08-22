namespace Microsoft.Xna.Framework.Media;

/// <summary>Internal ownership adapter for one native-backed media-library object.</summary>
internal sealed class MediaLibraryObjectAdapter<TBase>
    where TBase : CNA.Media.MediaLibraryObject
{
    internal MediaLibraryObjectAdapter(TBase inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        Inner = inner;
    }

    internal TBase Inner { get; }

    internal bool IsDisposed => Inner.IsDisposed;

    internal void Dispose() => Inner.Dispose();

    internal void ReleaseHandleOnly() => Inner.ReleaseHandleOnly();

    internal bool Equals(MediaLibraryObjectAdapter<TBase>? other) =>
        other is not null && Inner.Equals(other.Inner);

    internal int GetHashCodeValue() => Inner.GetHashCode();
}
