using CNA.Interop;

namespace CNA.Audio;

/// <summary>Matches real XNA's <c>WaveBank</c>: the compiled wave data (<c>.xwb</c>) a
/// <see cref="SoundBank"/>'s cues play from. Loaded against an <see cref="AudioEngine"/> and used
/// only by it -- game code never touches individual waves, which is why this type has no accessors
/// beyond its state flags.</summary>
public class WaveBank : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public WaveBank(AudioEngine audioEngine, string nonStreamingWaveBankFilename)
    {
        ArgumentNullException.ThrowIfNull(audioEngine);
        ArgumentNullException.ThrowIfNull(nonStreamingWaveBankFilename);

        CnaHandle waveBank = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            nonStreamingWaveBankFilename, view => Native.cna_wave_bank_create(audioEngine.NativeHandle, view, out waveBank));
        GC.KeepAlive(audioEngine);
        CnaException.ThrowIfFailed(result, nameof(WaveBank));

        _handle = new NativeResourceHandle(waveBank.AsNint, Release);
    }

    /// <summary>The streaming form, matching real XNA's four-argument constructor: wave data is
    /// read from disk on demand rather than loaded whole.</summary>
    public WaveBank(AudioEngine audioEngine, string streamingWaveBankFilename, int offset, short packetSize)
    {
        ArgumentNullException.ThrowIfNull(audioEngine);
        ArgumentNullException.ThrowIfNull(streamingWaveBankFilename);

        CnaHandle waveBank = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            streamingWaveBankFilename,
            view => Native.cna_wave_bank_create_streaming(audioEngine.NativeHandle, view, offset, packetSize, out waveBank));
        GC.KeepAlive(audioEngine);
        CnaException.ThrowIfFailed(result, nameof(WaveBank));

        _handle = new NativeResourceHandle(waveBank.AsNint, Release);
    }

    private static void Release(nint handleValue) => Native.cna_wave_bank_destroy(new CnaHandle(handleValue));

    /// <summary>
    /// The native handle, read out of the owning <see cref="NativeResourceHandle"/>. Every caller
    /// pairs it with <see cref="GC.KeepAlive(object)"/> after the native call: once the handle
    /// value has been read this object can be unreachable, and an unreachable
    /// <see cref="System.Runtime.InteropServices.SafeHandle"/> may have its critical finalizer run
    /// <c>destroy</c> while the call is still in flight. Defeating exactly that is what
    /// <see cref="System.Runtime.InteropServices.SafeHandle"/> is for, so reading the handle
    /// without keeping its owner alive gives the guarantee up -- see <c>plan.md</c> WP17.
    /// </summary>
    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public bool IsDisposed => GetFlag(Native.cna_wave_bank_get_is_disposed, nameof(IsDisposed));

    /// <summary>Whether the bank has finished loading. A streaming bank is not prepared
    /// immediately, which is why real XNA exposes this at all.</summary>
    public bool IsPrepared => GetFlag(Native.cna_wave_bank_get_is_prepared, nameof(IsPrepared));

    public bool IsInUse => GetFlag(Native.cna_wave_bank_get_is_in_use, nameof(IsInUse));

    private delegate CnaResult GetFlagFunc(CnaHandle waveBank, out byte outValue);

    private bool GetFlag(GetFlagFunc getter, string propertyName)
    {
        CnaResult result = getter(NativeHandle, out byte value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, propertyName);
        return value != 0;
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
