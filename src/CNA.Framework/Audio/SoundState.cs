namespace CNA.Audio;

/// <summary>Matches real XNA's <c>SoundState</c> values -- also confirmed against the real
/// openeggbert/cna C++ engine's own <c>Microsoft::Xna::Framework::Audio::SoundState</c>
/// (<c>modules/audio/include/.../SoundState.hpp</c>), a plain <c>enum class</c> in the same
/// declaration order (0/1/2).</summary>
public enum SoundState
{
    Playing = 0,
    Paused = 1,
    Stopped = 2,
}
