namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Represents a batch of geometry within a model mesh.</summary>
public sealed class ModelMeshPart
{
    private ModelMesh? _parent;
    private Effect? _effect;
    private IndexBuffer? _indexBuffer;
    private VertexBuffer? _vertexBuffer;
    private int _startIndex;
    private int _primitiveCount;
    private int _vertexOffset;
    private int _numVertices;
    private bool _ownsResources;
    private bool _disposed;

    internal ModelMeshPart()
    {
    }

    internal ModelMeshPart(
        VertexBuffer? vertexBuffer,
        IndexBuffer? indexBuffer,
        int numVertices,
        int primitiveCount,
        int startIndex,
        int vertexOffset)
    {
        _vertexBuffer = vertexBuffer;
        _indexBuffer = indexBuffer;
        _numVertices = numVertices;
        _primitiveCount = primitiveCount;
        _startIndex = startIndex;
        _vertexOffset = vertexOffset;
    }

    public int StartIndex => _startIndex;

    public int PrimitiveCount => _primitiveCount;

    public int VertexOffset => _vertexOffset;

    public int NumVertices => _numVertices;

    public IndexBuffer? IndexBuffer => _indexBuffer;

    public VertexBuffer? VertexBuffer => _vertexBuffer;

    public Effect? Effect
    {
        get => _effect;
        set
        {
            if (ReferenceEquals(value, _effect))
            {
                return;
            }

            bool oldEffectStillUsed = false;
            bool newEffectAlreadyUsed = false;
            if (_parent is not null)
            {
                foreach (ModelMeshPart part in _parent.MeshParts)
                {
                    if (ReferenceEquals(part, this))
                    {
                        continue;
                    }

                    if (ReferenceEquals(part.Effect, _effect))
                    {
                        oldEffectStillUsed = true;
                    }
                    else if (ReferenceEquals(part.Effect, value))
                    {
                        newEffectAlreadyUsed = true;
                    }
                }

                if (!oldEffectStillUsed && _effect is not null)
                {
                    _parent.Effects.Remove(_effect);
                }

                if (!newEffectAlreadyUsed && value is not null)
                {
                    _parent.Effects.Add(value);
                }
            }

            _effect = value;
        }
    }

    public object? Tag { get; set; }

    internal void SetParent(ModelMesh parent) => _parent = parent;

    internal void SetVertexOffset(int value) => _vertexOffset = value;

    internal void SetNumVertices(int value) => _numVertices = value;

    internal void SetStartIndex(int value) => _startIndex = value;

    internal void SetPrimitiveCount(int value) => _primitiveCount = value;

    internal void SetVertexBuffer(VertexBuffer? value) => _vertexBuffer = value;

    internal void SetIndexBuffer(IndexBuffer? value) => _indexBuffer = value;

    internal void Draw()
    {
        if (NumVertices <= 0)
        {
            return;
        }

        GraphicsDevice graphicsDevice = VertexBuffer!.GraphicsDevice;
        graphicsDevice.SetVertexBuffer(VertexBuffer, VertexOffset);
        graphicsDevice.Indices = IndexBuffer;
        graphicsDevice.DrawIndexedPrimitives(
            PrimitiveType.TriangleList,
            0,
            0,
            NumVertices,
            StartIndex,
            PrimitiveCount);
    }

    internal void MarkResourcesOwned() => _ownsResources = true;

    internal void DisposeOwnedResources()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsResources)
        {
            VertexBuffer?.Dispose();
            IndexBuffer?.Dispose();
            Effect?.Dispose();
        }
    }
}
