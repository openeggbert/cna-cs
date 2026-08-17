using CNA.Graphics;

namespace CNA.Content.Xnb;

/// <summary>The fully-parsed, native-ABI-free result of reading a real <c>.xnb</c>
/// <c>VertexBufferReader</c> object graph -- raw vertex bytes plus the <see cref="VertexDeclaration"/>
/// describing them, not yet a real, native-backed <see cref="VertexBuffer"/> (see
/// <see cref="XnbModelData"/>'s own doc comment for why this split exists).</summary>
internal sealed class XnbVertexBufferData
{
    internal XnbVertexBufferData(VertexDeclaration declaration, int vertexCount, byte[] data)
    {
        Declaration = declaration;
        VertexCount = vertexCount;
        Data = data;
    }

    internal VertexDeclaration Declaration { get; }

    internal int VertexCount { get; }

    internal byte[] Data { get; }
}

/// <summary>Same rationale as <see cref="XnbVertexBufferData"/>, for a real <c>.xnb</c>
/// <c>IndexBufferReader</c> object graph.</summary>
internal sealed class XnbIndexBufferData
{
    internal XnbIndexBufferData(bool sixteenBits, byte[] data)
    {
        SixteenBits = sixteenBits;
        Data = data;
    }

    internal bool SixteenBits { get; }

    internal byte[] Data { get; }
}

/// <summary>The fully-parsed, native-ABI-free result of reading a real <c>.xnb</c>
/// <c>BasicEffectReader</c> object graph. Deliberately minimal, matching this feature's own scope
/// decision (see <c>XnbBasicEffectReader</c>'s own doc comment): every field <c>BasicEffectReader</c>
/// actually serializes is read (so the stream position stays correct for whatever follows), but
/// <see cref="TextureReference"/> is recorded exactly as read (the raw relative-path string, or
/// <see langword="null"/> for "no texture") rather than resolved or loaded -- real XNA's own
/// <c>ReadExternalReference&lt;Texture2D&gt;()</c> would path-validate it and recursively call
/// <c>ContentManager.Load&lt;Texture2D&gt;()</c>, which is itself native-ABI-blocked in this
/// project (see <c>ContentManager.LoadNativeTexture2DHandle</c>) -- actually resolving/loading this
/// reference is deferred along with the rest of this project's native-ABI-blocked content loading,
/// not something worth half-implementing (path validation with no corresponding load) here.</summary>
internal sealed class XnbBasicEffectData
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
