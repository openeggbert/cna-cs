namespace CNA.Content.Xnb;

/// <summary>
/// Reads a real <c>.xnb</c> <c>BasicEffectReader</c> object graph, matching the real
/// openeggbert/cna C++ engine's own <c>StockEffectContentTypeReaders.cpp</c>
/// (<c>BasicEffectReader::Read</c>) exactly: an external texture reference (a plain, non-dispatched
/// length-prefixed string -- empty means "no texture"), then diffuse/emissive/specular color,
/// specular power, alpha, and a vertex-color-enabled flag.
///
/// Deliberately minimal, not a full port: this reader itself only parses the bytes into
/// <see cref="XnbBasicEffectData"/> -- it does not construct or populate a real
/// <see cref="Graphics.BasicEffect"/> here, since doing so needs a real, native-backed
/// <see cref="Graphics.GraphicsDevice"/> this reader has no access to (that construction happens
/// later, in <c>XnbModelBuilder.BuildBasicEffect</c>, which *does* apply every field parsed here
/// except <see cref="XnbBasicEffectData.TextureReference"/> -- see that method's own doc comment
/// for why the texture stays unresolved). The real reference implementation also has several other
/// stock-effect readers (<c>AlphaTestEffectReader</c> and similar) this project doesn't register
/// at all yet -- every field *this* reader parses is still read correctly regardless, so the
/// stream position stays correct for whatever comes after this effect in the file.
/// </summary>
internal static class XnbBasicEffectReader
{
    internal static object Read(XnbContentReader reader)
    {
        // Real XNA's own ReadExternalReference<T>(): a plain length-prefixed string, not the
        // dispatch protocol -- empty means "no texture reference," not a corrupt file.
        string rawTextureReference = reader.ReadString();
        string? textureReference = rawTextureReference.Length == 0 ? null : rawTextureReference;

        Vector3 diffuseColor = reader.ReadVector3();
        Vector3 emissiveColor = reader.ReadVector3();
        Vector3 specularColor = reader.ReadVector3();
        float specularPower = reader.ReadSingle();
        float alpha = reader.ReadSingle();
        bool vertexColorEnabled = reader.ReadBoolean();

        return new XnbBasicEffectData(textureReference, diffuseColor, emissiveColor, specularColor, specularPower, alpha, vertexColorEnabled);
    }
}
