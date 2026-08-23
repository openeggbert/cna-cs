namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectTechnique</c>. A thin re-typing wrapper -- see
/// <see cref="EffectParameter"/> for why these reflection types wrap rather than
/// subclass.</summary>
public sealed class EffectTechnique
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
        CNA.Graphics.EffectTechnique,
        EffectTechnique> FrameworkFacades = new();

    private EffectPassCollection? _passes;
    private EffectAnnotationCollection? _annotations;

    private EffectTechnique(CNA.Graphics.EffectTechnique technique)
    {
        Framework = technique;
    }

    internal static EffectTechnique Wrap(CNA.Graphics.EffectTechnique technique) =>
        FrameworkFacades.GetValue(technique, static value => new EffectTechnique(value));

    internal CNA.Graphics.EffectTechnique Framework { get; }

    public string Name => Framework.Name;

    public EffectPassCollection Passes => _passes ??= new(Framework.Passes);

    public EffectAnnotationCollection Annotations => _annotations ??= new(Framework.Annotations);
}
