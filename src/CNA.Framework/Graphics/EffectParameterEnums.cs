namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>EffectParameterClass</c> values exactly -- also confirmed
/// against the real, shipped openeggbert/cna C API's own <c>CNA_EFFECT_PARAMETER_CLASS_*</c>
/// constants (<c>effects.h</c>).</summary>
public enum EffectParameterClass
{
    Scalar = 0,
    Vector = 1,
    Matrix = 2,
    Object = 3,
    Struct = 4,
}

/// <summary>Matches real XNA's <c>EffectParameterType</c> values exactly -- also confirmed against
/// the real, shipped openeggbert/cna C API's own <c>CNA_EFFECT_PARAMETER_TYPE_*</c> constants
/// (<c>effects.h</c>).</summary>
public enum EffectParameterType
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
