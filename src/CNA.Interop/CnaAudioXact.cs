using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_AudioListener</c> exactly
/// (<c>audio.h</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaAudioListener
{
    public uint StructSize;
    public uint StructVersion;
    public CnaVector3 Forward;
    public CnaVector3 Position;
    public CnaVector3 Up;
    public CnaVector3 Velocity;

    public unsafe CnaAudioListener()
    {
        StructSize = (uint)sizeof(CnaAudioListener);
        StructVersion = 1;
    }
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_AudioEmitter</c> exactly
/// (<c>audio.h</c>) -- the same fields as <see cref="CnaAudioListener"/> plus
/// <see cref="DopplerScale"/>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaAudioEmitter
{
    public uint StructSize;
    public uint StructVersion;
    public float DopplerScale;
    public CnaVector3 Forward;
    public CnaVector3 Position;
    public CnaVector3 Up;
    public CnaVector3 Velocity;

    public unsafe CnaAudioEmitter()
    {
        StructSize = (uint)sizeof(CnaAudioEmitter);
        StructVersion = 1;
    }
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_CueInfo</c> exactly
/// (<c>xact.h</c>) -- eight independent state flags in one call, rather than eight getters.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaCueInfo
{
    public uint StructSize;
    public uint StructVersion;
    public byte IsCreated;
    public byte IsDisposed;
    public byte IsPaused;
    public byte IsPlaying;
    public byte IsPrepared;
    public byte IsPreparing;
    public byte IsStopped;
    public byte IsStopping;

    public unsafe CnaCueInfo()
    {
        StructSize = (uint)sizeof(CnaCueInfo);
        StructVersion = 1;
    }
}
