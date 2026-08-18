namespace CNA.Storage;

/// <summary>Matches real XNA's <c>StorageDeviceNotConnectedException</c>: the selected
/// <see cref="StorageDevice"/> is no longer connected. <see cref="StorageDevice.IsConnected"/> is
/// how a caller checks before acting; this type exists for XNA source that catches
/// instead.</summary>
public class StorageDeviceNotConnectedException : Exception
{
    public StorageDeviceNotConnectedException()
        : base("The storage device is not connected.")
    {
    }

    public StorageDeviceNotConnectedException(string message)
        : base(message)
    {
    }

    public StorageDeviceNotConnectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
