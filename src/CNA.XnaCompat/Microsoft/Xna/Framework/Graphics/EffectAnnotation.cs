namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectAnnotation</c>. A thin re-typing wrapper for the same
/// reason <see cref="EffectParameter"/> is one.</summary>
public sealed class EffectAnnotation
{
    private readonly CNA.Graphics.EffectAnnotation _annotation;

    internal EffectAnnotation(CNA.Graphics.EffectAnnotation annotation)
    {
        _annotation = annotation;
    }

    public string Name => _annotation.Name;

    public string Semantic => _annotation.Semantic;

    public int RowCount => _annotation.RowCount;

    public int ColumnCount => _annotation.ColumnCount;

    public EffectParameterClass ParameterClass => (EffectParameterClass)(int)_annotation.ParameterClass;

    public EffectParameterType ParameterType => (EffectParameterType)(int)_annotation.ParameterType;

    public bool GetValueBoolean() => _annotation.GetValueBoolean();

    public int GetValueInt32() => _annotation.GetValueInt32();

    public float GetValueSingle() => _annotation.GetValueSingle();

    public string GetValueString() => _annotation.GetValueString();

    public Matrix GetValueMatrix() => _annotation.GetValueMatrix().ToCompat();

    public Vector2 GetValueVector2() => _annotation.GetValueVector2().ToCompat();

    public Vector3 GetValueVector3() => _annotation.GetValueVector3().ToCompat();

    public Vector4 GetValueVector4() => _annotation.GetValueVector4().ToCompat();
}
