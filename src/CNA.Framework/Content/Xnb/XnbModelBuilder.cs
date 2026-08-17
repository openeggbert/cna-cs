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
///
/// <c>CNA.XnaCompat</c>'s own <c>XnbCompatModelBuilder</c> is this method's compat-typed
/// counterpart -- it near-duplicates this method's bone-tree/mesh-part/effect-assignment-ordering
/// control flow rather than sharing it (a code-review finding, confirmed and accepted rather than
/// engineered around: <see cref="ModelBoneCollection"/>/<see cref="ModelMeshCollection"/> are
/// independent reimplementations, not subclasses -- the same wall that already ruled out a
/// covariant-return factory hook when this whole feature was designed -- so sharing this control
/// flow would need generic type parameters with delegate factories for every construction point,
/// real added complexity for what's a few dozen lines of straight-line assembly code. The same
/// "independent reimplementation accepts some duplication as the cost of a real structural
/// constraint" trade-off this project's own picture-library feature already made for
/// <c>MediaLibrary.SavePicture</c>'s orchestration). <b>If you fix a bug here (like the
/// once-real "Effect assignment order matters" one below), check <c>XnbCompatModelBuilder.Build</c>
/// too</b> -- there is no compiler-enforced link between the two.
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

            // Effect assignment has to happen *after* the ModelMesh constructor above (which sets
            // each part's Parent link) -- see ModelMeshPart.Effect's own doc comment: assigning it
            // before the part has a parent is a real, matching-the-real-engine no-op for mesh
            // effect-collection registration. A code-review finding caught this originally being
            // skipped entirely: every loaded ModelMeshPart.Effect stayed null, so
            // ModelMesh.Draw() silently skipped every part ("if (effect is null ...) continue;")
            // -- the model loaded without error but rendered nothing.
            for (int i = 0; i < meshData.Parts.Count; i++)
            {
                XnbBasicEffectData? effectData = meshData.Parts[i].Effect;
                if (effectData is not null)
                {
                    parts[i].Effect = BuildBasicEffect(graphicsDevice, effectData);
                }
            }

            // -1 ("no parent bone") falls back to bone 0, matching Model's own 4-argument
            // constructor's existing rootBoneIndex leniency (see Model.cs) -- a mesh genuinely has
            // to hang off *some* bone for CopyAbsoluteBoneTransformsTo to make sense of it.
            int parentIndex = meshData.ParentBoneIndex >= 0 ? meshData.ParentBoneIndex : 0;
            meshParentBones.Add(bones[parentIndex]);
        }

        int rootBoneIndex = data.RootBoneIndex >= 0 ? data.RootBoneIndex : 0;
        return new Model(graphicsDevice, bones, meshes, meshParentBones, rootBoneIndex);
    }

    /// <summary>Builds a real, base-typed <see cref="VertexBuffer"/> -- <c>internal</c> rather than
    /// <c>private</c> specifically so <c>CNA.XnaCompat</c>'s own compat-typed model builder can
    /// reuse it directly: <see cref="Graphics.ModelMeshPart"/> stays base-typed even for a compat
    /// <see cref="Model"/> (a documented, narrow compat gap -- see <c>Microsoft.Xna.Framework.Graphics.Model</c>'s
    /// own doc comment), so there is no compat-typed variant of this method to reuse instead of
    /// duplicating; this is the exact same object either builder would need to construct.</summary>
    internal static VertexBuffer BuildVertexBuffer(GraphicsDevice graphicsDevice, XnbVertexBufferData data)
    {
        var buffer = new VertexBuffer(graphicsDevice, data.Declaration, data.VertexCount, BufferUsage.None);
        buffer.SetData(data.Data);
        return buffer;
    }

    /// <summary>Same rationale as <see cref="BuildVertexBuffer"/>.</summary>
    internal static IndexBuffer BuildIndexBuffer(GraphicsDevice graphicsDevice, XnbIndexBufferData data)
    {
        IndexElementSize size = data.SixteenBits ? IndexElementSize.SixteenBits : IndexElementSize.ThirtyTwoBits;
        int indexCount = data.Data.Length / (data.SixteenBits ? 2 : 4);
        var buffer = new IndexBuffer(graphicsDevice, size, indexCount, BufferUsage.None);
        buffer.SetData(data.Data);
        return buffer;
    }

    /// <summary>Applies every field <see cref="XnbBasicEffectData"/> actually carries -- everything
    /// except <see cref="XnbBasicEffectData.TextureReference"/>, which stays unresolved (see that
    /// type's own doc comment for why: resolving it needs <c>ContentManager.Load&lt;Texture2D&gt;()</c>,
    /// itself native-ABI-blocked). <see cref="BasicEffect.TextureEnabled"/> is left at its default
    /// (<see langword="false"/>) rather than set <see langword="true"/> with no actual texture,
    /// which would be a real, misleading divergence from the source asset -- not a full
    /// reproduction of it, but an honest one. Same "internal, reused by CNA.XnaCompat's own model
    /// builder" rationale as <see cref="BuildVertexBuffer"/>.</summary>
    internal static BasicEffect BuildBasicEffect(GraphicsDevice graphicsDevice, XnbBasicEffectData data) => new(graphicsDevice)
    {
        DiffuseColor = data.DiffuseColor,
        EmissiveColor = data.EmissiveColor,
        SpecularColor = data.SpecularColor,
        SpecularPower = data.SpecularPower,
        Alpha = data.Alpha,
        VertexColorEnabled = data.VertexColorEnabled,
    };
}
