namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible <c>RendererDetail</c>. Subclasses its <c>CNA.Audio</c> counterpart,
/// which already carries everything -- there is no divergent type on it to re-type.</summary>
public class RendererDetail : CNA.Audio.RendererDetail
{
    internal RendererDetail(CNA.Audio.RendererDetail source)
        : base(source.FriendlyName, source.RendererId, source.GetHashCode())
    {
    }
}
