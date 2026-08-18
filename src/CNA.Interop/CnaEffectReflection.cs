using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>Mirrors <c>CNA_EffectParameterClass</c> exactly (<c>effects.h</c>).</summary>
internal enum CnaEffectParameterClass : uint
{
    Scalar = 0,
    Vector = 1,
    Matrix = 2,
    Object = 3,
    Struct = 4,
}

/// <summary>Mirrors <c>CNA_EffectParameterType</c> exactly (<c>effects.h</c>).</summary>
internal enum CnaEffectParameterType : uint
{
    Void = 0,
    Bool = 1,
    Int32 = 2,
    Single = 3,
    String = 4,
    Texture = 5,
    Texture1D = 6,
    Texture2D = 7,
    Texture3D = 8,
    TextureCube = 9,
}

/// <summary>Mirrors <c>CNA_EffectValueType</c> exactly (<c>effects.h</c>) -- the discriminator the
/// single <c>cna_effect_parameter_get_value</c>/<c>_set_value</c> pair uses instead of one
/// function per type.</summary>
internal enum CnaEffectValueType : uint
{
    Boolean = 0,
    Int32 = 1,
    Single = 2,
    Matrix = 3,
    MatrixTranspose = 4,
    Quaternion = 5,
    Vector2 = 6,
    Vector3 = 7,
    Vector4 = 8,
}

/// <summary>Mirrors <c>CNA_EffectTextureType</c> exactly (<c>effects.h:452</c>).</summary>
internal enum CnaEffectTextureType : uint
{
    Base = 0,
    Texture2D = 1,
    Texture3D = 2,
    TextureCube = 3,
}

/// <summary>Mirrors <c>CNA_EffectParameterInfo</c> exactly (<c>effects.h</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaEffectParameterInfo
{
    public uint StructSize;
    public uint StructVersion;
    public int RowCount;
    public int ColumnCount;
    public CnaEffectParameterClass ParameterClass;
    public CnaEffectParameterType ParameterType;

    public unsafe CnaEffectParameterInfo()
    {
        StructSize = (uint)sizeof(CnaEffectParameterInfo);
        StructVersion = 1;
    }
}

/// <summary>Mirrors <c>CNA_EffectAnnotationInfo</c> exactly (<c>effects.h</c>) -- field-for-field
/// identical to <see cref="CnaEffectParameterInfo"/>, but kept as its own type because the two are
/// distinct in the C API and its accessors take one or the other.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaEffectAnnotationInfo
{
    public uint StructSize;
    public uint StructVersion;
    public int RowCount;
    public int ColumnCount;
    public CnaEffectParameterClass ParameterClass;
    public CnaEffectParameterType ParameterType;

    public unsafe CnaEffectAnnotationInfo()
    {
        StructSize = (uint)sizeof(CnaEffectAnnotationInfo);
        StructVersion = 1;
    }
}
