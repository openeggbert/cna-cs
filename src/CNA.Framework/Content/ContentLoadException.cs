namespace CNA.Content;

/// <summary>Matches real XNA's <c>Microsoft.Xna.Framework.Content.ContentLoadException</c> exactly:
/// thrown by <see cref="ContentManager.Load{T}"/> and the real-<c>.xnb</c>-format reader
/// (<c>CNA.Content.Xnb</c>) for any content-loading failure -- a missing file, a corrupt/truncated
/// container, an unsupported compression scheme, or a malformed object graph.</summary>
public class ContentLoadException : Exception
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
