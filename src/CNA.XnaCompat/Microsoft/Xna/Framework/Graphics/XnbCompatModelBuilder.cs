using CNA.Content.Xnb;

namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Builds a real, native-backed, compat-typed <see cref="Model"/> from an <see cref="XnbModelData"/>
/// -- this namespace's own counterpart to <c>CNA.Content.Xnb.XnbModelBuilder</c>, reusing that same
/// shared, native-free parsing layer directly (via <c>ContentManager.LoadXnbModelData</c>) rather
/// than re-parsing anything, the same "reuse the shared low-level helper, reimplement only the thin
/// native-backed assembly around it" pattern this compat layer's own <c>MediaLibrary</c> already
/// established for <c>SavedPictureStore</c>.
///
/// Builds compat-typed <see cref="Model"/>/<see cref="ModelBone"/>/<see cref="ModelMesh"/>, but each
/// mesh's <c>ModelMeshPart</c>s stay base-typed (<c>CNA.Graphics.ModelMeshPart</c>) -- the
/// same documented, narrow compat gap <see cref="Model"/>'s own doc comment already establishes for
/// hand-built compat models, applied identically here. <see cref="CNA.Content.Xnb.XnbModelBuilder.BuildVertexBuffer"/>/
/// <c>BuildIndexBuffer</c>/<c>BuildBasicEffect</c> are reused directly (made <c>internal</c>
/// specifically for this reuse) rather than duplicated -- they already build exactly the
/// base-typed <c>VertexBuffer</c>/<c>IndexBuffer</c>/<c>BasicEffect</c> instances a base-typed
/// <see cref="CNA.Graphics.ModelMeshPart"/> needs, so there is nothing compat-specific left for a
/// separate copy of that logic to add.
/// </summary>
internal static class XnbCompatModelBuilder
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
            var parts = new List<CNA.Graphics.ModelMeshPart>(meshData.Parts.Count);
            foreach (XnbMeshPartData partData in meshData.Parts)
            {
                CNA.Graphics.VertexBuffer? vertexBuffer = partData.VertexBuffer is null
                    ? null
                    : XnbModelBuilder.BuildVertexBuffer(graphicsDevice, partData.VertexBuffer);
                CNA.Graphics.IndexBuffer? indexBuffer = partData.IndexBuffer is null
                    ? null
                    : XnbModelBuilder.BuildIndexBuffer(graphicsDevice, partData.IndexBuffer);

                parts.Add(new CNA.Graphics.ModelMeshPart(
                    vertexBuffer, indexBuffer, partData.NumVertices, partData.PrimitiveCount, partData.StartIndex, partData.VertexOffset));
            }

            var mesh = new ModelMesh(graphicsDevice, meshData.Name, parts) { BoundingSphere = meshData.BoundingSphere };
            meshes.Add(mesh);

            // Same ordering requirement as CNA.Content.Xnb.XnbModelBuilder's own -- Effect
            // assignment has to happen after the ModelMesh constructor above (which sets each
            // part's Parent link).
            for (int i = 0; i < meshData.Parts.Count; i++)
            {
                XnbBasicEffectData? effectData = meshData.Parts[i].Effect;
                if (effectData is not null)
                {
                    parts[i].Effect = XnbModelBuilder.BuildBasicEffect(graphicsDevice, effectData);
                }
            }

            int parentIndex = meshData.ParentBoneIndex >= 0 ? meshData.ParentBoneIndex : 0;
            meshParentBones.Add(bones[parentIndex]);
        }

        int rootBoneIndex = data.RootBoneIndex >= 0 ? data.RootBoneIndex : 0;
        return new Model(graphicsDevice, bones, meshes, meshParentBones, rootBoneIndex);
    }
}
