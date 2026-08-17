using CNA.Content.Cnj;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Builds a real, native-backed, compat-typed <see cref="Model"/> from a <see cref="CnjModelData"/>
/// -- this namespace's own counterpart to <c>CNA.Content.Cnj.CnjModelBuilder</c>, reusing that same
/// shared, native-free parsing layer directly (via <c>ContentManager.LoadCnjModelData</c>) rather
/// than re-parsing anything, the same "reuse the shared low-level helper, reimplement only the thin
/// native-backed assembly around it" pattern <see cref="XnbCompatModelBuilder"/> already established
/// for the <c>.xnb</c> path (and, before that, this compat layer's own <c>MediaLibrary</c> for
/// <c>SavedPictureStore</c>).
///
/// Builds compat-typed <see cref="Model"/>/<see cref="ModelBone"/>/<see cref="ModelMesh"/>/
/// <see cref="ModelMeshPart"/> throughout, same as <see cref="XnbCompatModelBuilder"/> -- so this
/// has its own <see cref="VertexBuffer"/>/<see cref="IndexBuffer"/>/<see cref="BasicEffect"/>
/// construction and its own <see cref="VertexDeclaration"/> converter, rather than reusing
/// <c>CNA.Content.Cnj.CnjModelBuilder</c>'s own (base-typed) versions of those, for the identical
/// reason <see cref="XnbCompatModelBuilder"/>'s own doc comment already explains.
///
/// <c>ModelMesh.Effects</c>/<c>ModelEffectCollection</c> still stay base-typed, unaffected by this
/// type -- see <see cref="ModelMesh"/>'s own doc comment for why that one specific gap doesn't have
/// a safe fix.
///
/// The "no bone hierarchy, synthesize one root bone plus one child bone per mesh" control flow below
/// *does* near-duplicate <c>CNA.Content.Cnj.CnjModelBuilder.Build</c>'s own -- the same trade-off
/// <see cref="XnbCompatModelBuilder"/>'s own doc comment already accepts (and explains in full) for
/// its own near-duplication of <c>CNA.Content.Xnb.XnbModelBuilder.Build</c>. <b>If you fix a bug in
/// this method, check <c>CNA.Content.Cnj.CnjModelBuilder.Build</c> too</b> -- there is no
/// compiler-enforced link between the two.
/// </summary>
internal static class CnjCompatModelBuilder
{
    internal static Model Build(GraphicsDevice graphicsDevice, CnjModelData data)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(data);

        var rootBone = new ModelBone(0, "Root");
        var bones = new List<ModelBone> { rootBone };

        var meshes = new List<ModelMesh>(data.Meshes.Count);
        var meshParentBones = new List<ModelBone>(data.Meshes.Count);

        foreach (CnjMeshData meshData in data.Meshes)
        {
            VertexBuffer vertexBuffer = BuildVertexBuffer(graphicsDevice, meshData.VertexBuffer);
            IndexBuffer indexBuffer = BuildIndexBuffer(graphicsDevice, meshData.IndexBuffer);

            var part = new ModelMeshPart(
                vertexBuffer, indexBuffer, meshData.VertexBuffer.VertexCount, meshData.PrimitiveCount, startIndex: 0, vertexOffset: 0);

            // BoundingSphere deliberately left at its default -- same real, faithfully-reproduced
            // gap CNA.Content.Cnj.CnjModelBuilder.Build's own doc comment already explains.
            var mesh = new ModelMesh(graphicsDevice, meshData.Name, [part]);
            meshes.Add(mesh);

            var childBone = new ModelBone(bones.Count, meshData.Name);
            rootBone.AddChild(childBone);
            bones.Add(childBone);
            mesh.ParentBone = childBone;
            meshParentBones.Add(childBone);

            // Same ordering requirement as CNA.Content.Cnj.CnjModelBuilder.Build's own -- Effect
            // assignment has to happen after the ModelMesh constructor above (which sets the part's
            // Parent link).
            part.Effect = BuildBasicEffect(graphicsDevice, meshData.Effect);
        }

        return new Model(graphicsDevice, bones, meshes, meshParentBones, rootBoneIndex: 0);
    }

    private static VertexBuffer BuildVertexBuffer(GraphicsDevice graphicsDevice, CNA.Content.Xnb.XnbVertexBufferData data)
    {
        var buffer = new VertexBuffer(graphicsDevice, ToCompat(data.Declaration), data.VertexCount, BufferUsage.None);
        buffer.SetData(data.Data);
        return buffer;
    }

    private static IndexBuffer BuildIndexBuffer(GraphicsDevice graphicsDevice, CNA.Content.Xnb.XnbIndexBufferData data)
    {
        IndexElementSize size = data.SixteenBits ? IndexElementSize.SixteenBits : IndexElementSize.ThirtyTwoBits;
        int indexCount = data.Data.Length / (data.SixteenBits ? 2 : 4);
        var buffer = new IndexBuffer(graphicsDevice, size, indexCount, BufferUsage.None);
        buffer.SetData(data.Data);
        return buffer;
    }

    /// <summary>Reuses <c>CNA.Content.Cnj.CnjModelBuilder.ApplyBasicEffectData</c> directly for the
    /// field-assignment logic itself (matching <see cref="XnbCompatModelBuilder"/>'s own identical
    /// reuse of <c>XnbModelBuilder.ApplyBasicEffectData</c>) -- nothing about it is compat-specific,
    /// since compat <see cref="BasicEffect"/> subclasses the base one directly and so upcasts fine
    /// as that method's parameter.</summary>
    private static BasicEffect BuildBasicEffect(GraphicsDevice graphicsDevice, CnjBasicEffectData data)
    {
        var effect = new BasicEffect(graphicsDevice);
        CnjModelBuilder.ApplyBasicEffectData(effect, data);
        return effect;
    }

    /// <summary>Same reasoning and shape as <see cref="XnbCompatModelBuilder"/>'s own identical
    /// converter -- <c>CNA.Content.Cnj</c> has no knowledge of this namespace, so it only ever
    /// produces a base <see cref="CNA.Graphics.VertexDeclaration"/>.</summary>
    private static VertexDeclaration ToCompat(CNA.Graphics.VertexDeclaration declaration)
    {
        CNA.Graphics.VertexElement[] source = declaration.GetVertexElements();
        var elements = new VertexElement[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            elements[i] = source[i];
        }

        return new VertexDeclaration(declaration.VertexStride, elements);
    }
}
