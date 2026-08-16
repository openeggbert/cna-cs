namespace CNA.Graphics;

/// <summary>
/// One directional light slot on a <see cref="BasicEffect"/>. Matches real XNA's
/// <c>DirectionalLight</c> exactly, including its constructor being non-public -- confirmed
/// against the real openeggbert/cna C++ engine's own implementation, where only
/// <see cref="BasicEffect"/> constructs these (its three light members are created directly in
/// <c>BasicEffect</c>'s own constructor, not exposed as a general-purpose lighting type).
/// </summary>
public class DirectionalLight
{
    public Vector3 Direction { get; set; }

    public Vector3 DiffuseColor { get; set; }

    public Vector3 SpecularColor { get; set; }

    public bool Enabled { get; set; }

    internal DirectionalLight(Vector3 direction, Vector3 diffuseColor, Vector3 specularColor, bool enabled)
    {
        Direction = direction;
        DiffuseColor = diffuseColor;
        SpecularColor = specularColor;
        Enabled = enabled;
    }
}
