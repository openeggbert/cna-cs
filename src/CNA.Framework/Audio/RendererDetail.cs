using CNA.Interop;

namespace CNA.Audio;

/// <summary>
/// Matches real XNA's <c>RendererDetail</c>: one audio renderer an <see cref="AudioEngine"/> can
/// open, as listed by <see cref="AudioEngine.RendererDetails"/>.
///
/// Immutable and read at construction rather than a live view, because the ABI addresses a detail
/// by index into a point-in-time list -- holding an index across a device change would silently
/// answer about a different renderer. Both strings are read once, up front, which is also what the
/// canonical value type is.
///
/// Was missing until the WP16 re-audit, along with <see cref="AudioEngine.RendererDetails"/>.
/// </summary>
public class RendererDetail : IEquatable<RendererDetail>
{
    internal RendererDetail(string friendlyName, string rendererId, int hashCode)
    {
        FriendlyName = friendlyName;
        RendererId = rendererId;
        _hashCode = hashCode;
    }

    private readonly int _hashCode;

    public string FriendlyName { get; }

    public string RendererId { get; }

    /// <summary>By <see cref="RendererId"/>, matching the canonical rule -- the friendly name is a
    /// display string and two renderers may share one.</summary>
    public bool Equals(RendererDetail? other) => other is not null && RendererId == other.RendererId;

    public override bool Equals(object? obj) => Equals(obj as RendererDetail);

    /// <summary>Native's own hash, so it stays consistent with the identity native compares
    /// on.</summary>
    public override int GetHashCode() => _hashCode;

    public override string ToString() => FriendlyName;

    public static bool operator ==(RendererDetail? left, RendererDetail? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(RendererDetail? left, RendererDetail? right) => !(left == right);
}
