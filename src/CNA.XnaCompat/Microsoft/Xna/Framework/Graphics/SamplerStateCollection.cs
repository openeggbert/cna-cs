namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>SamplerStateCollection</c>. Holds no state of its own -- the
/// base class reads and writes straight through to native; this only re-types what comes back,
/// through the base's own <c>Wrap</c> hook, and re-types the indexer so callers see this
/// namespace's <see cref="SamplerState"/>.</summary>
public class SamplerStateCollection : CNA.Graphics.SamplerStateCollection
{
    internal SamplerStateCollection(GraphicsDevice graphicsDevice, bool vertexStage)
        : base(graphicsDevice, vertexStage)
    {
    }

    public new SamplerState this[int index]
    {
        get => (SamplerState)base[index];
        set => base[index] = value;
    }

    protected override CNA.Graphics.SamplerState Wrap(CNA.Graphics.SamplerState state) => new SamplerState(state);
}
