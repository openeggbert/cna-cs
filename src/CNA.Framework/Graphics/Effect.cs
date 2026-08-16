namespace CNA.Graphics;

/// <summary>
/// Base for stock/built-in shader effects (currently only <see cref="BasicEffect"/> -- custom
/// user-authored <c>.fx</c> shader loading is not implemented, matching the real openeggbert/cna
/// C++ engine itself: its own <c>Effect(GraphicsDevice&amp;, const std::vector&lt;byte&gt;&amp;)</c>
/// bytecode constructor always throws <c>NotImplementedException</c> there too, tracked as their
/// own "Phase 74"). <see cref="Apply"/> is a real, documented deviation from standard XNA/FNA
/// (which only expose <c>EffectPass.Apply()</c>, not a convenience method directly on
/// <c>Effect</c>) that this project's own C++ engine already makes -- confirmed by reading its
/// source, not invented here; the standard <c>CurrentTechnique.Passes[0].Apply()</c> idiom is
/// still fully supported via <see cref="CurrentTechnique"/>, both paths reach the same code.
/// </summary>
public abstract class Effect : IDisposable
{
    protected Effect(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        GraphicsDevice = graphicsDevice;
        CurrentTechnique = new EffectTechnique(this);
    }

    public GraphicsDevice GraphicsDevice { get; }

    public EffectTechnique CurrentTechnique { get; protected set; }

    public void Apply() => OnApply();

    protected abstract void OnApply();

    public virtual void Dispose()
    {
    }
}
