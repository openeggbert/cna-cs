namespace CNA.Media;

/// <summary>Real XNA enum, exact ordinals (non-contiguous -- confirmed against the real
/// openeggbert/cna C++ engine's own <c>MediaSourceType</c> declaration, not assumed
/// contiguous).</summary>
public enum MediaSourceType
{
    LocalDevice = 0,
    WindowsMediaConnect = 4,
}
