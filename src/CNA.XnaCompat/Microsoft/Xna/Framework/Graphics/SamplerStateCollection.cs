namespace Microsoft.Xna.Framework.Graphics;

public sealed class SamplerStateCollection
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly CNA.Graphics.SamplerStateCollection _collection;
    private readonly SamplerState[] _states;

    internal SamplerStateCollection(GraphicsDevice graphicsDevice, bool vertexStage, int slotCount)
    {
        _graphicsDevice = graphicsDevice;
        _collection = new CNA.Graphics.SamplerStateCollection(graphicsDevice.Framework, vertexStage);
        _states = new SamplerState[slotCount];
        Array.Fill(_states, SamplerState.LinearWrap);
    }

    public SamplerState this[int index]
    {
        get
        {
            ValidateSlot(index);
            return _states[index];
        }
        set
        {
            ValidateSlot(index);
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(value, _states[index]))
            {
                return;
            }

            value.Bind(_graphicsDevice);
            _collection[index] = value.Framework;
            _states[index] = value;
        }
    }

    private void ValidateSlot(int index)
    {
        if (index < 0 || index >= _states.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
