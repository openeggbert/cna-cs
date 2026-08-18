namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>DeviceLostException</c>. Subclasses its
/// <c>CNA.Graphics</c> counterpart, so a <c>catch</c> written against either namespace catches an
/// instance thrown by this layer -- which is the point of the type existing at all.</summary>
public class DeviceLostException : CNA.Graphics.DeviceLostException
{
    public DeviceLostException()
    {
    }

    public DeviceLostException(string message)
        : base(message)
    {
    }

    public DeviceLostException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>XNA 4.0-compatible <c>DeviceNotResetException</c>. See
/// <see cref="DeviceLostException"/>.</summary>
public class DeviceNotResetException : CNA.Graphics.DeviceNotResetException
{
    public DeviceNotResetException()
    {
    }

    public DeviceNotResetException(string message)
        : base(message)
    {
    }

    public DeviceNotResetException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>XNA 4.0-compatible <c>NoSuitableGraphicsDeviceException</c>. See
/// <see cref="DeviceLostException"/>.</summary>
public class NoSuitableGraphicsDeviceException : CNA.Graphics.NoSuitableGraphicsDeviceException
{
    public NoSuitableGraphicsDeviceException()
    {
    }

    public NoSuitableGraphicsDeviceException(string message)
        : base(message)
    {
    }

    public NoSuitableGraphicsDeviceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
