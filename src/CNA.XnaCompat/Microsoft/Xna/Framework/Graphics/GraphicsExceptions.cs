namespace Microsoft.Xna.Framework.Graphics;

[Serializable]
public sealed class DeviceLostException : Exception
{
    public DeviceLostException()
    {
    }

    public DeviceLostException(string message)
        : base(message)
    {
    }

    public DeviceLostException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

[Serializable]
public sealed class DeviceNotResetException : Exception
{
    public DeviceNotResetException()
    {
    }

    public DeviceNotResetException(string message)
        : base(message)
    {
    }

    public DeviceNotResetException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

[Serializable]
public sealed class NoSuitableGraphicsDeviceException : Exception
{
    public NoSuitableGraphicsDeviceException()
    {
    }

    public NoSuitableGraphicsDeviceException(string message)
        : base(message)
    {
    }

    public NoSuitableGraphicsDeviceException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
