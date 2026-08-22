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
/// <see cref="ModelMeshPart"/> throughout, same as <see cref="XnbCompatModelBuilder"/> -- rather
/// than reusing <c>CNA.Content.Cnj.CnjModelBuilder</c>'s own (base-typed)
/// <see cref="VertexBuffer"/>/<see cref="IndexBuffer"/> construction, for the identical reason
/// <see cref="XnbCompatModelBuilder"/>'s own doc comment already explains. Reuses
/// <see cref="XnbCompatModelBuilder.BuildVertexBuffer"/>/<see cref="XnbCompatModelBuilder.BuildIndexBuffer"/>/
/// <see cref="XnbCompatModelBuilder.ToCompat"/> directly rather than duplicating them, though --
/// <c>CnjMeshData</c>'s vertex/index buffer data is itself the exact same, format-agnostic
/// <c>CNA.Content.Xnb</c>-namespaced types <c>XnbCompatModelBuilder</c> already builds from
/// (a code-review finding caught a first attempt duplicating those three members byte-for-byte,
/// with nothing actually format-specific about them).
///
/// <c>ModelMesh.Effects</c>/<c>ModelEffectCollection</c> still stay base-typed, unaffected by this
/// type -- see <see cref="ModelMesh"/>'s own doc comment for why that one specific gap doesn't have
/// a safe fix.
///
/// When the document has a real, multi-entry <c>"bones"</c> hierarchy (<c>CnjModelData.Bones</c>
/// non-empty, cnjVersion 2), this links that real, file-supplied bone tree -- matching
/// <c>CNA.Content.Cnj.CnjModelBuilder.Build</c>'s own identical dual-path shape exactly (a single
/// forward pass, since <c>.cnj</c> encodes each bone's own parent index). Otherwise (no real
/// hierarchy), this builder falls back to its original, pre-hierarchy behavior: synthesizes one root
/// <see cref="ModelBone"/> ("Root") plus one real, synthetic child bone per mesh.
///
/// This control flow *does* near-duplicate <c>CNA.Content.Cnj.CnjModelBuilder.Build</c>'s own -- the
/// same trade-off <see cref="XnbCompatModelBuilder"/>'s own doc comment already accepts (and explains
/// in full) for its own near-duplication of <c>CNA.Content.Xnb.XnbModelBuilder.Build</c>. <b>If you
/// fix a bug in this method, check <c>CNA.Content.Cnj.CnjModelBuilder.Build</c> too</b> -- there is
/// no compiler-enforced link between the two.
/// </summary>
internal static class CnjCompatModelBuilder
{
    internal static Model Build(GraphicsDevice graphicsDevice, CnjModelData data)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(data);

        bool hasBoneHierarchy = data.Bones.Count > 0;

        List<ModelBone> bones;
        if (hasBoneHierarchy)
        {
            bones = new List<ModelBone>(data.Bones.Count);
            for (int i = 0; i < data.Bones.Count; i++)
            {
                CnjBoneData boneData = data.Bones[i];
                var bone = new ModelBone(i, boneData.Name) { Transform = boneData.Transform.ToCompat() };
                bones.Add(bone);

                // Entry 0 is always the root -- its own recorded Parent value is unused, matching
                // CnjModelReader.ReadBones's own doc comment. Every later entry's Parent was already
                // validated (< i) by the reader, so it's always an already-constructed earlier bone.
                if (i > 0)
                {
                    bones[boneData.Parent].AddChild(bone);
                }
            }
        }
        else
        {
            bones = [new ModelBone(0, "Root")];
        }

        var meshes = new List<ModelMesh>(data.Meshes.Count);
        var meshParentBones = new List<ModelBone>(data.Meshes.Count);

        foreach (CnjMeshData meshData in data.Meshes)
        {
            VertexBuffer vertexBuffer = XnbCompatModelBuilder.BuildVertexBuffer(graphicsDevice, meshData.VertexBuffer);
            IndexBuffer indexBuffer = XnbCompatModelBuilder.BuildIndexBuffer(graphicsDevice, meshData.IndexBuffer);

            var part = new ModelMeshPart(
                vertexBuffer, indexBuffer, meshData.VertexBuffer.VertexCount, meshData.PrimitiveCount, startIndex: 0, vertexOffset: 0);

            // BoundingSphere deliberately left at its default -- same real, faithfully-reproduced
            // gap CNA.Content.Cnj.CnjModelBuilder.Build's own doc comment already explains.
            var mesh = new ModelMesh(graphicsDevice, meshData.Name, [part]);
            meshes.Add(mesh);

            ModelBone parentBone;
            if (hasBoneHierarchy)
            {
                // Guaranteed non-null here -- CnjModelReader only leaves ParentBoneIndex null when
                // the document has no real bone hierarchy, the else branch below.
                parentBone = bones[meshData.ParentBoneIndex!.Value];
            }
            else
            {
                var childBone = new ModelBone(bones.Count, meshData.Name);
                bones[0].AddChild(childBone);
                bones.Add(childBone);
                parentBone = childBone;
            }

            mesh.SetParentBone(parentBone);
            meshParentBones.Add(parentBone);

            // Same ordering requirement as CNA.Content.Cnj.CnjModelBuilder.Build's own -- Effect
            // assignment has to happen after the ModelMesh constructor above (which sets the part's
            // Parent link).
            part.Effect = BuildBasicEffect(graphicsDevice, meshData.Effect);
            part.MarkResourcesOwned();
        }

        return new Model(graphicsDevice, bones, meshes, meshParentBones, rootBoneIndex: 0);
    }

    /// <summary>Reuses <c>CNA.Content.Cnj.CnjModelBuilder.ApplyBasicEffectData</c> directly for the
    /// field-assignment logic itself (matching <see cref="XnbCompatModelBuilder"/>'s own identical
    /// reuse of <c>XnbModelBuilder.ApplyBasicEffectData</c>) -- nothing about it is compat-specific,
    /// since compat <see cref="BasicEffect"/> subclasses the base one directly and so upcasts fine
    /// as that method's parameter.</summary>
    private static BasicEffect BuildBasicEffect(GraphicsDevice graphicsDevice, CnjBasicEffectData data)
    {
        var effect = new BasicEffect(graphicsDevice);
        CnjModelBuilder.ApplyBasicEffectData((CNA.Graphics.BasicEffect)effect.Inner, data);
        return effect;
    }
}
