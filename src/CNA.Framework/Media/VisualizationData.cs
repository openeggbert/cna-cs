namespace CNA.Media;

/// <summary>
/// Real-time audio visualization data for whatever song <see cref="MediaPlayer"/> is currently
/// playing: a 256-bin frequency spectrum (via FFT) and 256 raw waveform samples, matching real
/// XNA's own <c>VisualizationData</c> exactly. Grounded against the real openeggbert/cna C++
/// engine's own working implementation (<c>modules/media/src/Internal/VisualizationCapture.cpp</c>/
/// <c>VisualizationFFT.cpp</c>, <c>modules/media/src/Xna/VisualizationData.cpp</c>) -- unlike
/// <c>MediaLibrary</c>'s music-scanning subsystem, this is real, working, and unusually
/// well-engineered, not blocked on infrastructure this project has no equivalent for: a lock-free
/// single-producer/single-consumer ring buffer fed from SDL3_mixer's post-mix callback (installed
/// only while <see cref="MediaPlayer.IsVisualizationEnabled"/> is <see langword="true"/> -- zero
/// cost when disabled), and a from-scratch, dependency-free 512-point radix-2 FFT (Hann-windowed,
/// magnitude-normalized) over the most recent 256 captured samples. Both arrays stay all-zero when
/// visualization is disabled or nothing has been captured yet -- the real engine's own
/// <c>GetVisualizationData</c> takes that exact fallback already, not invented here.
///
/// <see cref="Frequencies"/>/<see cref="Samples"/> are populated in place by
/// <see cref="MediaPlayer.GetVisualizationData"/>, matching real XNA's own "reuse the same array
/// instances across calls" contract -- this type's own constructor allocates both arrays once,
/// never replaces them.
/// </summary>
public class VisualizationData
{
    public const int Size = 256;

    public VisualizationData()
    {
        Frequencies = new float[Size];
        Samples = new float[Size];
    }

    public float[] Frequencies { get; }

    public float[] Samples { get; }
}
