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
public class ModelMeshPart : IDisposable
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
    /// registration purposes, confirmed against the real openeggbert/cna C++ engine's own
    /// <c>ModelMesh</c> constructor, which sets each part's parent link via a raw field assignment
    /// too, with no equivalent re-registration step. <b>This is not merely inconvenient if
    /// violated</b>: <see cref="Model.Draw"/> only updates <see cref="IEffectMatrices"/> for
    /// effects it finds in <see cref="ModelMesh.Effects"/>, so an effect assigned before its part
    /// had a parent silently never gets its <c>World</c>/<c>View</c>/<c>Projection</c> updated (and
    /// never gets the "does this effect implement <see cref="IEffectMatrices"/>" safety check
    /// either) while <see cref="ModelMesh.Draw"/> still renders that part with it, using whatever
    /// stale matrix values the effect already had -- silently wrong rendering, not an exception.
    /// Always hand-build parts, construct the mesh, *then* assign each part's effect.
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

    /// <summary>
    /// Releases the vertex buffer, index buffer and effect this part holds.
    ///
    /// Real XNA has no <c>ModelMeshPart.Dispose</c> -- there, a loaded model's GPU resources are
    /// reclaimed with the device. Here they are native handles with no such owner, so without this
    /// a loaded model leaked one effect (plus its three directional lights) and two buffers per
    /// part until the GC got round to the finalizers. Found by a code-review pass; see
    /// <c>plan.md</c> WP18.
    ///
    /// Only disposes what a model builder created for this part. A part whose buffers or effect
    /// were assigned by game code (<see cref="SetVertexBuffer"/>, <see cref="SetIndexBuffer"/>,
    /// <see cref="Effect"/>) is that caller's to manage, so those are left alone -- disposing a
    /// buffer the caller still holds would be worse than the leak this fixes.
    /// </summary>
    public void Dispose()
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

        GC.SuppressFinalize(this);
    }

    private bool _disposed;
    private bool _ownsResources;

    /// <summary>Marks this part's current buffers and effect as builder-created, so
    /// <see cref="Dispose"/> releases them. Set only by the model builders -- see that method's own
    /// doc comment for why assignment from game code deliberately does not set it.</summary>
    internal void MarkResourcesOwned() => _ownsResources = true;

}
