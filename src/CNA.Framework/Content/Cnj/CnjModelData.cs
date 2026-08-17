using CNA.Content.Xnb;
using CNA.Graphics;

namespace CNA.Content.Cnj;

/// <summary>
/// The fully-parsed, native-ABI-free intermediate result of reading a real <c>.cnj</c>
/// <c>Model</c> document -- the same "return raw pieces, let <see cref="CnjModelBuilder"/> build the
/// native-backed object" split <see cref="XnbModelData"/> already established for the <c>.xnb</c>
/// path (see that type's own doc comment for why this split exists). <see cref="Bones"/> is empty
/// unless the document has a real, multi-entry <c>"bones"</c> hierarchy (cnjVersion 2) -- a
/// <c>"bones"</c> array with 0 or 1 entries is the cnjVersion-1-compatible "no real hierarchy" case
/// (matching the real engine's own <c>hasBoneHierarchy = cnjBones.size() &gt; 1</c> check), so
/// <see cref="CnjModelBuilder"/> still synthesizes one root bone plus one child bone per mesh in that
/// case, exactly like before this bone-hierarchy increment landed.
/// </summary>
internal sealed class CnjModelData
{
    internal CnjModelData(IReadOnlyList<CnjBoneData> bones, IReadOnlyList<CnjMeshData> meshes)
    {
        Bones = bones;
        Meshes = meshes;
    }

    internal IReadOnlyList<CnjBoneData> Bones { get; }

    internal IReadOnlyList<CnjMeshData> Meshes { get; }
}

/// <summary>One bone read from a <c>.cnj</c> document's real, multi-entry <c>"bones"</c> array
/// (cnjVersion 2) -- a flat, parent-before-child scene graph: entry 0 is always the root (its own
/// <see cref="Parent"/> value, if present, is unused -- there is no earlier entry for it to
/// reference), every later entry's <see cref="Parent"/> is a 0-based index into this same list,
/// always referring to an already-constructed earlier entry. Unlike <c>.xnb</c>'s own
/// <see cref="XnbBoneData"/> (which encodes each bone's *children* as an index list, needing a
/// second pass once every bone exists), <c>.cnj</c> encodes each bone's own *parent* instead, so
/// <see cref="CnjModelBuilder"/> can link the whole tree in one forward pass.</summary>
internal sealed class CnjBoneData
{
    internal CnjBoneData(string name, int parent, Matrix transform)
    {
        Name = name;
        Parent = parent;
        Transform = transform;
    }

    internal string Name { get; }

    internal int Parent { get; }

    internal Matrix Transform { get; }
}

/// <summary>One mesh read from a <c>.cnj</c> document's <c>"meshes"</c> array. Unlike <c>.xnb</c>'s
/// <c>ModelReader</c>, a <c>.cnj</c> mesh is always exactly one <see cref="ModelMeshPart"/> -- the
/// format has no concept of splitting a mesh into multiple parts.</summary>
internal sealed class CnjMeshData
{
    internal CnjMeshData(string name, XnbVertexBufferData vertexBuffer, XnbIndexBufferData indexBuffer, int primitiveCount, CnjBasicEffectData effect, int? parentBoneIndex)
    {
        Name = name;
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        PrimitiveCount = primitiveCount;
        Effect = effect;
        ParentBoneIndex = parentBoneIndex;
    }

    internal string Name { get; }

    /// <summary>Reuses <c>CNA.Content.Xnb</c>'s own vertex-buffer data shape verbatim -- a
    /// <see cref="Graphics.VertexDeclaration"/> plus a vertex count plus raw bytes is exactly what a
    /// <c>.cnj</c> vertex sidecar file's contents are too (see <see cref="CnjModelReader"/>'s own doc
    /// comment for the byte-layout grounding), so a second, format-specific type here would be a
    /// distinction with no actual difference.</summary>
    internal XnbVertexBufferData VertexBuffer { get; }

    /// <summary>Same reuse reasoning as <see cref="VertexBuffer"/> -- a <c>.cnj</c> index sidecar
    /// file's contents are exactly "sixteen- or thirty-two-bit indices, raw bytes," identical in
    /// shape to <c>.xnb</c>'s own <see cref="XnbIndexBufferData"/>.</summary>
    internal XnbIndexBufferData IndexBuffer { get; }

    internal int PrimitiveCount { get; }

    internal CnjBasicEffectData Effect { get; }

    /// <summary>0-based index into <see cref="CnjModelData.Bones"/> this mesh is parented to (from
    /// its own <c>"parentBone"</c> field, default 0), or <see langword="null"/> when the document has
    /// no real bone hierarchy (<see cref="CnjModelData.Bones"/> is empty) -- in that case
    /// <see cref="CnjModelBuilder"/> falls back to synthesizing a fresh child bone per mesh instead,
    /// matching this reader's own pre-existing behavior.</summary>
    internal int? ParentBoneIndex { get; }
}

/// <summary>
/// The fully-parsed <c>.cnj</c> mesh entry's <c>"effect"</c> data -- deliberately a *much* smaller
/// field set than <see cref="XnbBasicEffectData"/>: a real, load-bearing finding from this feature's
/// own format research is that <c>.cnj</c>'s <c>BasicEffect</c> JSON has **no material-color fields
/// at all** (no <c>diffuseColor</c>/<c>specularColor</c>/<c>alpha</c>/<c>specularPower</c>, unlike
/// <c>.xnb</c>'s own <c>BasicEffectReader</c>) -- only <c>texture</c>/<c>vertexColorEnabled</c> are
/// ever read from a <c>.cnj</c> mesh's <c>"effect"</c> data, so <see cref="CnjModelBuilder"/> does
/// **not** reuse <see cref="XnbModelBuilder.ApplyBasicEffectData"/>, which applies a field set this
/// format simply doesn't have.
/// </summary>
internal sealed class CnjBasicEffectData
{
    internal CnjBasicEffectData(string? textureReference, bool vertexColorEnabled)
    {
        TextureReference = textureReference;
        VertexColorEnabled = vertexColorEnabled;
    }

    /// <summary>The resolved, containment-validated (see <see cref="CnjPathContainment"/>) sidecar
    /// path for this mesh's <c>"texture"</c> field, or <see langword="null"/> if that field was
    /// absent/empty. Recorded exactly as resolved rather than actually loaded, matching
    /// <see cref="XnbBasicEffectData.TextureReference"/>'s own deliberate deferral: resolving it into
    /// a real <see cref="Texture2D"/> needs <see cref="ContentManager.Load{T}"/>'s own
    /// <see cref="Texture2D"/> case, which resolves by *asset name* under
    /// <see cref="ContentManager.RootDirectory"/> (native-side extension probing), not by an
    /// already-resolved absolute file path the way this field is validated here -- bridging that gap
    /// would need real, non-trivial new logic for no payoff beyond this project's own native-ABI-blocked
    /// <see cref="Texture2D"/> loading, which can't actually run yet regardless (see
    /// <see cref="ContentManager.LoadNativeTexture2DHandle"/>). <see cref="CnjModelBuilder"/>
    /// therefore leaves <see cref="Graphics.BasicEffect.Texture"/>/<see cref="Graphics.BasicEffect.TextureEnabled"/>
    /// at their constructor defaults even when this is non-null, the same "honest, not a full
    /// reproduction" choice <see cref="XnbModelBuilder.ApplyBasicEffectData"/>'s own doc comment
    /// already made.</summary>
    internal string? TextureReference { get; }

    internal bool VertexColorEnabled { get; }
}
