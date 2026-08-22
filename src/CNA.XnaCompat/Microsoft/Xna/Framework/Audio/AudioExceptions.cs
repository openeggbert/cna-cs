using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Audio;

[Serializable]
public sealed class InstancePlayLimitException : ExternalException
{
    public InstancePlayLimitException()
    {
    }

    public InstancePlayLimitException(string message)
        : base(message)
    {
    }

    public InstancePlayLimitException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

[Serializable]
public sealed class NoAudioHardwareException : ExternalException
{
    public NoAudioHardwareException()
    {
    }

    public NoAudioHardwareException(string message)
        : base(message)
    {
    }

    public NoAudioHardwareException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

[Serializable]
public sealed class NoMicrophoneConnectedException : Exception
{
    public NoMicrophoneConnectedException()
    {
    }

    public NoMicrophoneConnectedException(string message)
        : base(message)
    {
    }

    public NoMicrophoneConnectedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
