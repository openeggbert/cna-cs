namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible <c>WaveBank</c>. A pure subclass -- every member involves only
/// strings and bools.</summary>
public class WaveBank : CNA.Audio.WaveBank
{
    public WaveBank(AudioEngine audioEngine, string nonStreamingWaveBankFilename)
        : base(audioEngine, nonStreamingWaveBankFilename)
    {
    }

    public WaveBank(AudioEngine audioEngine, string streamingWaveBankFilename, int offset, short packetSize)
        : base(audioEngine, streamingWaveBankFilename, offset, packetSize)
    {
    }
}
