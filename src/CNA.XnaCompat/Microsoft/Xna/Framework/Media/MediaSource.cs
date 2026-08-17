namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>MediaSource</c>. Extends <c>CNA.Media.MediaSource</c> directly
/// (safe here -- see <c>MediaLibrary</c>'s own doc comment for why the downcast-pass-through
/// pattern this session has used carefully elsewhere is unconditionally safe throughout this
/// feature).</summary>
public class MediaSource : CNA.Media.MediaSource
{
    internal MediaSource(MediaSourceType mediaSourceType, string name)
        : base((CNA.Media.MediaSourceType)(int)mediaSourceType, name)
    {
    }

    public new MediaSourceType MediaSourceType => (MediaSourceType)(int)base.MediaSourceType;

    public static new IReadOnlyList<MediaSource> GetAvailableMediaSources() =>
        [new MediaSource(MediaSourceType.LocalDevice, "Local Device")];
}
