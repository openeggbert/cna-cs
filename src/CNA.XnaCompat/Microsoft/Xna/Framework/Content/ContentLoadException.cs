namespace Microsoft.Xna.Framework.Content;

/// <summary>XNA 4.0-compatible <c>ContentLoadException</c>. A pure subclass of
/// <see cref="CNA.Content.ContentLoadException"/>, so a <c>catch</c> written against either
/// namespace's type still catches what this namespace's <see cref="ContentManager"/> throws --
/// which matters here in a way it doesn't for the value types, since the base
/// <c>ContentManager</c>'s own loading code (reused unchanged by the compat subclass) is what
/// actually raises it, and it raises the base type.</summary>
public class ContentLoadException : CNA.Content.ContentLoadException
{
    public ContentLoadException(string message)
        : base(message)
    {
    }

    public ContentLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
