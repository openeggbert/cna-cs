namespace Microsoft.Xna.Framework.Storage;

/// <summary>XNA 4.0-compatible <c>StorageDeviceNotConnectedException</c>. Subclasses its
/// <c>CNA.Storage</c> counterpart -- see
/// <c>Microsoft.Xna.Framework.Graphics.DeviceLostException</c> for why.</summary>
public class StorageDeviceNotConnectedException : CNA.Storage.StorageDeviceNotConnectedException
{
    public StorageDeviceNotConnectedException()
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
