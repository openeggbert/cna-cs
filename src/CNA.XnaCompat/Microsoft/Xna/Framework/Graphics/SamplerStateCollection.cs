namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>SamplerStateCollection</c>. Holds no state of its own -- the
/// base class reads and writes straight through to native; this only re-types what comes back,
/// through the base's own <c>Wrap</c> hook, and re-types the indexer so callers see this
/// namespace's <see cref="SamplerState"/>.</summary>
public class SamplerStateCollection : CNA.Graphics.SamplerStateCollection
{
    internal SamplerStateCollection(GraphicsDevice graphicsDevice, bool vertexStage)
        : base(graphicsDevice.Framework, vertexStage)
    {
    }

    public new SamplerState this[int index]
    {
        get => new(base[index]);
        set => base[index] = (value ?? throw new ArgumentNullException(nameof(value))).Framework;
    }
}
