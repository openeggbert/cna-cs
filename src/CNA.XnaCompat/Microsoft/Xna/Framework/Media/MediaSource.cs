namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>MediaSource</c>: a compat-typed view over
/// <c>CNA.Media.MediaSource</c>, wrapping for the same reason the rest of this namespace's media
/// family does.</summary>
public sealed class MediaSource
{
    internal MediaSource(CNA.Media.MediaSource inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        Inner = inner;
    }

    internal CNA.Media.MediaSource Inner { get; }

    public MediaSourceType MediaSourceType => (MediaSourceType)(int)Inner.MediaSourceType;

    public string Name => Inner.Name;

    public override string ToString() => Name;

    /// <summary>Enumerates the device's real media sources. It used to return a hardcoded
    /// local-device entry; <c>CNA.Media.MediaSource</c> now asks native, and this forwards.</summary>
    public static IList<MediaSource> GetAvailableMediaSources()
    {
        IReadOnlyList<CNA.Media.MediaSource> sources = CNA.Media.MediaSource.GetAvailableMediaSources();
        var wrapped = new MediaSource[sources.Count];
        for (int i = 0; i < wrapped.Length; i++)
        {
            wrapped[i] = new MediaSource(sources[i]);
        }

        return wrapped;
    }
}
