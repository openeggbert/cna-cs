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
    internal static Model Build(
        GraphicsDevice graphicsDevice, XnbModelData data, Content.ContentManager contentManager)
    {
        ArgumentNullException.ThrowIfNull(contentManager);

        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(data);

        var bones = new List<ModelBone>(data.Bones.Count);
        foreach (XnbBoneData boneData in data.Bones)
        {
            bones.Add(new ModelBone(boneData.Index, boneData.Name) { Transform = boneData.Transform.ToCompat() });
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
                    vertexBuffer, indexBuffer, partData.NumVertices, partData.PrimitiveCount, partData.StartIndex, partData.VertexOffset)
                {
                    Tag = partData.Tag,
                });
            }

            var mesh = new ModelMesh(graphicsDevice, meshData.Name, parts) { Tag = meshData.Tag };
            mesh.SetBoundingSphere(meshData.BoundingSphere.ToCompat());
            meshes.Add(mesh);

            // Same ordering requirement as CNA.Content.Xnb.XnbModelBuilder's own -- Effect
            // assignment has to happen after the ModelMesh constructor above (which sets each
            // part's Parent link).
            for (int i = 0; i < meshData.Parts.Count; i++)
            {
                XnbEffectData? effectData = meshData.Parts[i].Effect;
                if (effectData is not null)
                {
                    parts[i].Effect = BuildEffect(graphicsDevice, contentManager, effectData);
                    parts[i].MarkResourcesOwned();
                }
            }

            int parentIndex = meshData.ParentBoneIndex >= 0 ? meshData.ParentBoneIndex : 0;
            meshParentBones.Add(bones[parentIndex]);
        }

        int rootBoneIndex = data.RootBoneIndex >= 0 ? data.RootBoneIndex : 0;
        return new Model(graphicsDevice, bones, meshes, meshParentBones, rootBoneIndex) { Tag = data.Tag };
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
    /// with nothing actually compat-specific about it). The compat <see cref="BasicEffect"/> uses
    /// composition rather than subclassing the base implementation, so the shared
    /// helper is handed that inner effect rather than the wrapper.</summary>
    /// <summary>The compat-typed counterpart of <c>CNA.Content.Xnb.XnbModelBuilder.BuildEffect</c>:
    /// same four shapes, same external references, constructed as this namespace's own types
    /// because <see cref="ModelMeshPart.Effect"/> is compat-typed. The references are loaded through
    /// the compat <see cref="Content.ContentManager"/> so they land as compat
    /// <see cref="Texture2D"/>/<see cref="TextureCube"/>/<see cref="Effect"/> and share that
    /// manager's cache and unload lifetime.</summary>
    private static Effect BuildEffect(
        GraphicsDevice graphicsDevice, Content.ContentManager contentManager, XnbEffectData data) =>
        data switch
        {
            XnbBasicEffectData basic => BuildBasicEffect(graphicsDevice, contentManager, basic),
            XnbEffectMaterialData material => BuildEffectMaterial(contentManager, material),
            XnbEnvironmentMapEffectData environmentMap => BuildEnvironmentMapEffect(graphicsDevice, contentManager, environmentMap),
            XnbDualTextureEffectData dualTexture => BuildDualTextureEffect(graphicsDevice, contentManager, dualTexture),
            _ => throw new Content.ContentLoadException($"Unsupported model effect data {data.GetType().Name}."),
        };

    private static BasicEffect BuildBasicEffect(
        GraphicsDevice graphicsDevice, Content.ContentManager contentManager, XnbBasicEffectData data)
    {
        var effect = new BasicEffect(graphicsDevice);
        XnbModelBuilder.ApplyBasicEffectData((CNA.Graphics.BasicEffect)effect.Inner, data);

        if (data.TextureReference is { } reference)
        {
            effect.Texture = contentManager.Load<Texture2D>(reference);
            effect.TextureEnabled = true;
        }

        return effect;
    }

    private static EffectMaterial BuildEffectMaterial(
        Content.ContentManager contentManager, XnbEffectMaterialData data)
    {
        if (data.EffectReference is not { } reference)
        {
            throw new Content.ContentLoadException(
                "This .xnb file's EffectMaterial names no effect to clone, which XNA's own reader has no defined behaviour for.");
        }

        var material = new EffectMaterial(contentManager.Load<Effect>(reference));

        // Applied to the inner framework effect, which is the same native effect the compat
        // wrapper exposes, so the parameter-setting rules live in one place rather than two. A
        // texture parameter is resolved by name through this compat manager rather than the
        // framework one, so it is cached and unloaded alongside everything else this manager holds.
        XnbModelBuilder.ApplyEffectParameters(
            material.Inner,
            data.Parameters,
            reference => LoadReferencedTexture(contentManager, reference));

        return material;
    }

    /// <summary>The compat-typed counterpart of <c>CNA.Content.ContentManager.LoadReferencedTexture</c>:
    /// the referenced file names its own root reader, so the texture kind is read rather than
    /// guessed. Loading through the compat manager is what puts the texture in this manager's cache
    /// and unload set; the framework texture underneath is what the shared parameter-application
    /// code assigns, since it operates on the framework effect.</summary>
    private static CNA.Graphics.Texture LoadReferencedTexture(Content.ContentManager contentManager, string assetName) =>
        XnbContainer.RootReaderName(contentManager.RootDirectory, assetName) switch
        {
            "Microsoft.Xna.Framework.Content.Texture2DReader" =>
                contentManager.Load<Texture2D>(assetName).FrameworkTexture,
            "Microsoft.Xna.Framework.Content.TextureCubeReader" =>
                contentManager.Load<TextureCube>(assetName).FrameworkTexture,
            var other => throw new Content.ContentLoadException(
                $"External reference '{assetName}' is a {other ?? "null object"}, which this project's " +
                ".xnb reader cannot load as a texture."),
        };

    private static EnvironmentMapEffect BuildEnvironmentMapEffect(
        GraphicsDevice graphicsDevice, Content.ContentManager contentManager, XnbEnvironmentMapEffectData data)
    {
        var effect = new EnvironmentMapEffect(graphicsDevice)
        {
            EnvironmentMapAmount = data.EnvironmentMapAmount,
            EnvironmentMapSpecular = data.EnvironmentMapSpecular.ToCompat(),
            FresnelFactor = data.FresnelFactor,
            DiffuseColor = data.DiffuseColor.ToCompat(),
            EmissiveColor = data.EmissiveColor.ToCompat(),
            Alpha = data.Alpha,
        };

        if (data.TextureReference is { } texture)
        {
            effect.Texture = contentManager.Load<Texture2D>(texture);
        }

        if (data.EnvironmentMapReference is { } environmentMap)
        {
            effect.EnvironmentMap = contentManager.Load<TextureCube>(environmentMap);
        }

        return effect;
    }

    private static DualTextureEffect BuildDualTextureEffect(
        GraphicsDevice graphicsDevice, Content.ContentManager contentManager, XnbDualTextureEffectData data)
    {
        var effect = new DualTextureEffect(graphicsDevice)
        {
            DiffuseColor = data.DiffuseColor.ToCompat(),
            Alpha = data.Alpha,
            VertexColorEnabled = data.VertexColorEnabled,
        };

        if (data.TextureReference is { } texture)
        {
            effect.Texture = contentManager.Load<Texture2D>(texture);
        }

        if (data.Texture2Reference is { } texture2)
        {
            effect.Texture2 = contentManager.Load<Texture2D>(texture2);
        }

        return effect;
    }

    /// <summary>Converts a base <see cref="CNA.Graphics.VertexDeclaration"/> (all
    /// <c>CNA.Content.Xnb</c>/<c>CNA.Content.Cnj</c> ever produce, since neither namespace has any
    /// knowledge of <c>CNA.XnaCompat</c>) into this namespace's own -- element-wise, through
    /// <see cref="VertexElement"/>'s internal conversion methods, the same "arrays of distinct
    /// value types do not convert automatically" reason every
    /// other array conversion in this compat layer needs one. <c>internal</c>, reused by
    /// <see cref="CnjCompatModelBuilder"/> for the same reason <see cref="BuildVertexBuffer"/>/
    /// <see cref="BuildIndexBuffer"/> are.</summary>
    internal static VertexDeclaration ToCompat(CNA.Graphics.VertexDeclaration declaration)
    {
        CNA.Graphics.VertexElement[] source = declaration.GetVertexElements();
        var elements = new VertexElement[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            elements[i] = VertexElement.FromFramework(source[i]);
        }

        return new VertexDeclaration(declaration.VertexStride, elements);
    }
}
