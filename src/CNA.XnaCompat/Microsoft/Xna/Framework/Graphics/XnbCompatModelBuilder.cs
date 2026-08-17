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
/// Builds compat-typed <see cref="Model"/>/<see cref="ModelBone"/>/<see cref="ModelMesh"/>/
/// <see cref="ModelMeshPart"/> throughout -- a follow-up pass extended this from the original
/// scope (which left <c>ModelMeshPart</c> base-typed) once <see cref="ModelMeshPart"/>/
/// <see cref="ModelMeshPartCollection"/> got their own compat mirror. This means the buffer/effect
/// construction below can no longer reuse <c>CNA.Content.Xnb.XnbModelBuilder</c>'s own
/// <c>BuildVertexBuffer</c>/<c>BuildIndexBuffer</c>/<c>BuildBasicEffect</c> directly (those build
/// *base*-typed instances, which is no longer sufficient now that a compat <see cref="ModelMeshPart"/>'s
/// own constructor expects compat-typed buffers) -- this type has its own versions instead,
/// including a <see cref="VertexDeclaration"/> converter (base <see cref="CNA.Graphics.VertexDeclaration"/>
/// has no compat equivalent produced anywhere upstream in <c>CNA.Content.Xnb</c>, which has no
/// knowledge of this namespace at all).
///
/// <c>ModelMesh.Effects</c>/<c>ModelEffectCollection</c> still stay base-typed, unaffected by
/// this pass -- see <see cref="ModelMesh"/>'s own doc comment for why that one specific gap doesn't
/// have a safe fix.
///
/// The bone-tree/mesh-part/effect-assignment-ordering/parent-bone-fallback control flow below
/// *does* near-duplicate <c>CNA.Content.Xnb.XnbModelBuilder.Build</c>'s own -- a code-review
/// finding, confirmed and accepted rather than engineered around (see that type's own doc comment
/// for the full reasoning: a shared, generic, delegate-parameterized assembler was judged more
/// complex than the duplication it would remove, the same trade-off already accepted for
/// <c>MediaLibrary.SavePicture</c>'s own orchestration duplication). <b>If you fix a bug in this
/// method, check <c>CNA.Content.Xnb.XnbModelBuilder.Build</c> too</b> -- there is no
/// compiler-enforced link between the two.
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
            var parts = new List<ModelMeshPart>(meshData.Parts.Count);
            foreach (XnbMeshPartData partData in meshData.Parts)
            {
                VertexBuffer? vertexBuffer = partData.VertexBuffer is null ? null : BuildVertexBuffer(graphicsDevice, partData.VertexBuffer);
                IndexBuffer? indexBuffer = partData.IndexBuffer is null ? null : BuildIndexBuffer(graphicsDevice, partData.IndexBuffer);

                parts.Add(new ModelMeshPart(
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
                    parts[i].Effect = BuildBasicEffect(graphicsDevice, effectData);
                }
            }

            int parentIndex = meshData.ParentBoneIndex >= 0 ? meshData.ParentBoneIndex : 0;
            meshParentBones.Add(bones[parentIndex]);
        }

        int rootBoneIndex = data.RootBoneIndex >= 0 ? data.RootBoneIndex : 0;
        return new Model(graphicsDevice, bones, meshes, meshParentBones, rootBoneIndex);
    }

    // internal, not private -- a code-review finding caught CnjCompatModelBuilder duplicating these
    // three members byte-for-byte, since CnjMeshData reuses these exact CNA.Content.Xnb-namespaced
    // types verbatim (they're format-agnostic: "declaration/count/raw bytes" and "sixteen-bit
    // flag/raw bytes"), so there was nothing compat-specific enough about the .xnb path here to
    // justify a second, identical copy -- the same "reuse if there's no real difference" reasoning
    // this class's own BuildBasicEffect already applies to XnbModelBuilder.ApplyBasicEffectData.
    internal static VertexBuffer BuildVertexBuffer(GraphicsDevice graphicsDevice, XnbVertexBufferData data)
    {
        var buffer = new VertexBuffer(graphicsDevice, ToCompat(data.Declaration), data.VertexCount, BufferUsage.None);
        buffer.SetData(data.Data);
        return buffer;
    }

    internal static IndexBuffer BuildIndexBuffer(GraphicsDevice graphicsDevice, XnbIndexBufferData data)
    {
        IndexElementSize size = data.SixteenBits ? IndexElementSize.SixteenBits : IndexElementSize.ThirtyTwoBits;
        int indexCount = data.Data.Length / (data.SixteenBits ? 2 : 4);
        var buffer = new IndexBuffer(graphicsDevice, size, indexCount, BufferUsage.None);
        buffer.SetData(data.Data);
        return buffer;
    }

    /// <summary>Constructs a compat-typed <see cref="BasicEffect"/> (since <see cref="ModelMeshPart"/>'s
    /// constructor, unlike its <c>Effect</c> property, is not the documented gap), but reuses
    /// <c>CNA.Content.Xnb.XnbModelBuilder.ApplyBasicEffectData</c> directly for the field-assignment
    /// logic itself (a code-review finding: the two used to duplicate that logic field-by-field,
    /// with nothing actually compat-specific about it -- compat <see cref="BasicEffect"/> subclasses
    /// the base one directly, so it upcasts fine as that method's parameter).</summary>
    private static BasicEffect BuildBasicEffect(GraphicsDevice graphicsDevice, XnbBasicEffectData data)
    {
        var effect = new BasicEffect(graphicsDevice);
        XnbModelBuilder.ApplyBasicEffectData(effect, data);
        return effect;
    }

    /// <summary>Converts a base <see cref="CNA.Graphics.VertexDeclaration"/> (all
    /// <c>CNA.Content.Xnb</c>/<c>CNA.Content.Cnj</c> ever produce, since neither namespace has any
    /// knowledge of <c>CNA.XnaCompat</c>) into this namespace's own -- element-wise, through
    /// <see cref="VertexElement"/>'s own implicit conversion operators, the same "arrays of a
    /// type with a user-defined conversion operator do not convert automatically" reason every
    /// other array conversion in this compat layer needs one. <c>internal</c>, reused by
    /// <see cref="CnjCompatModelBuilder"/> for the same reason <see cref="BuildVertexBuffer"/>/
    /// <see cref="BuildIndexBuffer"/> are.</summary>
    internal static VertexDeclaration ToCompat(CNA.Graphics.VertexDeclaration declaration)
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
