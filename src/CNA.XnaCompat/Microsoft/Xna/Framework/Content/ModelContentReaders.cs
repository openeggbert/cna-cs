namespace Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// The model pipeline, read through the public <see cref="ContentReader"/> protocol.
///
/// <b>Why a second path to a Model.</b> A top-level <c>Load&lt;Model&gt;</c> goes to CNA's own
/// content loader and never comes here. A model nested inside something else does -- a sky box, a
/// game's own model class carrying its meshes and textures together, a skinned model whose
/// animation data wraps the geometry. The survey of the XNA 4.0 sample collection finds three such
/// assets, and each failed entirely: an unresolvable reader anywhere in the table fails the whole
/// file, not just the part that needs it.
///
/// <b>The shared-resource indirection is the load-bearing part.</b> A mesh part references its
/// vertex buffer, index buffer and effect by index into a table written *after* the root object, so
/// the references are registered as fix-ups and resolved once those bytes arrive. Two mesh parts
/// routinely share one buffer by naming the same index, which is the whole reason the format works
/// that way and the reason reading them eagerly is not an option.
///
/// Every format below is transcribed from the decompiled XNA 4.0 readers.
/// </summary>
internal sealed class VertexDeclarationContentReader : ContentTypeReader<VertexDeclaration>
{
    protected internal override VertexDeclaration Read(ContentReader input, VertexDeclaration existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);

        int vertexStride = input.ReadInt32();
        int elementCount = input.ReadInt32();
        if (elementCount is < 0 or > 256)
        {
            throw new ContentLoadException(
                $"Content asset '{input.AssetName}' declares {elementCount} vertex elements.");
        }

        var elements = new VertexElement[elementCount];
        for (int index = 0; index < elementCount; index++)
        {
            elements[index] = new VertexElement(
                input.ReadInt32(),
                (VertexElementFormat)input.ReadInt32(),
                (VertexElementUsage)input.ReadInt32(),
                input.ReadInt32());
        }

        return new VertexDeclaration(vertexStride, elements);
    }
}

/// <summary>See <see cref="VertexDeclarationContentReader"/>. The declaration is read raw -- it has
/// no type-index prefix of its own, because the buffer's reader already knows what follows.</summary>
internal sealed class VertexBufferContentReader : ContentTypeReader<VertexBuffer>
{
    protected internal override VertexBuffer Read(ContentReader input, VertexBuffer existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);

        VertexDeclaration declaration = input.ReadRawObject<VertexDeclaration>();
        int vertexCount = input.ReadInt32();
        int byteCount = vertexCount * declaration.VertexStride;
        byte[] data = ContentTextureLevels.ReadExact(input, byteCount, "vertex buffer");

        var buffer = new VertexBuffer(
            GraphicsContentHelper.GraphicsDeviceFromContentReader(input),
            declaration,
            vertexCount,
            BufferUsage.None);
        buffer.SetData(data, 0, byteCount);
        return buffer;
    }
}

/// <summary>See <see cref="VertexDeclarationContentReader"/>. The leading boolean says sixteen-bit,
/// and the count that follows is in <em>bytes</em>, not indices.</summary>
internal sealed class IndexBufferContentReader : ContentTypeReader<IndexBuffer>
{
    protected internal override IndexBuffer Read(ContentReader input, IndexBuffer existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);

        bool sixteenBits = input.ReadBoolean();
        int byteCount = input.ReadInt32();
        byte[] data = ContentTextureLevels.ReadExact(input, byteCount, "index buffer");

        IndexElementSize elementSize = sixteenBits ? IndexElementSize.SixteenBits : IndexElementSize.ThirtyTwoBits;
        int indexCount = sixteenBits ? byteCount / 2 : byteCount / 4;

        var buffer = new IndexBuffer(
            GraphicsContentHelper.GraphicsDeviceFromContentReader(input),
            elementSize,
            indexCount,
            BufferUsage.None);
        buffer.SetData(data, 0, byteCount);
        return buffer;
    }
}

/// <summary>
/// A texture reference inside a model.
///
/// XNA's own reader returns the existing instance and reads nothing, which looks like a stub and is
/// not one: the texture arrives through the shared-resource or external-reference machinery, and
/// this reader exists only so the type has an entry in the table.
/// </summary>
internal sealed class AbstractTextureContentReader : ContentTypeReader<Texture>
{
    protected internal override Texture Read(ContentReader input, Texture existingInstance) => existingInstance;
}

/// <summary>Compiled effect bytecode. Whether the renderer can consume it is the renderer's
/// answer -- <c>GraphicsCapability.CompiledEffects</c> is the query for asking first.</summary>
internal sealed class EffectContentReader : ContentTypeReader<Effect>
{
    protected internal override Effect Read(ContentReader input, Effect existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);

        int byteCount = input.ReadInt32();
        byte[] bytecode = ContentTextureLevels.ReadExact(input, byteCount, "effect bytecode");
        return new Effect(GraphicsContentHelper.GraphicsDeviceFromContentReader(input), bytecode);
    }
}

/// <summary>
/// The stock <c>BasicEffect</c> a model's mesh parts usually carry.
///
/// XNA clones one shared effect per device rather than constructing a new one each time. This
/// constructs one each time instead: cloning exists to share compiled shader state, and here every
/// <c>BasicEffect</c> is the same native stock effect already. The observable difference is that
/// two mesh parts get two effect objects, which is what a caller mutating one of them expects
/// anyway.
/// </summary>
internal sealed class BasicEffectContentReader : ContentTypeReader<BasicEffect>
{
    protected internal override BasicEffect Read(ContentReader input, BasicEffect existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);

        var effect = new BasicEffect(GraphicsContentHelper.GraphicsDeviceFromContentReader(input));

        if (input.ReadExternalReference<Texture>() is Texture2D texture)
        {
            effect.Texture = texture;
            effect.TextureEnabled = true;
        }

        effect.DiffuseColor = input.ReadVector3();
        effect.EmissiveColor = input.ReadVector3();
        effect.SpecularColor = input.ReadVector3();
        effect.SpecularPower = input.ReadSingle();
        effect.Alpha = input.ReadSingle();
        effect.VertexColorEnabled = input.ReadBoolean();
        return effect;
    }
}

/// <summary>See <see cref="VertexDeclarationContentReader"/> for what this path is for.</summary>
internal sealed class ModelContentReader : ContentTypeReader<Model>
{
    protected internal override Model Read(ContentReader input, Model existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);

        GraphicsDevice device = GraphicsContentHelper.GraphicsDeviceFromContentReader(input);

        ModelBone[] bones = ReadBones(input);
        (ModelMesh[] meshes, ModelBone?[] meshParents) = ReadMeshes(input, device, bones);
        ModelBone? root = ReadBoneReference(input, bones);
        object? tag = input.ReadObject<object>();

        // The facade's constructor wants a parent bone per mesh or none at all, and takes the root
        // by index rather than by reference.
        ModelBone[] parents = meshParents.All(static bone => bone is not null)
            ? [.. meshParents.Select(static bone => bone!)]
            : [];

        var model = new Model(device, bones, meshes, parents, root?.Index ?? 0)
        {
            Tag = tag,
        };

        return model;
    }

    private static ModelBone[] ReadBones(ContentReader input)
    {
        int count = input.ReadInt32();
        if (count is < 0 or > 100_000)
        {
            throw new ContentLoadException($"Content asset '{input.AssetName}' declares {count} bones.");
        }

        var bones = new ModelBone[count];
        for (int index = 0; index < count; index++)
        {
            string name = input.ReadObject<string>();
            Matrix transform = input.ReadMatrix();
            bones[index] = new ModelBone(index, name) { Transform = transform };
        }

        // The hierarchy is a second pass, because a bone can name a parent that has not been read
        // yet.
        foreach (ModelBone bone in bones)
        {
            _ = ReadBoneReference(input, bones);   // the parent, established by the child's AddChild
            int childCount = input.ReadInt32();
            if (childCount is < 0 or > 100_000)
            {
                throw new ContentLoadException(
                    $"Content asset '{input.AssetName}' declares {childCount} children for bone '{bone.Name}'.");
            }

            for (int index = 0; index < childCount; index++)
            {
                if (ReadBoneReference(input, bones) is { } child)
                {
                    bone.AddChild(child);
                }
            }
        }

        return bones;
    }

    private static (ModelMesh[] Meshes, ModelBone?[] Parents) ReadMeshes(
        ContentReader input, GraphicsDevice device, ModelBone[] bones)
    {
        int count = input.ReadInt32();
        if (count is < 0 or > 100_000)
        {
            throw new ContentLoadException($"Content asset '{input.AssetName}' declares {count} meshes.");
        }

        var meshes = new ModelMesh[count];
        var parents = new ModelBone?[count];
        for (int index = 0; index < count; index++)
        {
            string name = input.ReadObject<string>();
            parents[index] = ReadBoneReference(input, bones);
            var boundingSphere = new BoundingSphere(input.ReadVector3(), input.ReadSingle());
            object? tag = input.ReadObject<object>();
            ModelMeshPart[] parts = ReadMeshParts(input);

            var mesh = new ModelMesh(device, name, parts) { Tag = tag };
            mesh.SetBoundingSphere(boundingSphere);
            meshes[index] = mesh;
        }

        return (meshes, parents);
    }

    private static ModelMeshPart[] ReadMeshParts(ContentReader input)
    {
        int count = input.ReadInt32();
        if (count is < 0 or > 100_000)
        {
            throw new ContentLoadException($"Content asset '{input.AssetName}' declares {count} mesh parts.");
        }

        var parts = new ModelMeshPart[count];
        for (int index = 0; index < count; index++)
        {
            var part = new ModelMeshPart();
            part.SetVertexOffset(input.ReadInt32());
            part.SetNumVertices(input.ReadInt32());
            part.SetStartIndex(input.ReadInt32());
            part.SetPrimitiveCount(input.ReadInt32());
            part.Tag = input.ReadObject<object>();
            parts[index] = part;

            // Deferred, and each closure must capture its own part: the buffers live in the shared
            // resource table after the root object, and two parts commonly name the same one.
            ModelMeshPart captured = part;
            input.ReadSharedResource<VertexBuffer>(buffer => captured.SetVertexBuffer(buffer));
            input.ReadSharedResource<IndexBuffer>(buffer => captured.SetIndexBuffer(buffer));
            input.ReadSharedResource<Effect>(effect => captured.Effect = effect);
        }

        return parts;
    }

    /// <summary>A bone reference: one byte while the table is small enough, otherwise a full
    /// <c>int32</c>. Zero means no bone; a real reference is stored one-based.</summary>
    private static ModelBone? ReadBoneReference(ContentReader input, ModelBone[] bones)
    {
        int raw = bones.Length + 1 > 255 ? input.ReadInt32() : input.ReadByte();
        if (raw == 0)
        {
            return null;
        }

        if (raw < 1 || raw > bones.Length)
        {
            throw new ContentLoadException(
                $"Content asset '{input.AssetName}' references bone {raw}, but it declares {bones.Length}.");
        }

        return bones[raw - 1];
    }
}
