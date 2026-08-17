using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_SoundEffectCreateInfo</c> exactly
/// (<c>audio.h:49-64</c>) -- confirmed directly with <c>cnabinding</c> that the real struct only
/// carries <see cref="SampleRate"/>/<see cref="Channels"/>: the loop-start/loop-length region this
/// project's own <c>SoundEffect</c> constructor also exposes does <em>not</em> survive into this
/// struct at all. It is instead a real, separate creation route,
/// <see cref="Native.cna_sound_effect_create_pcm16_range_ext"/> ("the canonical seven-argument
/// constructor"), which takes offset/count/loop_start/loop_length as plain parameters alongside
/// this struct rather than folding them into it -- matches real XNA's own 7-argument
/// <c>SoundEffect</c> constructor shape exactly, which this project's own constructor was already
/// designed around before this migration. See <see cref="CnaGameFrameHooks"/>'s own constructor
/// doc comment for the self-populating-constructor rationale.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaSoundEffectCreateInfo
{
    public uint StructSize;
    public uint StructVersion;
    public uint SampleRate;
    public uint Channels;
    public ulong Reserved;

    public unsafe CnaSoundEffectCreateInfo()
    {
        StructSize = (uint)sizeof(CnaSoundEffectCreateInfo);
        StructVersion = 1;
    }
}

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_SoundEffectInstanceInfo</c> exactly
/// (<c>audio.h:67-94</c>) -- the real ABI has no individual <c>cna_sound_effect_instance_get_state</c>/
/// <c>_get_volume</c>/<c>_get_pitch</c>/<c>_get_pan</c>/<c>_get_is_looped</c> getters at all (unlike
/// this project's old guessed shape, which assumed five separate getters symmetric with the five
/// separate setters that <em>do</em> exist individually) -- only this one combined snapshot call,
/// <see cref="Native.cna_sound_effect_instance_get_info"/>. <c>CNA.Audio.SoundEffectInstance</c>'s
/// getters all now go through this one native round trip instead. See
/// <see cref="CnaGameFrameHooks"/>'s own constructor doc comment for the self-populating
/// -constructor rationale.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaSoundEffectInstanceInfo
{
    public uint StructSize;
    public uint StructVersion;
    public uint State;
    public byte IsLooped;
    public CnaReservedBytes3 Reserved0;
    public float Volume;
    public float Pitch;
    public float Pan;
    public uint Reserved1;

    public unsafe CnaSoundEffectInstanceInfo()
    {
        StructSize = (uint)sizeof(CnaSoundEffectInstanceInfo);
        StructVersion = 1;
    }
}
