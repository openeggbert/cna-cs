using CNA.Interop;

namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>EffectAnnotation</c>: a compile-time metadata value attached to
/// a parameter, technique or pass. Read-only by definition -- annotations are baked into the
/// effect, which is why every accessor here is a getter. A borrowed handle, same ownership rule as
/// <see cref="EffectParameter"/>.</summary>
public class EffectAnnotation
{
    private readonly CnaHandle _handle;

    internal EffectAnnotation(CnaHandle handle)
    {
        _handle = handle;
    }

    public unsafe string Name => NativeStringReader.Read(
        Native.cna_effect_annotation_get_name_byte_count, Native.cna_effect_annotation_copy_name, _handle, nameof(Name));

    public unsafe string Semantic => NativeStringReader.Read(
        Native.cna_effect_annotation_get_semantic_byte_count, Native.cna_effect_annotation_copy_semantic, _handle, nameof(Semantic));

    public int RowCount => GetInfo().RowCount;

    public int ColumnCount => GetInfo().ColumnCount;

    public EffectParameterClass ParameterClass => (EffectParameterClass)GetInfo().ParameterClass;

    public EffectParameterType ParameterType => (EffectParameterType)GetInfo().ParameterType;

    public bool GetValueBoolean()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_boolean(_handle, out byte value);
        CnaException.ThrowIfFailed(result, nameof(GetValueBoolean));
        return value != 0;
    }

    public int GetValueInt32()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_int32(_handle, out int value);
        CnaException.ThrowIfFailed(result, nameof(GetValueInt32));
        return value;
    }

    public float GetValueSingle()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_single(_handle, out float value);
        CnaException.ThrowIfFailed(result, nameof(GetValueSingle));
        return value;
    }

    public Matrix GetValueMatrix()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_matrix(_handle, out CnaMatrix value);
        CnaException.ThrowIfFailed(result, nameof(GetValueMatrix));
        return Matrix.FromNative(value);
    }

    public Vector2 GetValueVector2()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_vector2(_handle, out CnaVector2 value);
        CnaException.ThrowIfFailed(result, nameof(GetValueVector2));
        return Vector2.FromNative(value);
    }

    public Vector3 GetValueVector3()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_vector3(_handle, out CnaVector3 value);
        CnaException.ThrowIfFailed(result, nameof(GetValueVector3));
        return Vector3.FromNative(value);
    }

    public Vector4 GetValueVector4()
    {
        CnaResult result = Native.cna_effect_annotation_get_value_vector4(_handle, out CnaVector4 value);
        CnaException.ThrowIfFailed(result, nameof(GetValueVector4));
        return Vector4.FromNative(value);
    }

    public unsafe string GetValueString() => NativeStringReader.Read(
        Native.cna_effect_annotation_get_value_string_byte_count,
        Native.cna_effect_annotation_copy_value_string,
        _handle,
        nameof(GetValueString));

    private CnaEffectAnnotationInfo GetInfo()
    {
        var info = new CnaEffectAnnotationInfo();
        CnaResult result = Native.cna_effect_annotation_get_info(_handle, ref info);
        CnaException.ThrowIfFailed(result, "cna_effect_annotation_get_info");
        return info;
    }
}
