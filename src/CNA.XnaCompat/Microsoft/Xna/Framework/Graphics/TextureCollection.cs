namespace Microsoft.Xna.Framework.Graphics;

public sealed class TextureCollection
{
    private readonly CNA.Graphics.TextureCollection _collection;

    internal TextureCollection(GraphicsDevice graphicsDevice, bool vertexStage)
    {
        _collection = new CNA.Graphics.TextureCollection(graphicsDevice.Framework, vertexStage);
    }

    public Texture? this[int index]
    {
        get => Texture.FromFramework(_collection[index]);
        set => _collection[index] = value?.FrameworkTexture;
    }
}
