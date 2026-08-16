namespace CNA.Audio;

/// <summary>Matches real XNA's <c>AudioChannels</c> values exactly (starts at 1, not 0) --
/// also confirmed against the real openeggbert/cna C++ engine's own
/// <c>Microsoft::Xna::Framework::Audio::AudioChannels</c> enum
/// (<c>modules/audio/include/.../AudioChannels.hpp</c>), which declares the same values.</summary>
public enum AudioChannels
{
    Mono = 1,
    Stereo = 2,
}
