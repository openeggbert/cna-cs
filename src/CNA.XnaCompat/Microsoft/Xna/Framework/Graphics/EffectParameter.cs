namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>EffectParameter</c>.
///
/// A thin re-typing wrapper, not a subclass: <see cref="CNA.Graphics.EffectParameter"/>'s only
/// constructor is internal (it wraps a borrowed native handle), and the collections that produce
/// parameters are native-backed views that build the base type. Wrapping keeps the compat surface
/// honest without duplicating any of the value marshalling.
/// </summary>
public class EffectParameter
{
    private readonly CNA.Graphics.EffectParameter _parameter;

    internal EffectParameter(CNA.Graphics.EffectParameter parameter)
    {
        _parameter = parameter;
    }

    public string Name => _parameter.Name;

    public string Semantic => _parameter.Semantic;

    public int RowCount => _parameter.RowCount;

    public int ColumnCount => _parameter.ColumnCount;

    public EffectParameterClass ParameterClass => (EffectParameterClass)(int)_parameter.ParameterClass;

    public EffectParameterType ParameterType => (EffectParameterType)(int)_parameter.ParameterType;

    public EffectParameterCollection Elements => new(_parameter.Elements);

    public EffectParameterCollection StructureMembers => new(_parameter.StructureMembers);

    public EffectAnnotationCollection Annotations => new(_parameter.Annotations);

    public bool GetValueBoolean() => _parameter.GetValueBoolean();

    public int GetValueInt32() => _parameter.GetValueInt32();

    public float GetValueSingle() => _parameter.GetValueSingle();

    public string GetValueString() => _parameter.GetValueString();

    public Matrix GetValueMatrix() => _parameter.GetValueMatrix();

    public Quaternion GetValueQuaternion() => _parameter.GetValueQuaternion();

    public Vector2 GetValueVector2() => _parameter.GetValueVector2();

    public Vector3 GetValueVector3() => _parameter.GetValueVector3();

    public Vector4 GetValueVector4() => _parameter.GetValueVector4();

    public Matrix[] GetValueMatrixArray(int count) => Convert(_parameter.GetValueMatrixArray(count), static m => (Matrix)m);

    public Vector2[] GetValueVector2Array(int count) => Convert(_parameter.GetValueVector2Array(count), static v => (Vector2)v);

    public Vector3[] GetValueVector3Array(int count) => Convert(_parameter.GetValueVector3Array(count), static v => (Vector3)v);

    public Vector4[] GetValueVector4Array(int count) => Convert(_parameter.GetValueVector4Array(count), static v => (Vector4)v);

    public float[] GetValueSingleArray(int count) => _parameter.GetValueSingleArray(count);

    public int[] GetValueInt32Array(int count) => _parameter.GetValueInt32Array(count);

    public void SetValue(bool value) => _parameter.SetValue(value);

    public void SetValue(int value) => _parameter.SetValue(value);

    public void SetValue(float value) => _parameter.SetValue(value);

    public void SetValue(string value) => _parameter.SetValue(value);

    public void SetValue(Matrix value) => _parameter.SetValue(value);

    public void SetValue(Quaternion value) => _parameter.SetValue(value);

    public void SetValue(Vector2 value) => _parameter.SetValue(value);

    public void SetValue(Vector3 value) => _parameter.SetValue(value);

    public void SetValue(Vector4 value) => _parameter.SetValue(value);

    public void SetValue(Texture? value) => _parameter.SetValue(value?.FrameworkTexture);

    public void SetValue(float[] value) => _parameter.SetValue(value);

    public void SetValue(int[] value) => _parameter.SetValue(value);

    public void SetValue(Matrix[] value) => _parameter.SetValue(Convert(value, static m => (CNA.Matrix)m));

    public void SetValue(Vector2[] value) => _parameter.SetValue(Convert(value, static v => (CNA.Vector2)v));

    public void SetValue(Vector3[] value) => _parameter.SetValue(Convert(value, static v => (CNA.Vector3)v));

    public void SetValue(Vector4[] value) => _parameter.SetValue(Convert(value, static v => (CNA.Vector4)v));

    /// <summary>Element-wise conversion, not collection-level -- the value types convert
    /// implicitly per element but arrays of them do not, the same limitation
    /// <c>ContentManager</c>'s own converters document.</summary>
    private static TOut[] Convert<TIn, TOut>(TIn[] source, Func<TIn, TOut> convert)
    {
        ArgumentNullException.ThrowIfNull(source);

        var result = new TOut[source.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = convert(source[i]);
        }

        return result;
    }

    /// <summary>Re-typed: <c>Texture2D</c> is a separate class per namespace. Detaches the handle
    /// from the framework wrapper rather than sharing it -- two owners of one texture handle is a
    /// double-free.</summary>
    public Texture2D? GetValueTexture2D()
    {
        using CNA.Graphics.Texture2D? texture = _parameter.GetValueTexture2D();
        return texture is null ? null : new Texture2D(GraphicsDeviceOf(texture), texture.DetachNativeHandle());
    }

    /// <summary>See <see cref="GetValueTexture2D"/>.</summary>
    public Texture3D? GetValueTexture3D()
    {
        using CNA.Graphics.Texture3D? texture = _parameter.GetValueTexture3D();
        return texture is null ? null : new Texture3D(GraphicsDeviceOf(texture), texture.DetachNativeHandle());
    }

    /// <summary>See <see cref="GetValueTexture2D"/>.</summary>
    public TextureCube? GetValueTextureCube()
    {
        using CNA.Graphics.TextureCube? texture = _parameter.GetValueTextureCube();
        return texture is null ? null : new TextureCube(GraphicsDeviceOf(texture), texture.DetachNativeHandle());
    }

    /// <summary>The compat device the framework texture was built against. It is compat-typed for
    /// every reachable instance -- the parameter's device is threaded down from the compat
    /// <c>Effect</c> that created it -- but the cast is checked rather than assumed, because
    /// <c>GraphicsResource.GraphicsDevice</c> is public and this class cannot prove otherwise.</summary>
    private static GraphicsDevice GraphicsDeviceOf(CNA.Graphics.Texture texture) =>
        texture.GraphicsDevice as GraphicsDevice
        ?? throw new InvalidOperationException(
            "This effect parameter's texture belongs to a CNA.Graphics.GraphicsDevice rather than a " +
            "compat one, so it cannot be re-typed into this namespace.");
}
