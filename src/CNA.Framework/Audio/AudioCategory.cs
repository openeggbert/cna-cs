using CNA.Interop;

namespace CNA.Audio;

/// <summary>
/// Matches real XNA's <c>AudioCategory</c>: a named group of cues an
/// <see cref="AudioEngine"/> can pause, resume, stop or re-volume together.
///
/// A <see langword="struct"/> in real XNA, and its equality compares the underlying category
/// rather than the wrapper -- reproduced here (via the C API's own
/// <c>cna_audio_category_equals</c>/<c>_get_hash_code</c>) because two lookups of the same name
/// legitimately produce two objects, and XNA callers compare them.
///
/// A class here rather than a struct despite that, because it owns a native handle that must be
/// destroyed; a struct could not do so safely.
/// </summary>
public class AudioCategory : IEquatable<AudioCategory>, IDisposable
{
    private readonly NativeResourceHandle _handle;

    internal AudioCategory(nint nativeHandleValue)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, h => Native.cna_audio_category_destroy(new CnaHandle(h)));
    }

    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    public unsafe string Name => NativeStringReader.Read(
        Native.cna_audio_category_get_name_size, Native.cna_audio_category_copy_name, NativeHandle, nameof(Name));

    public void Pause()
    {
        CnaResult result = Native.cna_audio_category_pause(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Pause));
    }

    public void Resume()
    {
        CnaResult result = Native.cna_audio_category_resume(NativeHandle);
        CnaException.ThrowIfFailed(result, nameof(Resume));
    }

    public void SetVolume(float volume)
    {
        CnaResult result = Native.cna_audio_category_set_volume(NativeHandle, volume);
        CnaException.ThrowIfFailed(result, nameof(SetVolume));
    }

    public void Stop(AudioStopOptions options)
    {
        CnaResult result = Native.cna_audio_category_stop(NativeHandle, (uint)options);
        CnaException.ThrowIfFailed(result, nameof(Stop));
    }

    public bool Equals(AudioCategory? other)
    {
        if (other is null)
        {
            return false;
        }

        CnaResult result = Native.cna_audio_category_equals(NativeHandle, other.NativeHandle, out byte equal);
        CnaException.ThrowIfFailed(result, nameof(Equals));
        return equal != 0;
    }

    public override bool Equals(object? obj) => obj is AudioCategory other && Equals(other);

    public override int GetHashCode()
    {
        CnaResult result = Native.cna_audio_category_get_hash_code(NativeHandle, out int hashCode);
        CnaException.ThrowIfFailed(result, nameof(GetHashCode));
        return hashCode;
    }

    public static bool operator ==(AudioCategory? a, AudioCategory? b) => a is null ? b is null : a.Equals(b);

    public static bool operator !=(AudioCategory? a, AudioCategory? b) => !(a == b);

    public override string ToString() => Name;

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
