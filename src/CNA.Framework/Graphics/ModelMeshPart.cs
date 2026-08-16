namespace CNA.Graphics;

/// <summary>
/// A batch of geometry within a <see cref="ModelMesh"/> that shares one <see cref="Effect"/>.
/// Real XNA's own constructor and its <c>SetXxx</c> setters are all content-pipeline-only
/// (<c>internal</c>) -- this project exposes them publicly for the same "no content pipeline
/// exists here" reason documented on <see cref="ModelBone"/>, matching the real openeggbert/cna
/// C++ engine's own <c>CNAEXT</c>-marked equivalents exactly (member-for-member, not invented).
/// Neither buffer parameter is validated non-null -- the real C++ constructor doesn't validate
/// them either, since a part with no buffers yet (filled in later via <see cref="SetVertexBuffer"/>/
/// <see cref="SetIndexBuffer"/>) is a legitimate intermediate state during hand-building.
/// </summary>
public class ModelMeshPart
{
    private Effect? _effect;

    internal ModelMesh? Parent;

    public ModelMeshPart()
    {
    }

    public ModelMeshPart(VertexBuffer? vertexBuffer, IndexBuffer? indexBuffer, int numVertices, int primitiveCount, int startIndex, int vertexOffset)
    {
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        NumVertices = numVertices;
        PrimitiveCount = primitiveCount;
        StartIndex = startIndex;
        VertexOffset = vertexOffset;
    }

    public int NumVertices { get; private set; }

    public int PrimitiveCount { get; private set; }

    public int StartIndex { get; private set; }

    public int VertexOffset { get; private set; }

    /// <summary>
    /// Setting this auto-maintains the parent <see cref="ModelMesh"/>'s <see cref="ModelMesh.Effects"/>
    /// collection, reproducing the real openeggbert/cna C++ engine's own
    /// <c>ModelMeshPart::setEffectProperty</c> algorithm exactly: adds the new effect to the
    /// parent's <see cref="ModelEffectCollection"/> if no sibling part already references it, and
    /// removes the old effect from it only if this was the last part still using it. This only
    /// does anything once the part actually belongs to a mesh (i.e. after being passed into a
    /// <see cref="ModelMesh"/> constructor, which is what sets its parent link) -- setting
    /// <see cref="Effect"/> before that point is a real, matching-the-real-engine no-op for mesh
    /// registration purposes, not a bug: hand-build parts, construct the mesh, *then* assign each
    /// part's effect.
    /// </summary>
    public Effect? Effect
    {
        get => _effect;
        set
        {
            if (ReferenceEquals(value, _effect))
            {
                return;
            }

            if (_effect is not null && Parent is not null)
            {
                bool stillUsedByAnotherPart = false;
                foreach (ModelMeshPart part in Parent.MeshParts)
                {
                    if (!ReferenceEquals(part, this) && ReferenceEquals(part.Effect, _effect))
                    {
                        stillUsedByAnotherPart = true;
                        break;
                    }
                }

                if (!stillUsedByAnotherPart)
                {
                    Parent.Effects.Remove(_effect);
                }
            }

            _effect = value;

            if (_effect is not null && Parent is not null && !Parent.Effects.Contains(_effect))
            {
                Parent.Effects.Add(_effect);
            }
        }
    }

    public IndexBuffer? IndexBuffer { get; private set; }

    public VertexBuffer? VertexBuffer { get; private set; }

    public object? Tag { get; set; }

    public void SetVertexOffset(int value) => VertexOffset = value;

    public void SetNumVertices(int value) => NumVertices = value;

    public void SetStartIndex(int value) => StartIndex = value;

    public void SetPrimitiveCount(int value) => PrimitiveCount = value;

    public void SetVertexBuffer(VertexBuffer? value) => VertexBuffer = value;

    public void SetIndexBuffer(IndexBuffer? value) => IndexBuffer = value;
}
