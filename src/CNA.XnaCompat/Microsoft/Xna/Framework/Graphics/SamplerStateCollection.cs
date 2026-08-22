namespace Microsoft.Xna.Framework.Graphics;

public sealed class SamplerStateCollection
{
    private readonly CNA.Graphics.SamplerStateCollection _collection;

    internal SamplerStateCollection(GraphicsDevice graphicsDevice, bool vertexStage)
    {
        _collection = new CNA.Graphics.SamplerStateCollection(graphicsDevice.Framework, vertexStage);
    }

    public SamplerState this[int index]
    {
        get => new(_collection[index]);
        set => _collection[index] = (value ?? throw new ArgumentNullException(nameof(value))).Framework;
    }
}
