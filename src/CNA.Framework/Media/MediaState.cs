namespace CNA.Media;

/// <summary>Real XNA enum, exact ordinals -- confirmed against the real openeggbert/cna C++
/// engine's own <c>MediaState</c> declaration order.</summary>
public enum MediaState
{
    Stopped = 0,
    Playing = 1,
    Paused = 2,
}
