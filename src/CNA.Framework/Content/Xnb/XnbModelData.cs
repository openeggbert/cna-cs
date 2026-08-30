using CNA.Graphics;

namespace CNA.Content.Xnb;

/// <summary>
/// The fully-parsed, native-ABI-free intermediate result of reading a real <c>.xnb</c>
/// <c>ModelReader</c> object graph -- deliberately *not* a real <see cref="Model"/> yet, since
/// building one requires real, native-backed <see cref="VertexBuffer"/>/<see cref="IndexBuffer"/>
/// instances (see <see cref="ContentManager.Load{T}"/>'s own doc comment for why this split
/// exists: it's the same "return raw pieces, let the caller build the native-backed object" split
/// <see cref="ContentManager.SpriteFontData"/> already uses for <c>SpriteFont</c>). Everything in
/// this file and its siblings (<c>XnbBoneData</c>/<c>XnbMeshData</c>/<c>XnbMeshPartData</c>) is
/// pure data with no native dependency at all, and so is fully unit-testable without a real
/// <c>cna-native</c> -- a genuine rarity for this project's content-loading surface.
/// </summary>
internal sealed class XnbModelData
{
    internal XnbModelData(
        IReadOnlyList<XnbBoneData> bones,
        IReadOnlyList<XnbMeshData> meshes,
        int rootBoneIndex,
        object? tag)
    {
        Bones = bones;
        Meshes = meshes;
        RootBoneIndex = rootBoneIndex;
        Tag = tag;
    }

    /// <summary>Whatever the file's <c>Tag</c> slot held -- see
    /// <see cref="XnbContentReader.ReadTag"/> for why this is carried rather than refused.</summary>
    internal object? Tag { get; }

    internal IReadOnlyList<XnbBoneData> Bones { get; }

    internal IReadOnlyList<XnbMeshData> Meshes { get; }

    /// <summary>-1-based (matches <see cref="XnbContentReader.ReadBoneReference"/>'s "no bone"
    /// convention) -- an explicit -1 here is treated as "use bone 0" by
    /// <see cref="XnbModelBuilder"/>, matching this project's own existing
    /// <see cref="Model"/>(GraphicsDevice,IReadOnlyList{ModelBone},IReadOnlyList{ModelMesh},IReadOnlyList{ModelBone},int)
    /// constructor's own default-to-0 leniency (see <c>Model.cs</c>).</summary>
    internal int RootBoneIndex { get; }
}

/// <summary>One bone read by <c>ModelReader</c>: a name, a transform, and the *indices* of its
/// children (resolved to real <see cref="ModelBone"/> instances by <see cref="XnbModelBuilder"/>
/// once every bone has been read -- children can reference a bone by an index that hasn't been
/// constructed yet at the point it's read, so index-based linking here, object-based linking only
/// after the full list exists, mirrors the real reader's own two-pass bones-then-hierarchy
/// shape).</summary>
internal sealed class XnbBoneData
{
    internal XnbBoneData(int index, string? name, Matrix transform)
    {
        Index = index;
        Name = name;
        Transform = transform;
    }

    internal int Index { get; }

    internal string? Name { get; }

    internal Matrix Transform { get; }

    internal List<int> ChildIndices { get; } = [];
}

/// <summary>One mesh read by <c>ModelReader</c>.</summary>
internal sealed class XnbMeshData
{
    internal XnbMeshData(
        string? name,
        int parentBoneIndex,
        BoundingSphere boundingSphere,
        IReadOnlyList<XnbMeshPartData> parts,
        object? tag)
    {
        Name = name;
        ParentBoneIndex = parentBoneIndex;
        BoundingSphere = boundingSphere;
        Parts = parts;
        Tag = tag;
    }

    internal object? Tag { get; }

    internal string? Name { get; }

    /// <summary>-1 means no parent bone -- see <see cref="XnbModelData.RootBoneIndex"/>'s own doc
    /// comment for the same convention.</summary>
    internal int ParentBoneIndex { get; }

    internal BoundingSphere BoundingSphere { get; }

    internal IReadOnlyList<XnbMeshPartData> Parts { get; }
}

/// <summary>
/// One mesh part read by <c>ModelReader</c>. <see cref="VertexBuffer"/>/<see cref="IndexBuffer"/>/
/// <see cref="Effect"/> start <see langword="null"/> and are populated *after* <c>ModelReader</c>
/// itself returns -- they're real <c>.xnb</c> shared resources (see
/// <see cref="XnbContentReader"/>'s own doc comment for the two-pass "read every shared resource,
/// then run every fixup" mechanism this relies on), so their actual bytes appear later in the file
/// than the mesh part that references them. Mutable (not a positional record) specifically so the
/// deferred fixup closures registered while reading this part can write into it once its shared
/// resources are actually read.
/// </summary>
internal sealed class XnbMeshPartData
{
    internal required int VertexOffset { get; init; }

    internal required int NumVertices { get; init; }

    internal required int StartIndex { get; init; }

    internal required int PrimitiveCount { get; init; }

    internal object? Tag { get; init; }

    internal XnbVertexBufferData? VertexBuffer { get; set; }

    internal XnbIndexBufferData? IndexBuffer { get; set; }

    /// <summary>Any of the four stock effect shapes -- see <see cref="XnbEffectData"/>. It used to
    /// be <c>XnbBasicEffectData</c> alone, which made a model whose part named any other stock
    /// effect fail at the shared-resource type check.</summary>
    internal XnbEffectData? Effect { get; set; }
}
