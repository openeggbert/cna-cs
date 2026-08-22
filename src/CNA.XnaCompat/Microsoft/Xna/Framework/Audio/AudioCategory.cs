namespace Microsoft.Xna.Framework.Audio;

/// <summary>Represents a category of authored sounds.</summary>
public struct AudioCategory : IEquatable<AudioCategory>
{
    private readonly AudioEngine? _parent;
    private readonly CNA.Audio.AudioCategory? _category;

    internal AudioCategory(AudioEngine parent, CNA.Audio.AudioCategory category)
    {
        _parent = parent;
        _category = category;
    }

    public string Name => _category is null ? null! : _category.Name;

    public void Pause() => Category.Pause();

    public void Resume() => Category.Resume();

    public void SetVolume(float volume) => Category.SetVolume(volume);

    public void Stop(AudioStopOptions options) => Category.Stop((CNA.Audio.AudioStopOptions)(int)options);

    public bool Equals(AudioCategory other)
    {
        if (!ReferenceEquals(_parent, other._parent))
        {
            return false;
        }

        if (_category is null || other._category is null)
        {
            return _category is null && other._category is null;
        }

        return _category.Equals(other._category);
    }

    public override bool Equals(object? obj) => obj is AudioCategory other && Equals(other);

    public override int GetHashCode() => (_category?.GetHashCode() ?? 0) ^ (_parent?.GetHashCode() ?? 0);

    public override string ToString() => _category?.Name ?? string.Empty;

    public static bool operator ==(AudioCategory value1, AudioCategory value2) => value1.Equals(value2);

    public static bool operator !=(AudioCategory value1, AudioCategory value2) => !value1.Equals(value2);

    private CNA.Audio.AudioCategory Category =>
        _category ?? throw new InvalidOperationException("The audio category is uninitialized.");
}
