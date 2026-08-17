using CNA.Content.Xnb;
using CNA.Graphics;

namespace CNA.Content.Cnj;

/// <summary>
/// The fully-parsed, native-ABI-free intermediate result of reading a real <c>.cnj</c>
/// <c>Model</c> document -- the same "return raw pieces, let <see cref="CnjModelBuilder"/> build the
/// native-backed object" split <see cref="XnbModelData"/> already established for the <c>.xnb</c>
/// path (see that type's own doc comment for why this split exists). Deliberately has no
/// <c>Bones</c>/<c>RootBoneIndex</c> list the way <see cref="XnbModelData"/> does: this minimal
/// reader's scope excludes the <c>.cnj</c> <c>"bones"</c> hierarchy entirely (a <c>"bones"</c> array
/// with more than one entry is rejected outright by <see cref="CnjModelReader"/>), so every mesh's
/// bone is synthesized fresh by <see cref="CnjModelBuilder"/> instead of read from the file -- see
/// that type's own doc comment.
/// </summary>
internal sealed class CnjModelData
{
    internal CnjModelData(IReadOnlyList<CnjMeshData> meshes)
    {
        Meshes = meshes;
    }

    internal IReadOnlyList<CnjMeshData> Meshes { get; }
}

/// <summary>One mesh read from a <c>.cnj</c> document's <c>"meshes"</c> array. Unlike <c>.xnb</c>'s
/// <c>ModelReader</c>, a <c>.cnj</c> mesh is always exactly one <see cref="ModelMeshPart"/> -- the
/// format has no concept of splitting a mesh into multiple parts.</summary>
internal sealed class CnjMeshData
{
    internal CnjMeshData(string name, XnbVertexBufferData vertexBuffer, XnbIndexBufferData indexBuffer, int primitiveCount, CnjBasicEffectData effect)
    {
        Name = name;
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        PrimitiveCount = primitiveCount;
        Effect = effect;
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
