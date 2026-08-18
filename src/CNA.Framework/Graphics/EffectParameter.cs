using CNA;
using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>EffectParameter</c>: one named, typed input of an <see cref="Effect"/>,
/// reachable through <see cref="Effect.Parameters"/>.
///
/// A borrowed handle, not an owned resource: the C API hands parameters out from a collection the
/// effect owns, and only explicitly-created ones (<c>cna_effect_parameter_create</c>, which this
/// project never calls) need destroying. So this type has no <see cref="IDisposable"/> and no
/// finalizer -- it is a view, valid as long as its effect is.
///
/// The C API models every scalar/vector/matrix read and write through one
/// <c>get_value</c>/<c>set_value</c> pair discriminated by a value-type tag, with the caller
/// supplying correctly-sized storage. The typed methods below are that contract made safe: each
/// pins storage of exactly the right size for the tag it passes.
/// </summary>
public class EffectParameter : IDisposable
{
    private readonly NativeResourceHandle _ownedHandle;

    internal EffectParameter(CnaHandle handle)
    {
        _ownedHandle = new NativeResourceHandle(handle.AsNint, h => Native.cna_effect_parameter_destroy(new CnaHandle(h)));
    }

    private CnaHandle _handle => new(_ownedHandle.DangerousGetHandle());

    public void Dispose()
    {
        _ownedHandle.Dispose();
        GC.SuppressFinalize(this);
    }

    public unsafe string Name => NativeStringReader.Read(
        Native.cna_effect_parameter_get_name_byte_count, Native.cna_effect_parameter_copy_name, _handle, nameof(Name));

    public unsafe string Semantic => NativeStringReader.Read(
        Native.cna_effect_parameter_get_semantic_byte_count, Native.cna_effect_parameter_copy_semantic, _handle, nameof(Semantic));

    public int RowCount => GetInfo().RowCount;

    public int ColumnCount => GetInfo().ColumnCount;

    public EffectParameterClass ParameterClass => (EffectParameterClass)GetInfo().ParameterClass;

    public EffectParameterType ParameterType => (EffectParameterType)GetInfo().ParameterType;

    /// <summary>The per-element parameters when this one is an array; empty otherwise.</summary>
    public EffectParameterCollection Elements
    {
        get
        {
            CnaResult result = Native.cna_effect_parameter_get_elements(_handle, out CnaHandle collection);
            CnaException.ThrowIfFailed(result, nameof(Elements));
            return new EffectParameterCollection(collection);
        }
    }

    /// <summary>The member parameters when this one is a struct; empty otherwise.</summary>
    public EffectParameterCollection StructureMembers
    {
        get
        {
            CnaResult result = Native.cna_effect_parameter_get_structure_members(_handle, out CnaHandle collection);
            CnaException.ThrowIfFailed(result, nameof(StructureMembers));
            return new EffectParameterCollection(collection);
        }
    }

    public EffectAnnotationCollection Annotations
    {
        get
        {
            CnaResult result = Native.cna_effect_parameter_get_annotations(_handle, out CnaHandle collection);
            CnaException.ThrowIfFailed(result, nameof(Annotations));
            return new EffectAnnotationCollection(collection);
        }
    }

    public unsafe bool GetValueBoolean() => GetScalar<byte>(CnaEffectValueType.Boolean) != 0;

    public unsafe int GetValueInt32() => GetScalar<int>(CnaEffectValueType.Int32);

    public unsafe float GetValueSingle() => GetScalar<float>(CnaEffectValueType.Single);

    public unsafe Matrix GetValueMatrix() => Matrix.FromNative(GetScalar<CnaMatrix>(CnaEffectValueType.Matrix));

    public unsafe Quaternion GetValueQuaternion() => Quaternion.FromNative(GetScalar<CnaQuaternion>(CnaEffectValueType.Quaternion));

    public unsafe Vector2 GetValueVector2() => Vector2.FromNative(GetScalar<CnaVector2>(CnaEffectValueType.Vector2));

    public unsafe Vector3 GetValueVector3() => Vector3.FromNative(GetScalar<CnaVector3>(CnaEffectValueType.Vector3));

    public unsafe Vector4 GetValueVector4() => Vector4.FromNative(GetScalar<CnaVector4>(CnaEffectValueType.Vector4));

    public unsafe string GetValueString() => NativeStringReader.Read(
        Native.cna_effect_parameter_get_value_string_byte_count,
        Native.cna_effect_parameter_copy_value_string,
        _handle,
        nameof(GetValueString));

    public unsafe void SetValue(bool value) => SetScalar(CnaEffectValueType.Boolean, (byte)(value ? 1 : 0));

    public unsafe void SetValue(int value) => SetScalar(CnaEffectValueType.Int32, value);

    public unsafe void SetValue(float value) => SetScalar(CnaEffectValueType.Single, value);

    public unsafe void SetValue(Matrix value) => SetScalar(CnaEffectValueType.Matrix, value.ToNative());

    public unsafe void SetValue(Quaternion value) => SetScalar(CnaEffectValueType.Quaternion, value.ToNative());

    public unsafe void SetValue(Vector2 value) => SetScalar(CnaEffectValueType.Vector2, value.ToNative());

    public unsafe void SetValue(Vector3 value) => SetScalar(CnaEffectValueType.Vector3, value.ToNative());

    public unsafe void SetValue(Vector4 value) => SetScalar(CnaEffectValueType.Vector4, value.ToNative());

    public void SetValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        CnaResult result = CnaStringMarshal.WithStringView(value, view => Native.cna_effect_parameter_set_value_string(_handle, view));
        CnaException.ThrowIfFailed(result, nameof(SetValue));
    }

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
/// <summary>Matches real XNA's <c>SetValue(Texture)</c>. The texture-type tag is derived from
    /// the managed type rather than asked for, so a caller cannot pair a
    /// <see cref="TextureCube"/> with the 2D tag.</summary>
    public void SetValue(Texture? value)
    {
        CnaEffectTextureType textureType = value switch
        {
            null => CnaEffectTextureType.Base,
            TextureCube => CnaEffectTextureType.TextureCube,
            Texture3D => CnaEffectTextureType.Texture3D,
            Texture2D => CnaEffectTextureType.Texture2D,
            _ => CnaEffectTextureType.Base,
        };

        CnaHandle handle = value is null ? CnaHandle.Zero : new CnaHandle(value.NativeHandleValue);
        CnaResult result = Native.cna_effect_parameter_set_value_texture(_handle, textureType, handle);
        CnaException.ThrowIfFailed(result, nameof(SetValue));
    }

    public unsafe void SetValue(Matrix[] value) => SetArray(CnaEffectValueType.Matrix, value, static m => m.ToNative());

    public unsafe void SetValue(Vector2[] value) => SetArray(CnaEffectValueType.Vector2, value, static v => v.ToNative());

    public unsafe void SetValue(Vector3[] value) => SetArray(CnaEffectValueType.Vector3, value, static v => v.ToNative());

    public unsafe void SetValue(Vector4[] value) => SetArray(CnaEffectValueType.Vector4, value, static v => v.ToNative());

    public unsafe void SetValue(float[] value) => SetArray(CnaEffectValueType.Single, value, static f => f);

    public unsafe void SetValue(int[] value) => SetArray(CnaEffectValueType.Int32, value, static i => i);

    public unsafe Matrix[] GetValueMatrixArray(int count) =>
        GetArray<CnaMatrix, Matrix>(CnaEffectValueType.Matrix, count, Matrix.FromNative);

    public unsafe Vector2[] GetValueVector2Array(int count) =>
        GetArray<CnaVector2, Vector2>(CnaEffectValueType.Vector2, count, Vector2.FromNative);

    public unsafe Vector3[] GetValueVector3Array(int count) =>
        GetArray<CnaVector3, Vector3>(CnaEffectValueType.Vector3, count, Vector3.FromNative);

    public unsafe Vector4[] GetValueVector4Array(int count) =>
        GetArray<CnaVector4, Vector4>(CnaEffectValueType.Vector4, count, Vector4.FromNative);

    public unsafe float[] GetValueSingleArray(int count) => GetArray<float, float>(CnaEffectValueType.Single, count, static f => f);

    public unsafe int[] GetValueInt32Array(int count) => GetArray<int, int>(CnaEffectValueType.Int32, count, static i => i);

    private CnaEffectParameterInfo GetInfo()
    {
        var info = new CnaEffectParameterInfo();
        CnaResult result = Native.cna_effect_parameter_get_info(_handle, ref info);
        CnaException.ThrowIfFailed(result, "cna_effect_parameter_get_info");
        return info;
    }

    private unsafe T GetScalar<T>(CnaEffectValueType valueType) where T : unmanaged
    {
        T value = default;
        CnaResult result = Native.cna_effect_parameter_get_value(_handle, valueType, &value);
        CnaException.ThrowIfFailed(result, "cna_effect_parameter_get_value");
        return value;
    }

    private unsafe void SetScalar<T>(CnaEffectValueType valueType, T value) where T : unmanaged
    {
        CnaResult result = Native.cna_effect_parameter_set_value(_handle, valueType, &value);
        CnaException.ThrowIfFailed(result, "cna_effect_parameter_set_value");
    }

    private unsafe void SetArray<TManaged, TNative>(CnaEffectValueType valueType, TManaged[] value, Func<TManaged, TNative> convert)
        where TNative : unmanaged
    {
        ArgumentNullException.ThrowIfNull(value);

        var native = new TNative[value.Length];
        for (int i = 0; i < native.Length; i++)
        {
            native[i] = convert(value[i]);
        }

        fixed (TNative* nativePtr = native)
        {
            CnaResult result = Native.cna_effect_parameter_set_values(_handle, valueType, nativePtr, (ulong)native.Length);
            CnaException.ThrowIfFailed(result, "cna_effect_parameter_set_values");
        }
    }

    /// <summary>Trims to what native actually wrote rather than to <paramref name="count"/> --
    /// <c>cna_effect_parameter_get_values</c> reports its own written count, which can be lower
    /// for a shorter parameter.</summary>
    private unsafe TManaged[] GetArray<TNative, TManaged>(CnaEffectValueType valueType, int count, Func<TNative, TManaged> convert)
        where TNative : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
        {
            return [];
        }

        var native = new TNative[count];
        fixed (TNative* nativePtr = native)
        {
            CnaResult result = Native.cna_effect_parameter_get_values(
                _handle, valueType, (ulong)count, nativePtr, (ulong)count, out ulong written);
            CnaException.ThrowIfFailed(result, "cna_effect_parameter_get_values");

            var managed = new TManaged[written];
            for (int i = 0; i < managed.Length; i++)
            {
                managed[i] = convert(native[i]);
            }

            return managed;
        }
    }
}
