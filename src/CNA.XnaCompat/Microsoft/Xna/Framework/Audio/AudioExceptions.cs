namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible <c>InstancePlayLimitException</c>. Subclasses its
/// <c>CNA.Audio</c> counterpart -- see
/// <c>Microsoft.Xna.Framework.Graphics.DeviceLostException</c> for why.</summary>
public class InstancePlayLimitException : CNA.Audio.InstancePlayLimitException
{
    public InstancePlayLimitException()
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

/// <summary>XNA 4.0-compatible <c>NoAudioHardwareException</c>.</summary>
public class NoAudioHardwareException : CNA.Audio.NoAudioHardwareException
{
    public NoAudioHardwareException()
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

/// <summary>XNA 4.0-compatible <c>NoMicrophoneConnectedException</c>.</summary>
public class NoMicrophoneConnectedException : CNA.Audio.NoMicrophoneConnectedException
{
    public NoMicrophoneConnectedException()
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
