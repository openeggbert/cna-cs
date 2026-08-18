namespace CNA.Audio;

/// <summary>Matches real XNA's <c>MicrophoneState</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_MICROPHONE_STATE_*</c> constants
/// (<c>audio.h</c>).</summary>
public enum MicrophoneState
{
    Started = 0,
    Stopped = 1,
}
