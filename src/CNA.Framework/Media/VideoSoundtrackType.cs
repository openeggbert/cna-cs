namespace CNA.Media;

/// <summary>Matches real XNA's <c>VideoSoundtrackType</c> values exactly -- also confirmed against
/// the real, shipped openeggbert/cna C API's own <c>CNA_VIDEO_SOUNDTRACK_TYPE_*</c> constants
/// (<c>media.h:44-53</c>).</summary>
public enum VideoSoundtrackType
{
    Music = 0,
    Dialog = 1,
    MusicAndDialog = 2,
}
