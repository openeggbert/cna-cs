namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible <c>AudioCategory</c>. A thin re-typing wrapper rather than a
/// subclass, because <see cref="CNA.Audio.AudioCategory"/>'s only constructor is internal
/// (categories come from an engine lookup). Equality forwards to the base, which compares the
/// underlying native category rather than the wrapper -- see that type's doc comment for why that
/// matters.</summary>
public class AudioCategory : IEquatable<AudioCategory>, IDisposable
{
    private readonly CNA.Audio.AudioCategory _category;

    internal AudioCategory(CNA.Audio.AudioCategory category)
    {
        _category = category;
    }

    public string Name => _category.Name;

    public void Pause() => _category.Pause();

    public void Resume() => _category.Resume();

    public void SetVolume(float volume) => _category.SetVolume(volume);

    public void Stop(AudioStopOptions options) => _category.Stop((CNA.Audio.AudioStopOptions)(int)options);

    public bool Equals(AudioCategory? other) => other is not null && _category.Equals(other._category);

    public override bool Equals(object? obj) => obj is AudioCategory other && Equals(other);

    public override int GetHashCode() => _category.GetHashCode();

    public static bool operator ==(AudioCategory? a, AudioCategory? b) => a is null ? b is null : a.Equals(b);

    public static bool operator !=(AudioCategory? a, AudioCategory? b) => !(a == b);

    public override string ToString() => _category.ToString();

    public void Dispose()
    {
        _category.Dispose();
        GC.SuppressFinalize(this);
    }
}
