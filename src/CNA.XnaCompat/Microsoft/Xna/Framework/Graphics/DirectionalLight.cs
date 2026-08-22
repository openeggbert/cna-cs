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
public sealed class DirectionalLight
{
    private readonly CNA.Graphics.DirectionalLight? _light;
    private Vector3 _direction;
    private Vector3 _diffuseColor;
    private Vector3 _specularColor;
    private bool _enabled;

    internal DirectionalLight(CNA.Graphics.DirectionalLight light)
    {
        _light = light;
    }

    public DirectionalLight(
        EffectParameter directionParameter,
        EffectParameter diffuseColorParameter,
        EffectParameter specularColorParameter,
        DirectionalLight cloneSource)
    {
        ArgumentNullException.ThrowIfNull(directionParameter);
        ArgumentNullException.ThrowIfNull(diffuseColorParameter);
        ArgumentNullException.ThrowIfNull(specularColorParameter);
        ArgumentNullException.ThrowIfNull(cloneSource);
        _direction = cloneSource.Direction;
        _diffuseColor = cloneSource.DiffuseColor;
        _specularColor = cloneSource.SpecularColor;
        _enabled = cloneSource.Enabled;
    }

    public Vector3 Direction
    {
        get => _light is null ? _direction : _light.Direction.ToCompat();
        set
        {
            if (_light is null) _direction = value;
            else _light.Direction = value.ToFramework();
        }
    }

    public Vector3 DiffuseColor
    {
        get => _light is null ? _diffuseColor : _light.DiffuseColor.ToCompat();
        set
        {
            if (_light is null) _diffuseColor = value;
            else _light.DiffuseColor = value.ToFramework();
        }
    }

    public Vector3 SpecularColor
    {
        get => _light is null ? _specularColor : _light.SpecularColor.ToCompat();
        set
        {
            if (_light is null) _specularColor = value;
            else _light.SpecularColor = value.ToFramework();
        }
    }

    public bool Enabled
    {
        get => _light is null ? _enabled : _light.Enabled;
        set
        {
            if (_light is null) _enabled = value;
            else _light.Enabled = value;
        }
    }
}
