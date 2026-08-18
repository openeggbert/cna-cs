namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectPass</c>. A thin re-typing wrapper -- see
/// <see cref="EffectParameter"/>.</summary>
public class EffectPass
{
    private readonly CNA.Graphics.EffectPass _pass;

    internal EffectPass(CNA.Graphics.EffectPass pass)
    {
        _pass = pass;
    }

    public string Name => _pass.Name;

    public EffectAnnotationCollection Annotations => new(_pass.Annotations);

    public void Apply() => _pass.Apply();
}
