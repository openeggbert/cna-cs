using CNA;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Owns its native handle. <c>effects.h</c> documents every handle the reflection API hands out as
/// "Owned" -- a fresh registry slot per call, not an alias of something the effect owns -- and
/// declares a matching <c>destroy</c> for each. An earlier revision of this binding asserted the
/// opposite and destroyed none of them, which leaked on the per-frame
/// <c>ModelMesh.Draw</c> path (one technique + one pass collection + N passes per draw). Found by a
/// code-review pass.
///
/// Release runs through <see cref="NativeResourceHandle"/>, a <see cref="System.Runtime.InteropServices.SafeHandle"/>,
/// so the GC reclaims these even though real XNA's equivalents are not <see cref="IDisposable"/>
/// and callers never dispose them. <see cref="IDisposable"/> is offered too, for a caller that
/// wants the handle back promptly.
/// </summary>
/// <summary>Matches real XNA's <c>EffectAnnotation</c>: a compile-time metadata value attached to
/// a parameter, technique or pass. Read-only by definition -- annotations are baked into the
/// effect, which is why every accessor here is a getter. A borrowed handle, same ownership rule as
/// <see cref="EffectParameter"/>.</summary>
public class EffectAnnotation : IDisposable
{
    private readonly NativeResourceHandle _ownedHandle;

    internal EffectAnnotation(CnaHandle handle)
    {
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_annotation_destroy(new CnaHandle(h)));
    }

    /// <summary>
    /// The native handle, read out of the owning <see cref="NativeResourceHandle"/>.
    ///
    /// Every caller pairs this with <see cref="GC.KeepAlive(object)"/> after the native call. That
    /// is not decoration: these wrappers are routinely temporaries -- <c>effect.Parameters["World"]
    /// .SetValue(m)</c> leaves the <see cref="EffectParameter"/> unreachable the moment its handle
    /// has been read -- and the moment they are unreachable the <see cref="System.Runtime.InteropServices.SafeHandle"/>
    /// finalizer is free to run <c>destroy</c> while the native call is still in flight. Giving
    /// these types SafeHandle ownership is what fixed their leak; it is also what introduced this
    /// hazard, since before that they held a bare handle with no finalizer at all.
    ///
    /// <see cref="GC.KeepAlive(object)"/> rather than
    /// <see cref="System.Runtime.InteropServices.SafeHandle.DangerousAddRef"/>/<c>DangerousRelease</c>:
    /// it closes the reachability hazard, which is the real one here, but it does not make a
    /// concurrent <c>Dispose</c> from another thread safe. Nothing in this project is thread-safe,
    /// so that is consistent rather than a new gap -- and the ref-counted form is what
    /// <c>plan.md</c> WP17 will apply project-wide.
    /// </summary>
    private CnaHandle _handle => new(_ownedHandle.DangerousGetHandle());

    public void Dispose()
    {
        _ownedHandle.Dispose();
        GC.SuppressFinalize(this);
    }

    public unsafe string Name
    {
        get
        {
            string value = NativeStringReader.Read(
                Native.cna_effect_annotation_get_name_byte_count, Native.cna_effect_annotation_copy_name, _handle, nameof(Name));
            GC.KeepAlive(this);
            return value;
        }
    }

    public unsafe string Semantic
    {
        get
        {
            string value = NativeStringReader.Read(
                Native.cna_effect_annotation_get_semantic_byte_count, Native.cna_effect_annotation_copy_semantic, _handle, nameof(Semantic));
            GC.KeepAlive(this);
            return value;
        }
    }

    public int RowCount => GetInfo().RowCount;

    public int ColumnCount => GetInfo().ColumnCount;

    public EffectParameterClass ParameterClass => (EffectParameterClass)GetInfo().ParameterClass;

    public EffectParameterType ParameterType => (EffectParameterType)GetInfo().ParameterType;

    public bool GetValueBoolean()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_boolean(_handle, out byte value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetValueBoolean));
        return value != 0;
    }

    public int GetValueInt32()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_int32(_handle, out int value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetValueInt32));
        return value;
    }

    public float GetValueSingle()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_single(_handle, out float value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetValueSingle));
        return value;
    }

    public Matrix GetValueMatrix()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_matrix(_handle, out CnaMatrix value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetValueMatrix));
        return Matrix.FromNative(value);
    }

    public Vector2 GetValueVector2()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_vector2(_handle, out CnaVector2 value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetValueVector2));
        return Vector2.FromNative(value);
    }

    public Vector3 GetValueVector3()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_vector3(_handle, out CnaVector3 value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetValueVector3));
        return Vector3.FromNative(value);
    }

    public Vector4 GetValueVector4()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_vector4(_handle, out CnaVector4 value);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(GetValueVector4));
        return Vector4.FromNative(value);
    }

    public unsafe string GetValueString()
    {
        string value = NativeStringReader.Read(
            Native.cna_effect_annotation_get_value_string_byte_count,
            Native.cna_effect_annotation_copy_value_string,
            _handle,
            nameof(GetValueString));
        GC.KeepAlive(this);
        return value;
    }

    private CnaEffectAnnotationInfo GetInfo()
    {
        var info = new CnaEffectAnnotationInfo();
        CnaResult result = Native.cna_effect_annotation_get_info(_handle, ref info);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, "cna_effect_annotation_get_info");
        return info;
    }
}
