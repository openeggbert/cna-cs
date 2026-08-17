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
/// This minimal reader's scope excludes the <c>.cnj</c> <c>"bones"</c> hierarchy entirely (see
/// <see cref="CnjModelReader"/>'s own doc comment), so unlike <see cref="XnbModelBuilder"/> (which
/// links a real, file-supplied bone tree), this builder always synthesizes the same simple shape:
/// one root <see cref="ModelBone"/> ("Root") plus one real, synthetic child bone per mesh -- a
/// real, load-bearing design choice preserved from the real openeggbert/cna C++ engine's own
/// <c>ModelTypeReader</c> (its "no bone hierarchy" fallback branch), not an invented shortcut: game
/// code doing <c>model.Bones["PartName"]</c> lookups depends on every mesh having its own bone.
/// </summary>
internal static class CnjModelBuilder
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

            var part = new ModelMeshPart(vertexBuffer, indexBuffer, meshData.VertexBuffer.VertexCount, meshData.PrimitiveCount, startIndex: 0, vertexOffset: 0);

            // BoundingSphere is deliberately left at its default -- unlike .xnb's ModelReader (which
            // reads an explicit boundingSphere field), .cnj's ModelTypeReader never computes or sets
            // one for a rigid mesh. A real, faithfully-reproduced gap: this is not "improved on" by
            // computing one from vertex positions, since that would make this port's output diverge
            // from the reference implementation it's meant to match. See XnbModelBuilder.Build for
            // the .xnb path's own (genuinely different) behavior here.
            var mesh = new ModelMesh(graphicsDevice, meshData.Name, [part]);
            meshes.Add(mesh);

            var childBone = new ModelBone(bones.Count, meshData.Name);
            rootBone.AddChild(childBone);
            bones.Add(childBone);
            mesh.ParentBone = childBone;
            meshParentBones.Add(childBone);

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

    /// <summary>Applies only the fields <see cref="CnjBasicEffectData"/> actually carries --
    /// <b>deliberately does not call <see cref="XnbModelBuilder.ApplyBasicEffectData"/></b>, since
    /// <c>.cnj</c>'s <c>BasicEffect</c> JSON has no material-color fields at all (see
    /// <see cref="CnjBasicEffectData"/>'s own doc comment). <see cref="BasicEffect.Texture"/>/
    /// <see cref="BasicEffect.TextureEnabled"/> stay at their constructor defaults even when
    /// <see cref="CnjBasicEffectData.TextureReference"/> is non-null -- see that property's own doc
    /// comment for why actually resolving/loading it is deferred, the same "honest, not a full
    /// reproduction" choice <see cref="XnbModelBuilder.ApplyBasicEffectData"/> already made for the
    /// <c>.xnb</c> path's own unresolved texture reference.</summary>
    private static BasicEffect BuildBasicEffect(GraphicsDevice graphicsDevice, CnjBasicEffectData data)
    {
        var effect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = data.VertexColorEnabled,
        };

        return effect;
    }
}
