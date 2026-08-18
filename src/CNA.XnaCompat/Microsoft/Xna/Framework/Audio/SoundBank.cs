namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible <c>SoundBank</c>. A pure subclass -- only <see cref="GetCue"/>
/// needs re-typing; the 3D <c>PlayCue</c> overload's listener/emitter upcast.</summary>
public class SoundBank : CNA.Audio.SoundBank
{
    public SoundBank(AudioEngine audioEngine, string filename)
        : base(audioEngine, filename)
    {
    }

    public new Cue GetCue(string name) => new(base.GetCue(name));
}
