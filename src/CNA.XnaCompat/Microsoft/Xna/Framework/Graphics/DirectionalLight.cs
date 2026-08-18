namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>DirectionalLight</c>.
///
/// A thin re-typing wrapper rather than a subclass: <see cref="CNA.Graphics.DirectionalLight"/>'s
/// only constructor is internal (it wraps a handle the owning effect fetched), and the effects
/// construct their three lights inside their own constructors with no seam for a compat subclass
/// to intervene -- the exact situation <c>BasicEffect</c>'s doc comment describes. Wrapping the
/// already-constructed object sidesteps that entirely: there is one native light, one
/// <c>CNA.Graphics.DirectionalLight</c>, and this view over it, so no two pieces of mutable state
/// can desync.
/// </summary>
public class DirectionalLight
{
    private readonly CNA.Graphics.DirectionalLight _light;

    internal DirectionalLight(CNA.Graphics.DirectionalLight light)
    {
        _light = light;
    }

    public Vector3 Direction
    {
        get => _light.Direction;
        set => _light.Direction = value;
    }

    public Vector3 DiffuseColor
    {
        get => _light.DiffuseColor;
        set => _light.DiffuseColor = value;
    }

    public Vector3 SpecularColor
    {
        get => _light.SpecularColor;
        set => _light.SpecularColor = value;
    }

    public bool Enabled
    {
        get => _light.Enabled;
        set => _light.Enabled = value;
    }
}
