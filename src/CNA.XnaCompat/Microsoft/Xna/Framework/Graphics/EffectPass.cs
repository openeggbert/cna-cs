namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectPass</c>. A thin re-typing wrapper -- see
/// <see cref="EffectParameter"/>.</summary>
public sealed class EffectPass
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
        CNA.Graphics.EffectPass,
        EffectPass> FrameworkFacades = new();

    private readonly CNA.Graphics.EffectPass _pass;
    private EffectAnnotationCollection? _annotations;

    private EffectPass(CNA.Graphics.EffectPass pass)
    {
        _pass = pass;
    }

    internal static EffectPass Wrap(CNA.Graphics.EffectPass pass) =>
        FrameworkFacades.GetValue(pass, static value => new EffectPass(value));

    public string Name => _pass.Name;

    public EffectAnnotationCollection Annotations => _annotations ??= new(_pass.Annotations);

    public void Apply() => _pass.Apply();
}
