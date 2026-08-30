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
    internal static Model Build(GraphicsDevice graphicsDevice, XnbModelData data, ContentManager contentManager)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(contentManager);

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

                parts.Add(new ModelMeshPart(vertexBuffer, indexBuffer, partData.NumVertices, partData.PrimitiveCount, partData.StartIndex, partData.VertexOffset)
                {
                    Tag = partData.Tag,
                });
            }

            var mesh = new ModelMesh(graphicsDevice, meshData.Name, parts)
            {
                BoundingSphere = meshData.BoundingSphere,
                Tag = meshData.Tag,
            };
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
                XnbEffectData? effectData = meshData.Parts[i].Effect;
                if (effectData is not null)
                {
                    parts[i].Effect = BuildEffect(graphicsDevice, contentManager, effectData);
                    parts[i].MarkResourcesOwned();
                }
            }

            // -1 ("no parent bone") falls back to bone 0, matching Model's own 4-argument
            // constructor's existing rootBoneIndex leniency (see Model.cs) -- a mesh genuinely has
            // to hang off *some* bone for CopyAbsoluteBoneTransformsTo to make sense of it.
            int parentIndex = meshData.ParentBoneIndex >= 0 ? meshData.ParentBoneIndex : 0;
            meshParentBones.Add(bones[parentIndex]);
        }

        int rootBoneIndex = data.RootBoneIndex >= 0 ? data.RootBoneIndex : 0;
        return new Model(graphicsDevice, bones, meshes, meshParentBones, rootBoneIndex) { Tag = data.Tag };
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

    /// <summary>
    /// Builds the real effect a mesh part's parsed effect data describes, resolving its external
    /// references through <paramref name="contentManager"/>.
    ///
    /// Resolution happens here rather than in the reader because this is where a
    /// <see cref="ContentManager"/> and a <see cref="GraphicsDevice"/> both exist -- see
    /// <see cref="XnbContentReader.ReadExternalReference"/>. Nothing is caught: a model naming a
    /// texture that is not there is a broken model, and loading it with a null texture would draw
    /// wrongly instead of reporting the missing file.
    /// </summary>
    private static Effect BuildEffect(GraphicsDevice graphicsDevice, ContentManager contentManager, XnbEffectData data) =>
        data switch
        {
            XnbBasicEffectData basic => BuildBasicEffect(graphicsDevice, contentManager, basic),
            XnbEffectMaterialData material => BuildEffectMaterial(contentManager, material),
            XnbEnvironmentMapEffectData environmentMap => BuildEnvironmentMapEffect(graphicsDevice, contentManager, environmentMap),
            XnbDualTextureEffectData dualTexture => BuildDualTextureEffect(graphicsDevice, contentManager, dualTexture),
            _ => throw new ContentLoadException($"Unsupported model effect data {data.GetType().Name}."),
        };

    private static BasicEffect BuildBasicEffect(GraphicsDevice graphicsDevice, ContentManager contentManager, XnbBasicEffectData data)
    {
        var effect = new BasicEffect(graphicsDevice);
        ApplyBasicEffectData(effect, data);

        if (data.TextureReference is { } reference)
        {
            // TextureEnabled follows from having a texture, which is what XNA's own
            // BasicEffectReader does (it assigns Texture, and BasicEffect.Texture's setter is what
            // the effect consults). Setting it true with no texture, or leaving it false with one,
            // are both visible on screen.
            effect.Texture = contentManager.Load<Texture2D>(reference);
            effect.TextureEnabled = true;
        }

        return effect;
    }

    /// <summary>
    /// XNA's <c>EffectMaterialReader</c>: clone the referenced effect, then apply the recorded
    /// parameters. The clone is what makes two materials over one effect independent.
    /// </summary>
    private static EffectMaterial BuildEffectMaterial(ContentManager contentManager, XnbEffectMaterialData data)
    {
        if (data.EffectReference is not { } reference)
        {
            throw new ContentLoadException(
                "This .xnb file's EffectMaterial names no effect to clone, which XNA's own reader has no defined behaviour for.");
        }

        var material = new EffectMaterial(contentManager.Load<Effect>(reference));
        ApplyEffectParameters(material, data.Parameters, contentManager.LoadReferencedTexture);
        return material;
    }

    private static EnvironmentMapEffect BuildEnvironmentMapEffect(
        GraphicsDevice graphicsDevice, ContentManager contentManager, XnbEnvironmentMapEffectData data)
    {
        var effect = new EnvironmentMapEffect(graphicsDevice)
        {
            EnvironmentMapAmount = data.EnvironmentMapAmount,
            EnvironmentMapSpecular = data.EnvironmentMapSpecular,
            FresnelFactor = data.FresnelFactor,
            DiffuseColor = data.DiffuseColor,
            EmissiveColor = data.EmissiveColor,
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
        GraphicsDevice graphicsDevice, ContentManager contentManager, XnbDualTextureEffectData data)
    {
        var effect = new DualTextureEffect(graphicsDevice)
        {
            DiffuseColor = data.DiffuseColor,
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

    /// <summary>
    /// XNA's <c>EffectMaterialReader.TryToSetParameter</c>, over this assembly's own
    /// <see cref="EffectParameter"/>. <c>CNA.XnaCompat</c>'s model builder calls this too, on the
    /// framework effect its compat effect wraps -- there is one native effect underneath either
    /// spelling, so the parameter values only have to be applied once and in one place.
    ///
    /// <b>A parameter the effect does not have is skipped, not an error.</b> That is XNA's rule and
    /// it is load-bearing: the pipeline records what the material declared, and the effect it clones
    /// is free to have dropped a parameter since.
    ///
    /// <b>The widening path is taken up front</b> rather than as a fallback. XNA sets the value
    /// directly and catches <see cref="InvalidCastException"/> to retry through a widened
    /// <see cref="Vector4"/>; that control flow cannot be reproduced, because a shape mismatch here
    /// raises a <c>CnaException</c> from native rather than XNA's own exception, and catching that
    /// broadly would swallow real failures. For these types the widening is the identity when the
    /// shapes already agree -- a <see cref="Vector3"/> into a three-column parameter widens to
    /// <c>Vector4(v, 1)</c> and narrows back to exactly <c>v</c> -- so the two routes agree wherever
    /// XNA would not have thrown.
    /// </summary>
    internal static void ApplyEffectParameters(
        Effect effect,
        IReadOnlyDictionary<string, object?> parameters,
        Func<string, Texture> resolveTexture)
    {
        ArgumentNullException.ThrowIfNull(resolveTexture);

        foreach ((string name, object? value) in parameters)
        {
            if (value is null || effect.Parameters[name] is not { } parameter)
            {
                continue;
            }

            if (IsAVectorOrASingle(parameter) && AsVector4(value) is { } widened)
            {
                switch (parameter.ColumnCount)
                {
                    case 1: parameter.SetValue(widened.X); break;
                    case 2: parameter.SetValue(new Vector2(widened.X, widened.Y)); break;
                    case 3: parameter.SetValue(new Vector3(widened.X, widened.Y, widened.Z)); break;
                    default: parameter.SetValue(widened); break;
                }

                continue;
            }

            switch (value)
            {
                case int[] values: parameter.SetValue(values); break;
                case bool[] values: parameter.SetValue(values); break;
                case float[] values: parameter.SetValue(values); break;
                case Vector2[] values: parameter.SetValue(values); break;
                case Vector3[] values: parameter.SetValue(values); break;
                case Vector4[] values: parameter.SetValue(values); break;
                case Matrix[] values: parameter.SetValue(values); break;
                case int scalar: parameter.SetValue(scalar); break;
                case bool scalar: parameter.SetValue(scalar); break;
                case float scalar: parameter.SetValue(scalar); break;
                case Vector2 vector: parameter.SetValue(vector); break;
                case Vector3 vector: parameter.SetValue(vector); break;
                case Vector4 vector: parameter.SetValue(vector); break;
                case Matrix matrix: parameter.SetValue(matrix); break;
                case Texture texture: parameter.SetValue(texture); break;

                // A texture parameter arrives as the ExternalReference the pipeline wrote, which
                // this layer resolved to an asset name and left unloaded. XNA had already loaded it
                // by this point, so its own chain sees a Texture here; the load happens here
                // instead, which is the same place every other reference in this file resolves.
                case XnbExternalReference reference: parameter.SetValue(resolveTexture(reference.AssetName)); break;
                case string text: parameter.SetValue(text); break;

                // XNA's chain ends without an else, so an unrecognised type is left alone rather
                // than reported. A material carrying one is not a broken material.
                default: break;
            }
        }
    }

    /// <summary>XNA's <c>IsAVectorOrASingle</c>: a non-array vector of two to four columns, or a
    /// non-array scalar.</summary>
    private static bool IsAVectorOrASingle(EffectParameter parameter) =>
        parameter.Elements.Count == 0 &&
        parameter.RowCount == 1 &&
        ((parameter.ParameterClass == EffectParameterClass.Vector &&
          parameter.ColumnCount is >= 2 and <= 4) ||
         (parameter.ParameterClass == EffectParameterClass.Scalar &&
          parameter.ColumnCount == 1));

    private static Vector4? AsVector4(object value) => value switch
    {
        // Spelled out rather than new Vector4(vector, 0f, 1f): CNA.Vector4 has no
        // (Vector2, float, float) constructor, though Microsoft.Xna.Framework.Vector4 does.
        Vector2 vector => new Vector4(vector.X, vector.Y, 0f, 1f),
        Vector3 vector => new Vector4(vector, 1f),
        Vector4 vector => vector,
        _ => null,
    };

    /// <summary>Applies every field <see cref="XnbBasicEffectData"/> actually carries -- everything
    /// except <see cref="XnbBasicEffectData.TextureReference"/>, which stays unresolved (see that
    /// type's own doc comment for why: resolving it needs <c>ContentManager.Load&lt;Texture2D&gt;()</c>,
    /// itself native-ABI-blocked). <see cref="BasicEffect.TextureEnabled"/> is left at its default
    /// (<see langword="false"/>) rather than set <see langword="true"/> with no actual texture,
    /// which would be a real, misleading divergence from the source asset -- not a full
    /// reproduction of it, but an honest one. <c>internal</c> (a code-review finding), so
    /// <c>CNA.XnaCompat</c>'s own model builder can apply the same fields to a compat-typed
    /// <see cref="BasicEffect"/> too (compat <c>BasicEffect</c> subclasses this one directly, so it
    /// upcasts fine as this method's parameter) -- unlike <c>BuildVertexBuffer</c>/
    /// <c>BuildIndexBuffer</c>, whose *constructor* signatures genuinely differ across the compat
    /// boundary (a different <c>VertexDeclaration</c>/enum type each), this field-assignment logic
    /// has no such difference, so there was no reason to accept the duplication here the way the
    /// rest of this class's own doc comment explains for the bigger, genuinely-irreducible
    /// duplication in <see cref="Build"/> itself.</summary>
    internal static void ApplyBasicEffectData(BasicEffect effect, XnbBasicEffectData data)
    {
        effect.DiffuseColor = data.DiffuseColor;
        effect.EmissiveColor = data.EmissiveColor;
        effect.SpecularColor = data.SpecularColor;
        effect.SpecularPower = data.SpecularPower;
        effect.Alpha = data.Alpha;
        effect.VertexColorEnabled = data.VertexColorEnabled;
    }
}
