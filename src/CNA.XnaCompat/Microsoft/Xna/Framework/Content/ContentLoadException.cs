using System.Runtime.Serialization;

namespace Microsoft.Xna.Framework.Content;

[Serializable]
public class ContentLoadException : Exception
{
    public ContentLoadException()
    {
    }

    public ContentLoadException(string message)
        : base(message)
    {
    }

    public ContentLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

#pragma warning disable SYSLIB0051 // Required by the XNA 4.0 serializable exception contract.
    protected ContentLoadException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
#pragma warning restore SYSLIB0051
}
