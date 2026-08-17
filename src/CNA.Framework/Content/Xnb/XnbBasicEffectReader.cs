namespace CNA.Content.Xnb;

/// <summary>
/// Reads a real <c>.xnb</c> <c>BasicEffectReader</c> object graph, matching the real
/// openeggbert/cna C++ engine's own <c>StockEffectContentTypeReaders.cpp</c>
/// (<c>BasicEffectReader::Read</c>) exactly: an external texture reference (a plain, non-dispatched
/// length-prefixed string -- empty means "no texture"), then diffuse/emissive/specular color,
/// specular power, alpha, and a vertex-color-enabled flag.
///
/// Deliberately minimal, not a full port: this project's <see cref="Graphics.BasicEffect"/> is not
/// constructed or populated from these values here -- the real reference implementation also has
/// several other stock-effect readers (<c>AlphaTestEffectReader</c> and similar) this project
/// doesn't register at all yet, and
/// wiring any of them up to a real, native-backed <see cref="Graphics.Effect"/> needs a real
/// <see cref="Graphics.GraphicsDevice"/> this reader has no access to (see
/// <see cref="XnbBasicEffectData"/>'s own doc comment) -- every field is still read correctly, so
/// the stream position stays correct for whatever comes after this effect in the file, but the
/// values themselves are only carried as data for now, not applied to a real effect instance.
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
