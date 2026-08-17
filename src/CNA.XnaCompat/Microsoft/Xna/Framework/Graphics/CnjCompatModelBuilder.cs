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
/// The "no bone hierarchy, synthesize one root bone plus one child bone per mesh" control flow below
/// *does* near-duplicate <c>CNA.Content.Cnj.CnjModelBuilder.Build</c>'s own -- the same trade-off
/// <see cref="XnbCompatModelBuilder"/>'s own doc comment already accepts (and explains in full) for
/// its own near-duplication of <c>CNA.Content.Xnb.XnbModelBuilder.Build</c>. <b>If you fix a bug in
/// this method, check <c>CNA.Content.Cnj.CnjModelBuilder.Build</c> too</b> -- there is no
/// compiler-enforced link between the two.
///
/// <b>Real, documented gap, not yet closed:</b> unlike the base (non-compat) path, this builder does
/// not yet link a document's own real <c>"bones"</c> hierarchy (<c>CnjModelData.Bones</c>, cnjVersion
/// 2) -- it explicitly rejects such a document with a <c>ContentLoadException</c> rather than
/// silently falling back to the synthesize-a-bone-per-mesh shape below (which would produce a
/// genuinely *wrong*, not just incomplete, bone structure for it). Closing this gap is its own
/// separate, deliberately deferred follow-up, matching the "narrow reader/builder first, compat
/// mirror as a distinct, separately-reviewed follow-up" cadence this whole feature has already used
/// twice (once for the base <c>.cnj</c> reader itself, once for this compat mirror's own original
/// <c>BasicEffect</c>-only scope).
/// </summary>
internal static class CnjCompatModelBuilder
{
    internal static Model Build(GraphicsDevice graphicsDevice, CnjModelData data)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(data);

        // CnjModelBuilder.Build (the base, non-compat path) now links a document's own real
        // "bones" hierarchy when present -- this compat mirror doesn't yet, matching this whole
        // feature's own "narrow reader/builder first, compat mirror as a distinct, separately
        // -reviewed follow-up" cadence. Rejecting explicitly here (rather than silently falling
        // back to the synthesize-a-bone-per-mesh shape below, which would produce a genuinely
        // *wrong* bone structure for such a document) matches this whole feature's "detect and
        // throw a clear exception, never silently mis-load" discipline.
        if (data.Bones.Count > 0)
        {
            throw new CNA.Content.ContentLoadException(
                "This .cnj document has a real 'bones' hierarchy, which CnjCompatModelBuilder does not yet support " +
                "(CNA.Content.Cnj.CnjModelBuilder, the non-compat path, already does).");
        }

        var rootBone = new ModelBone(0, "Root");
        var bones = new List<ModelBone> { rootBone };

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
}
