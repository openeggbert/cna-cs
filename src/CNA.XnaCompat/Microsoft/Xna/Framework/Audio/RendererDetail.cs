namespace Microsoft.Xna.Framework.Audio;

/// <summary>Describes an audio renderer.</summary>
public struct RendererDetail
{
    private readonly string? _name;
    private readonly string? _id;

    internal RendererDetail(CNA.Audio.RendererDetail source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _name = source.FriendlyName;
        _id = source.RendererId;
    }

    public string FriendlyName => _name!;

    public string RendererId => _id!;

    public override int GetHashCode() => (_name?.GetHashCode() ?? 0) ^ (_id?.GetHashCode() ?? 0);

    public override string ToString() => base.ToString()!;

    public static bool operator ==(RendererDetail left, RendererDetail right) =>
        left._name == right._name && left._id == right._id;

    public static bool operator !=(RendererDetail left, RendererDetail right) => !(left == right);

    public override bool Equals(object? obj) => obj is RendererDetail other && this == other;
}
