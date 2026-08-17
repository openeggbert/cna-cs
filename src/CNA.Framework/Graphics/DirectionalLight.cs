using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// One directional light slot on a <see cref="BasicEffect"/>. Matches real XNA's
/// <c>DirectionalLight</c>'s public surface exactly, including its constructor being non-public --
/// only <see cref="BasicEffect"/> constructs these. Native-handle-backed now that the real,
/// shipped openeggbert/cna C API confirms directional lights are real objects
/// (<c>cna_directional_light_*</c>, <c>effects.h</c>) rather than the plain client-side data this
/// project originally guessed. Every property is a real, immediate native round trip -- confirmed
/// directly against <c>BasicEffectSmoke.c</c> that a light handle fetched via
/// <c>cna_effect_lights_get_directional_light</c> is independently owned (it stays valid even
/// after the parent effect is destroyed) and must be released with its own
/// <c>cna_directional_light_destroy</c> call -- <see cref="BasicEffect"/> fetches all three once,
/// at construction, and destroys them in its own <c>Dispose</c>.
/// </summary>
public class DirectionalLight
{
    private readonly CnaHandle _handle;

    internal DirectionalLight(CnaHandle handle)
    {
        _handle = handle;
    }

    internal CnaHandle NativeHandle => _handle;

    public Vector3 Direction
    {
        get
        {
            CnaResult result = Native.cna_directional_light_get_direction(_handle, out CnaVector3 value);
            CnaException.ThrowIfFailed(result, nameof(Direction));
            return Vector3.FromNative(value);
        }
        set
        {
            CnaResult result = Native.cna_directional_light_set_direction(_handle, value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(Direction));
        }
    }

    public Vector3 DiffuseColor
    {
        get
        {
            CnaResult result = Native.cna_directional_light_get_diffuse_color(_handle, out CnaVector3 value);
            CnaException.ThrowIfFailed(result, nameof(DiffuseColor));
            return Vector3.FromNative(value);
        }
        set
        {
            CnaResult result = Native.cna_directional_light_set_diffuse_color(_handle, value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(DiffuseColor));
        }
    }

    public Vector3 SpecularColor
    {
        get
        {
            CnaResult result = Native.cna_directional_light_get_specular_color(_handle, out CnaVector3 value);
            CnaException.ThrowIfFailed(result, nameof(SpecularColor));
            return Vector3.FromNative(value);
        }
        set
        {
            CnaResult result = Native.cna_directional_light_set_specular_color(_handle, value.ToNative());
            CnaException.ThrowIfFailed(result, nameof(SpecularColor));
        }
    }

    public bool Enabled
    {
        get
        {
            CnaResult result = Native.cna_directional_light_get_enabled(_handle, out byte value);
            CnaException.ThrowIfFailed(result, nameof(Enabled));
            return value != 0;
        }
        set
        {
            CnaResult result = Native.cna_directional_light_set_enabled(_handle, value ? (byte)1 : (byte)0);
            CnaException.ThrowIfFailed(result, nameof(Enabled));
        }
    }
}
