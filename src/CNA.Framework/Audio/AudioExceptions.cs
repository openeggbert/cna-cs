namespace CNA.Audio;

/// <summary>
/// Matches real XNA's <c>InstancePlayLimitException</c>: too many
/// <see cref="SoundEffectInstance"/>s are already playing.
///
/// Not thrown by <see cref="SoundEffect.Play()"/>, which reports the same condition the way the
/// canonical route does -- by returning <see langword="false"/> (<c>audio.h:477</c>). The type
/// exists because XNA source catches it, and because
/// <see cref="SoundEffect.CreateInstance"/> is where XNA itself throws rather than returns.
/// </summary>
public class InstancePlayLimitException : Exception
{
    public InstancePlayLimitException()
        : base("The sound effect instance play limit has been reached.")
    {
    }

    public InstancePlayLimitException(string message)
        : base(message)
    {
    }

    public InstancePlayLimitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Matches real XNA's <c>NoAudioHardwareException</c>: no usable audio device is
/// present.</summary>
public class NoAudioHardwareException : Exception
{
    public NoAudioHardwareException()
        : base("No audio hardware is available.")
    {
    }

    public NoAudioHardwareException(string message)
        : base(message)
    {
    }

    public NoAudioHardwareException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Matches real XNA's <c>NoMicrophoneConnectedException</c>: an operation needed a
/// microphone and none is connected. <see cref="Microphone.Default"/> answers
/// <see langword="null"/> for that case rather than throwing, matching the ABI's own
/// availability flag -- this type exists for XNA source that catches it.</summary>
public class NoMicrophoneConnectedException : Exception
{
    public NoMicrophoneConnectedException()
        : base("No microphone is connected.")
    {
    }

    public NoMicrophoneConnectedException(string message)
        : base(message)
    {
    }

    public NoMicrophoneConnectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
