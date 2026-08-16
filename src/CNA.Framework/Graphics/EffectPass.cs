namespace CNA.Graphics;

/// <summary>A single rendering pass within an <see cref="EffectTechnique"/>. This project's stock
/// effects only ever have exactly one pass (real multi-pass custom-shader effects aren't
/// implemented -- see <see cref="Effect"/>'s doc comment).</summary>
public class EffectPass
{
    private readonly Effect _effect;

    internal EffectPass(Effect effect)
    {
        _effect = effect;
    }

    public string Name => "Pass0";

    public void Apply() => _effect.Apply();
}
