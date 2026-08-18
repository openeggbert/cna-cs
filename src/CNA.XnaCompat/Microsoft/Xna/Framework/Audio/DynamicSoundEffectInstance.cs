namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible <c>DynamicSoundEffectInstance</c>. A pure subclass -- the buffer
/// surface involves only byte arrays and <see cref="TimeSpan"/>, and playback control comes from
/// the inherited <see cref="SoundEffectInstance"/>. Only the constructor's
/// <see cref="AudioChannels"/> needs re-typing.</summary>
public class DynamicSoundEffectInstance : CNA.Audio.DynamicSoundEffectInstance
{
    public DynamicSoundEffectInstance(int sampleRate, AudioChannels channels)
        : base(sampleRate, (CNA.Audio.AudioChannels)(int)channels)
    {
    }
}
