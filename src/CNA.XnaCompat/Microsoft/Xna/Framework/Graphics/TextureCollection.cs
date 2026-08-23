namespace Microsoft.Xna.Framework.Graphics;

public sealed class TextureCollection
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly CNA.Graphics.TextureCollection _collection;
    private readonly int _slotCount;

    internal TextureCollection(GraphicsDevice graphicsDevice, bool vertexStage, int slotCount)
    {
        _graphicsDevice = graphicsDevice;
        _collection = new CNA.Graphics.TextureCollection(graphicsDevice.Framework, vertexStage);
        _slotCount = slotCount;
    }

    public Texture? this[int index]
    {
        get
        {
            if (_graphicsDevice.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(GraphicsDevice));
            }

            ValidateSlot(index);
            return Texture.FromFramework(_collection[index]);
        }
        set
        {
            if (_graphicsDevice.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(GraphicsDevice));
            }

            if (value?.IsDisposed == true)
            {
                throw new ObjectDisposedException(value.GetType().Name);
            }

            ValidateSlot(index);
            _collection[index] = value?.FrameworkTexture;
        }
    }

    private void ValidateSlot(int index)
    {
        if (index < 0 || index >= _slotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
