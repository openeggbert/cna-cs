namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>DeviceLostException</c>: the graphics device has been lost and cannot be
/// used until it is reset.
///
/// XNA source catches these by type, so they must exist as distinct types rather than be folded
/// into one. They were missing until the WP16 re-audit -- the coverage count they were absent from
/// had been taken against an incomplete enumeration of XNA 4.0 rather than against XNA 4.0.
///
/// Nothing throws them yet. The C API reports device state through <c>CNA_Result</c> and
/// <c>cna_graphics_device_get_status</c> rather than by unwinding, so mapping a result code to one
/// of these is the graphics device's job at whatever point CNA starts reporting loss; declaring the
/// types is what lets ported source compile and catch in the meantime.
/// </summary>
public class DeviceLostException : Exception
{
    public DeviceLostException()
        : base("The graphics device has been lost.")
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

/// <summary>Matches real XNA's <c>DeviceNotResetException</c>: the device was lost and has not been
/// reset yet. See <see cref="DeviceLostException"/>.</summary>
public class DeviceNotResetException : Exception
{
    public DeviceNotResetException()
        : base("The graphics device has been lost and has not been reset.")
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

/// <summary>Matches real XNA's <c>NoSuitableGraphicsDeviceException</c>: no device meeting the
/// requested <see cref="GraphicsProfile"/> and presentation parameters could be created. See
/// <see cref="DeviceLostException"/>.</summary>
public class NoSuitableGraphicsDeviceException : Exception
{
    public NoSuitableGraphicsDeviceException()
        : base("No suitable graphics device could be created.")
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
