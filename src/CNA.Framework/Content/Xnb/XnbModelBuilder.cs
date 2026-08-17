using CNA.Graphics;

namespace CNA.Content.Xnb;

/// <summary>
/// Builds a real, native-backed <see cref="Model"/> from an <see cref="XnbModelData"/> -- the
/// final assembly step, deliberately kept separate from <see cref="XnbModelReader"/> because it
/// needs a real <see cref="GraphicsDevice"/> to construct real <see cref="VertexBuffer"/>/
/// <see cref="IndexBuffer"/> instances (both native-backed). Unlike everything upstream of it in
/// this feature (parsing the <c>.xnb</c> bytes into <see cref="XnbModelData"/> is pure C#, no
/// native dependency at all, and fully unit-testable), this step is native-ABI-blocked -- the same
/// "compiles, but needs a real <c>cna-native</c> to actually run" situation as
/// <see cref="ContentManager.Load{T}"/>'s existing <see cref="Texture2D"/>/<c>SoundEffect</c>/
/// <see cref="SpriteFont"/> cases.
/// </summary>
internal static class XnbModelBuilder
{
    internal static Model Build(GraphicsDevice graphicsDevice, XnbModelData data)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(data);

        var bones = new List<ModelBone>(data.Bones.Count);
        foreach (XnbBoneData boneData in data.Bones)
        {
            bones.Add(new ModelBone(boneData.Index, boneData.Name) { Transform = boneData.Transform });
        }

        foreach (XnbBoneData boneData in data.Bones)
        {
            ModelBone bone = bones[boneData.Index];
            foreach (int childIndex in boneData.ChildIndices)
            {
                bone.AddChild(bones[childIndex]);
            }
        }

        var meshes = new List<ModelMesh>(data.Meshes.Count);
        var meshParentBones = new List<ModelBone>(data.Meshes.Count);
        foreach (XnbMeshData meshData in data.Meshes)
        {
            var parts = new List<ModelMeshPart>(meshData.Parts.Count);
            foreach (XnbMeshPartData partData in meshData.Parts)
            {
                VertexBuffer? vertexBuffer = partData.VertexBuffer is null ? null : BuildVertexBuffer(graphicsDevice, partData.VertexBuffer);
                IndexBuffer? indexBuffer = partData.IndexBuffer is null ? null : BuildIndexBuffer(graphicsDevice, partData.IndexBuffer);

                parts.Add(new ModelMeshPart(vertexBuffer, indexBuffer, partData.NumVertices, partData.PrimitiveCount, partData.StartIndex, partData.VertexOffset));
            }

            var mesh = new ModelMesh(graphicsDevice, meshData.Name, parts) { BoundingSphere = meshData.BoundingSphere };
            meshes.Add(mesh);

            // -1 ("no parent bone") falls back to bone 0, matching Model's own 4-argument
            // constructor's existing rootBoneIndex leniency (see Model.cs) -- a mesh genuinely has
            // to hang off *some* bone for CopyAbsoluteBoneTransformsTo to make sense of it.
            int parentIndex = meshData.ParentBoneIndex >= 0 ? meshData.ParentBoneIndex : 0;
            meshParentBones.Add(bones[parentIndex]);
        }

        int rootBoneIndex = data.RootBoneIndex >= 0 ? data.RootBoneIndex : 0;
        return new Model(graphicsDevice, bones, meshes, meshParentBones, rootBoneIndex);
    }

    private static VertexBuffer BuildVertexBuffer(GraphicsDevice graphicsDevice, XnbVertexBufferData data)
    {
        var buffer = new VertexBuffer(graphicsDevice, data.Declaration, data.VertexCount, BufferUsage.None);
        buffer.SetData(data.Data);
        return buffer;
    }

    private static IndexBuffer BuildIndexBuffer(GraphicsDevice graphicsDevice, XnbIndexBufferData data)
    {
        IndexElementSize size = data.SixteenBits ? IndexElementSize.SixteenBits : IndexElementSize.ThirtyTwoBits;
        int indexCount = data.Data.Length / (data.SixteenBits ? 2 : 4);
        var buffer = new IndexBuffer(graphicsDevice, size, indexCount, BufferUsage.None);
        buffer.SetData(data.Data);
        return buffer;
    }
}
