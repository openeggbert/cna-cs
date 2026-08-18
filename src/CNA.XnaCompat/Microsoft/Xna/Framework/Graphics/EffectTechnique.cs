namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectTechnique</c>. A thin re-typing wrapper -- see
/// <see cref="EffectParameter"/> for why these reflection types wrap rather than
/// subclass.</summary>
public class EffectTechnique
{
    internal EffectTechnique(CNA.Graphics.EffectTechnique technique)
    {
        Framework = technique;
    }

    internal CNA.Graphics.EffectTechnique Framework { get; }

    public string Name => Framework.Name;

    public EffectPassCollection Passes => new(Framework.Passes);

    public EffectAnnotationCollection Annotations => new(Framework.Annotations);
}
