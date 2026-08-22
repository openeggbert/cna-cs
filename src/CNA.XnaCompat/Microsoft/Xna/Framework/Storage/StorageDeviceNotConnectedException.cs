using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Microsoft.Xna.Framework.Storage;

[Serializable]
public class StorageDeviceNotConnectedException : ExternalException
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

#pragma warning disable SYSLIB0051 // Required by the XNA 4.0 serializable exception contract.
    protected StorageDeviceNotConnectedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
#pragma warning restore SYSLIB0051
}
