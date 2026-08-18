namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible <c>AudioEngine</c>. A pure subclass -- only
/// <see cref="GetCategory"/> needs re-typing.</summary>
public class AudioEngine : CNA.Audio.AudioEngine
{
    public AudioEngine(string settingsFile)
        : base(settingsFile)
    {
    }

    public new AudioCategory GetCategory(string name) => new(base.GetCategory(name));
}
