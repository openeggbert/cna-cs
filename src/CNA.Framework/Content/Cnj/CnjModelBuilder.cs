using CNA.Content.Xnb;
using CNA.Graphics;

namespace CNA.Content.Cnj;

/// <summary>
/// Builds a real, native-backed <see cref="Model"/> from a <see cref="CnjModelData"/> -- the final
/// assembly step, deliberately kept separate from <see cref="CnjModelReader"/> for the same reason
/// <see cref="XnbModelBuilder"/> is split from <see cref="XnbModelReader"/>: it needs a real
/// <see cref="Graphics.GraphicsDevice"/> to construct real <see cref="VertexBuffer"/>/
/// <see cref="IndexBuffer"/> instances, so it's native-ABI-blocked (compiles, needs a real
/// <c>cna-native</c> to actually run) even though <see cref="CnjModelReader"/> itself is not.
///
/// When the document has a real, multi-entry <c>"bones"</c> hierarchy (<see cref="CnjModelData.Bones"/>
/// non-empty), this links that real, file-supplied bone tree -- much like <see cref="XnbModelBuilder"/>
/// does for <c>.xnb</c>'s own bone hierarchy, though simpler: <c>.cnj</c> encodes each bone's own
/// parent index (always an already-constructed earlier entry), so a single forward pass suffices,
/// unlike <c>.xnb</c>'s child-index-list encoding, which needs a second pass once every bone exists.
/// Otherwise (no real hierarchy -- the cnjVersion-1-compatible case), this builder falls back to its
/// original, pre-hierarchy behavior: synthesizes one root <see cref="ModelBone"/> ("Root") plus one
/// real, synthetic child bone per mesh -- a real, load-bearing design choice preserved from the real
/// openeggbert/cna C++ engine's own <c>ModelTypeReader</c> (its own "no bone hierarchy" fallback
/// branch), not an invented shortcut: game code doing <c>model.Bones["PartName"]</c> lookups depends
/// on every mesh having its own bone either way.
/// </summary>
internal static class CnjModelBuilder
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
                var bone = new ModelBone(i, boneData.Name) { Transform = boneData.Transform };
                bones.Add(bone);

                // Entry 0 is always the root -- its own recorded Parent value is unused, matching
                // CnjModelReader.ReadBones's own doc comment (there is no earlier entry for it to
                // reference). Every later entry's Parent was already validated (< i) by the reader,
                // so it's always an already-constructed earlier bone here.
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
            VertexBuffer vertexBuffer = BuildVertexBuffer(graphicsDevice, meshData.VertexBuffer);
            IndexBuffer indexBuffer = BuildIndexBuffer(graphicsDevice, meshData.IndexBuffer);

            var part = new ModelMeshPart(vertexBuffer, indexBuffer, meshData.VertexBuffer.VertexCount, meshData.PrimitiveCount, startIndex: 0, vertexOffset: 0);

            // BoundingSphere is deliberately left at its default -- unlike .xnb's ModelReader (which
            // reads an explicit boundingSphere field), .cnj's ModelTypeReader never computes or sets
            // one for a rigid mesh. A real, faithfully-reproduced gap: this is not "improved on" by
            // computing one from vertex positions, since that would make this port's output diverge
            // from the reference implementation it's meant to match. See XnbModelBuilder.Build for
            // the .xnb path's own (genuinely different) behavior here.
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

            mesh.ParentBone = parentBone;
            meshParentBones.Add(parentBone);

            // Effect assignment has to happen *after* the ModelMesh constructor above (which sets
            // the part's Parent link) -- see ModelMeshPart.Effect's own doc comment, and
            // XnbModelBuilder.Build's own identical ordering requirement/comment for the .xnb path.
            part.Effect = BuildBasicEffect(graphicsDevice, meshData.Effect);
        }

        return new Model(graphicsDevice, bones, meshes, meshParentBones, rootBoneIndex: 0);
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

    private static BasicEffect BuildBasicEffect(GraphicsDevice graphicsDevice, CnjBasicEffectData data)
    {
        var effect = new BasicEffect(graphicsDevice);
        ApplyBasicEffectData(effect, data);
        return effect;
    }

    /// <summary>Applies only the fields <see cref="CnjBasicEffectData"/> actually carries --
    /// <b>deliberately does not call <see cref="XnbModelBuilder.ApplyBasicEffectData"/></b>, since
    /// <c>.cnj</c>'s <c>BasicEffect</c> JSON has no material-color fields at all (see
    /// <see cref="CnjBasicEffectData"/>'s own doc comment). <see cref="BasicEffect.Texture"/>/
    /// <see cref="BasicEffect.TextureEnabled"/> stay at their constructor defaults even when
    /// <see cref="CnjBasicEffectData.TextureReference"/> is non-null -- see that property's own doc
    /// comment for why actually resolving/loading it is deferred, the same "honest, not a full
    /// reproduction" choice <see cref="XnbModelBuilder.ApplyBasicEffectData"/> already made for the
    /// <c>.xnb</c> path's own unresolved texture reference. <c>internal</c>, matching
    /// <see cref="XnbModelBuilder.ApplyBasicEffectData"/>'s own reasoning exactly, so
    /// <c>CNA.XnaCompat</c>'s own <c>CnjCompatModelBuilder</c> can apply the same (trivial) field set
    /// to a compat-typed <see cref="BasicEffect"/> too (compat <c>BasicEffect</c> subclasses this one
    /// directly, so it upcasts fine as this method's parameter).</summary>
    internal static void ApplyBasicEffectData(BasicEffect effect, CnjBasicEffectData data)
    {
        effect.VertexColorEnabled = data.VertexColorEnabled;
    }
}
