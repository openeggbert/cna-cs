namespace CNA.Audio;

/// <summary>Matches real XNA's <c>AudioStopOptions</c> values exactly -- also confirmed against
/// the real, shipped openeggbert/cna C API's own <c>CNA_AUDIO_STOP_OPTIONS_*</c> constants
/// (<c>xact.h</c>).</summary>
public enum AudioStopOptions
{
    AsAuthored = 0,
    Immediate = 1,
}
