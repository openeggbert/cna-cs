namespace CNA.Graphics;

public class EffectTechnique
{
    internal EffectTechnique(Effect effect)
    {
        Passes = new EffectPassCollection(new EffectPass(effect));
    }

    public string Name => "Default";

    public EffectPassCollection Passes { get; }
}
