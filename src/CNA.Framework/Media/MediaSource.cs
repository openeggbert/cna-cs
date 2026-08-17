namespace CNA.Media;

/// <summary>
/// A media source device <see cref="MediaLibrary"/> reads content from. Real XNA's own constructor
/// is <c>MediaLibrary</c>-only (matching the real C++ engine's own <c>private</c>,
/// <c>friend class MediaLibrary</c> constructor exactly) -- kept <c>internal</c> here too, since
/// nothing outside <see cref="MediaLibrary"/> in this project ever needs to build one.
/// </summary>
public class MediaSource
{
    internal MediaSource(MediaSourceType mediaSourceType, string name)
    {
        MediaSourceType = mediaSourceType;
        Name = name;
    }

    public MediaSourceType MediaSourceType { get; }

    public string Name { get; }

    public override string ToString() => Name;

    /// <summary>
    /// Real XNA enumerates every media source actually reachable on the device. This project has
    /// no way to discover a real <see cref="MediaSourceType.WindowsMediaConnect"/> device, so this
    /// always returns exactly the one source that's unconditionally real on any device: the local
    /// one -- not an empty list (which would incorrectly suggest no media source is available at
    /// all) and not a guessed list of sources that don't actually exist here.
    /// </summary>
    public static IReadOnlyList<MediaSource> GetAvailableMediaSources() =>
        [new MediaSource(MediaSourceType.LocalDevice, "Local Device")];
}
