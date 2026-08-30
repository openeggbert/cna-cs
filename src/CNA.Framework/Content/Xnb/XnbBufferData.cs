using CNA.Graphics;

namespace CNA.Content.Xnb;

/// <summary>The fully-parsed, native-ABI-free result of reading a real <c>.xnb</c>
/// <c>VertexBufferReader</c> object graph -- raw vertex bytes plus the <see cref="VertexDeclaration"/>
/// describing them, not yet a real, native-backed <see cref="VertexBuffer"/> (see
/// <see cref="XnbModelData"/>'s own doc comment for why this split exists). <see cref="Data"/>'s
/// length is always exactly <see cref="VertexCount"/> times the declaration's vertex stride --
/// enforced here, not just documented, since a code-review finding (during <c>CNA.Content.Cnj</c>'s
/// own reuse of this type) pointed out that a doc-comment-only invariant relies on every caller
/// remembering to uphold it; this constructor makes violating it impossible instead.</summary>
internal sealed class XnbVertexBufferData
{
    internal XnbVertexBufferData(VertexDeclaration declaration, int vertexCount, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(vertexCount);
        long expectedLength = (long)vertexCount * declaration.VertexStride;
        if (data.Length != expectedLength)
        {
            throw new ArgumentException(
                $"{nameof(data)}.Length ({data.Length}) must equal {nameof(vertexCount)} ({vertexCount}) * VertexStride ({declaration.VertexStride}) = {expectedLength}.",
                nameof(data));
        }

        Declaration = declaration;
        VertexCount = vertexCount;
        Data = data;
    }

    internal VertexDeclaration Declaration { get; }

    internal int VertexCount { get; }

    internal byte[] Data { get; }
}

/// <summary>Same rationale as <see cref="XnbVertexBufferData"/>, for a real <c>.xnb</c>
/// <c>IndexBufferReader</c> object graph. <see cref="Data"/>'s length is always a whole multiple of
/// the index element size (2 bytes for <see cref="SixteenBits"/>, 4 otherwise) -- enforced here for
/// the same reason as <see cref="XnbVertexBufferData"/>'s own constructor check.</summary>
internal sealed class XnbIndexBufferData
{
    internal XnbIndexBufferData(bool sixteenBits, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        int elementSize = sixteenBits ? 2 : 4;
        if (data.Length % elementSize != 0)
        {
            throw new ArgumentException(
                $"{nameof(data)}.Length ({data.Length}) must be a whole multiple of the {elementSize}-byte index element size.",
                nameof(data));
        }

        SixteenBits = sixteenBits;
        Data = data;
    }

    internal bool SixteenBits { get; }

    internal byte[] Data { get; }
}

/// <summary>The fully-parsed, native-ABI-free result of reading a real <c>.xnb</c>
/// <c>BasicEffectReader</c> object graph. <see cref="TextureReference"/> is the **resolved asset
/// name** of the effect's texture, or <see langword="null"/> when the file named none; the texture
/// itself is loaded by the model builder, which is where a <c>ContentManager</c> and a graphics
/// device both exist. This used to hold the raw relative string and go no further, so every
/// textured model in the corpus built a <c>BasicEffect</c> with no texture and
/// <c>TextureEnabled</c> false -- it loaded, and drew untextured.</summary>
internal sealed class XnbBasicEffectData : XnbEffectData
{
    internal XnbBasicEffectData(
        string? textureReference,
        Vector3 diffuseColor,
        Vector3 emissiveColor,
        Vector3 specularColor,
        float specularPower,
        float alpha,
        bool vertexColorEnabled)
    {
        TextureReference = textureReference;
        DiffuseColor = diffuseColor;
        EmissiveColor = emissiveColor;
        SpecularColor = specularColor;
        SpecularPower = specularPower;
        Alpha = alpha;
        VertexColorEnabled = vertexColorEnabled;
    }

    internal string? TextureReference { get; }

    internal Vector3 DiffuseColor { get; }

    internal Vector3 EmissiveColor { get; }

    internal Vector3 SpecularColor { get; }

    internal float SpecularPower { get; }

    internal float Alpha { get; }

    internal bool VertexColorEnabled { get; }
}
