namespace CNA.Graphics;

/// <summary>
/// A mesh that is part of a <see cref="Model"/>, made up of one or more <see cref="ModelMeshPart"/>s.
/// Constructors are <c>CNAEXT</c> (content-pipeline-only in real XNA), same reason and same
/// treatment as <see cref="ModelBone"/>'s own constructor.
/// </summary>
public class ModelMesh : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;

    public ModelMesh(GraphicsDevice graphicsDevice, IReadOnlyList<ModelMeshPart> parts)
        : this(graphicsDevice, string.Empty, parts)
    {
    }

    /// <summary><paramref name="name"/> may be null: XNA content permits an unnamed mesh. See
    /// <see cref="ModelBone(int, string?)"/> for the measurement that established this.</summary>
    public ModelMesh(GraphicsDevice graphicsDevice, string? name, IReadOnlyList<ModelMeshPart> parts)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(parts);

        _graphicsDevice = graphicsDevice;
        Name = name;

        var partList = new List<ModelMeshPart>(parts);
        foreach (ModelMeshPart part in partList)
        {
            part.Parent = this;
        }

        MeshParts = new ModelMeshPartCollection(partList);
    }

    public BoundingSphere BoundingSphere { get; set; }

    public ModelEffectCollection Effects { get; } = new();

    public ModelMeshPartCollection MeshParts { get; }

    public string Name { get; }

    public ModelBone? ParentBone { get; set; }

    public object? Tag { get; set; }

    /// <summary>
    /// Draws every part that has both an <see cref="Effect"/> assigned and a positive
    /// <see cref="ModelMeshPart.PrimitiveCount"/>, reproducing the real openeggbert/cna C++
    /// engine's own <c>ModelMesh::Draw</c> loop exactly: bind the part's buffers, apply every pass
    /// of the effect's current technique, then issue one indexed draw call per pass. Skips the
    /// real engine's own <c>Ensure3DSupported</c> renderer-capability check -- that's an internal
    /// native-side detail this project's minimal <see cref="GraphicsDevice"/> surface has no
    /// equivalent for, not something dropped by mistake.
    /// </summary>
    public void Draw()
    {
        foreach (ModelMeshPart part in MeshParts)
        {
            Effect? effect = part.Effect;
            if (effect is null || part.PrimitiveCount <= 0)
            {
                continue;
            }

            _graphicsDevice.SetVertexBuffer(part.VertexBuffer);
            _graphicsDevice.Indices = part.IndexBuffer;

            // CurrentTechnique, its Passes collection, and every pass are OWNED native handles
            // minted per call (see EffectPass's doc comment), so this per-frame path disposes them
            // rather than leaving them to the SafeHandle finalizers -- otherwise a 60 fps draw
            // queues hundreds of native handles per second for finalization.
            using EffectTechnique technique = effect.CurrentTechnique;
            using EffectPassCollection passes = technique.Passes;

            foreach (EffectPass pass in passes)
            {
                using (pass)
                {
                    pass.Apply();
                    _graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        part.VertexOffset,
                        0,
                        part.NumVertices,
                        part.StartIndex,
                        part.PrimitiveCount);
                }
            }
        }
    }

    /// <summary>Disposes every part of this mesh -- see <see cref="ModelMeshPart.Dispose"/> for
    /// what each part actually releases.</summary>
    public void Dispose()
    {
        foreach (ModelMeshPart part in MeshParts)
        {
            part.Dispose();
        }

        GC.SuppressFinalize(this);
    }

}
