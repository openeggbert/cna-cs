namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0 blend-operation ordinals. Min/Max intentionally differ from CNA's native
/// representation and are translated at the facade boundary.</summary>
public enum BlendFunction
{
    Add = 0,
    Subtract = 1,
    ReverseSubtract = 2,
    Min = 3,
    Max = 4,
}

internal static class BlendFunctionConversions
{
    internal static BlendFunction ToCompat(this CNA.Graphics.BlendFunction value) => value switch
    {
        CNA.Graphics.BlendFunction.Min => BlendFunction.Min,
        CNA.Graphics.BlendFunction.Max => BlendFunction.Max,
        _ => (BlendFunction)(int)value,
    };

    internal static CNA.Graphics.BlendFunction ToFramework(this BlendFunction value) => value switch
    {
        BlendFunction.Min => CNA.Graphics.BlendFunction.Min,
        BlendFunction.Max => CNA.Graphics.BlendFunction.Max,
        _ => (CNA.Graphics.BlendFunction)(int)value,
    };
}
