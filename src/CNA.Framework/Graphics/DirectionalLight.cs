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
/// <c>cna_directional_light_destroy</c> call.
///
/// Which is why the handle lives in a <see cref="NativeResourceHandle"/> here rather than as a bare
/// <see cref="CnaHandle"/>, since WP15. The owning effect fetches all three at construction and
/// disposes them from <c>StockEffect.ReleaseAdditionalNativeResources</c>; before this, an effect
/// that was never disposed reclaimed its own handle through that class's owned handle but leaked
/// all three lights for the process lifetime, because nothing else would ever call their destroy.
/// Now the critical finalizer covers them too, on exactly the same terms.
///
/// This type is deliberately still not <see cref="IDisposable"/>: real XNA's
/// <c>DirectionalLight</c> is not, and a light is not a resource a caller acquires -- it is a slot
/// on an effect that hands it out. Disposal stays the effect's job.
/// </summary>
public class DirectionalLight
{
    private readonly NativeResourceHandle _handle;

    internal DirectionalLight(CnaHandle handle)
    {
        _handle = new NativeResourceHandle(handle.AsNint, h => Native.cna_directional_light_destroy(new CnaHandle(h)).IsSuccess());
    }

    /// <summary>Read out of the owning <see cref="NativeResourceHandle"/>; every use below pairs it
    /// with <see cref="GC.KeepAlive(object)"/> so the critical finalizer cannot run
    /// <c>destroy</c> while the call is still in flight -- see <c>plan.md</c> WP17.</summary>
    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    /// <summary>Releases the light. Internal, and called only by the owning effect's disposal --
    /// see this class's own doc comment for why this is not a public <c>Dispose</c>.</summary>
    internal void ReleaseNative() => _handle.Dispose();

    public Vector3 Direction
    {
        get
        {
            CnaResult result = Native.cna_directional_light_get_direction(NativeHandle, out CnaVector3 value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Direction));
            return Vector3.FromNative(value);
        }
        set
        {
            CnaResult result = Native.cna_directional_light_set_direction(NativeHandle, value.ToNative());
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Direction));
        }
    }

    public Vector3 DiffuseColor
    {
        get
        {
            CnaResult result = Native.cna_directional_light_get_diffuse_color(NativeHandle, out CnaVector3 value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(DiffuseColor));
            return Vector3.FromNative(value);
        }
        set
        {
            CnaResult result = Native.cna_directional_light_set_diffuse_color(NativeHandle, value.ToNative());
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(DiffuseColor));
        }
    }

    public Vector3 SpecularColor
    {
        get
        {
            CnaResult result = Native.cna_directional_light_get_specular_color(NativeHandle, out CnaVector3 value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(SpecularColor));
            return Vector3.FromNative(value);
        }
        set
        {
            CnaResult result = Native.cna_directional_light_set_specular_color(NativeHandle, value.ToNative());
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(SpecularColor));
        }
    }

    public bool Enabled
    {
        get
        {
            CnaResult result = Native.cna_directional_light_get_enabled(NativeHandle, out byte value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Enabled));
            return value != 0;
        }
        set
        {
            CnaResult result = Native.cna_directional_light_set_enabled(NativeHandle, value ? (byte)1 : (byte)0);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(Enabled));
        }
    }
}
