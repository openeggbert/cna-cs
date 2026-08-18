namespace Microsoft.Xna.Framework.Graphics;

/// <summary>See CNA.Graphics.EffectParameterClass; values kept numerically identical to it.</summary>
public enum EffectParameterClass
{
    Scalar = 0,
    Vector = 1,
    Matrix = 2,
    Object = 3,
    Struct = 4,
}

/// <summary>See CNA.Graphics.EffectParameterType; values kept numerically identical to it.</summary>
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
