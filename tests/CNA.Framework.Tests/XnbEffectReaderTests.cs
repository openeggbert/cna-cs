using CNA.Content;
using CNA.Content.Xnb;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// The three stock-effect readers and the external-reference machinery they rest on.
///
/// Each expectation is transcribed from the decompiled XNA 4.0 reader of the same name, and each
/// test asserts distinct values per field rather than a shape. The failure these guard against is
/// not "it threw" -- a reader with two fields transposed reads the same number of bytes and returns
/// a fully-populated, entirely wrong object.
/// </summary>
public sealed class XnbEffectReaderTests
{
    /// <summary>
    /// <c>EnvironmentMapEffectReader</c>: texture, environment map, amount, specular, fresnel,
    /// diffuse, emissive, alpha -- in that order.
    ///
    /// Every scalar gets a different value and every vector a distinct triple, so a swapped pair
    /// fails. Writing <c>1.0</c> everywhere would pass with any permutation.
    /// </summary>
    [Fact]
    public void EnvironmentMapEffect_ReadsEveryFieldInTheReadersOrder()
    {
        byte[] asset = XnbAssetWriter.Build(
            typeReaders: ["Microsoft.Xna.Framework.Content.EnvironmentMapEffectReader"],
            writeRoot: writer =>
            {
                writer.Write7BitEncodedInt(1);
                writer.Write("skin");                                  // texture reference
                writer.Write("sky");                                   // environment map reference
                writer.Write(0.25f);                                   // EnvironmentMapAmount
                writer.Write(1f); writer.Write(2f); writer.Write(3f);  // EnvironmentMapSpecular
                writer.Write(0.5f);                                    // FresnelFactor
                writer.Write(4f); writer.Write(5f); writer.Write(6f);  // DiffuseColor
                writer.Write(7f); writer.Write(8f); writer.Write(9f);  // EmissiveColor
                writer.Write(0.75f);                                   // Alpha
            });

        var data = Assert.IsType<XnbEnvironmentMapEffectData>(XnbAssetWriter.ReadRoot(asset, "Models\\Ship"));

        Assert.Equal("Models\\skin", data.TextureReference);
        Assert.Equal("Models\\sky", data.EnvironmentMapReference);
        Assert.Equal(0.25f, data.EnvironmentMapAmount);
        Assert.Equal(new Vector3(1f, 2f, 3f), data.EnvironmentMapSpecular);
        Assert.Equal(0.5f, data.FresnelFactor);
        Assert.Equal(new Vector3(4f, 5f, 6f), data.DiffuseColor);
        Assert.Equal(new Vector3(7f, 8f, 9f), data.EmissiveColor);
        Assert.Equal(0.75f, data.Alpha);
    }

    /// <summary><c>DualTextureEffectReader</c>: two texture references, diffuse, alpha, vertex
    /// colour. The two references are distinct strings, because reading the same one twice is the
    /// mistake a shared-value test cannot see.</summary>
    [Fact]
    public void DualTextureEffect_ReadsBothTexturesAndTheRemainingFields()
    {
        byte[] asset = XnbAssetWriter.Build(
            typeReaders: ["Microsoft.Xna.Framework.Content.DualTextureEffectReader"],
            writeRoot: writer =>
            {
                writer.Write7BitEncodedInt(1);
                writer.Write("base");
                writer.Write("overlay");
                writer.Write(1f); writer.Write(2f); writer.Write(3f);
                writer.Write(0.5f);
                writer.Write(true);
            });

        var data = Assert.IsType<XnbDualTextureEffectData>(XnbAssetWriter.ReadRoot(asset, "Models\\Ship"));

        Assert.Equal("Models\\base", data.TextureReference);
        Assert.Equal("Models\\overlay", data.Texture2Reference);
        Assert.Equal(new Vector3(1f, 2f, 3f), data.DiffuseColor);
        Assert.Equal(0.5f, data.Alpha);
        Assert.True(data.VertexColorEnabled);
    }

    /// <summary>
    /// <c>EffectMaterialReader</c>: the effect reference, then a parameter dictionary whose values
    /// are polymorphic. The dictionary carries three different value shapes -- a scalar, a vector,
    /// and an external reference -- because a material with one value type would not exercise the
    /// dispatch that makes the dictionary work at all.
    /// </summary>
    [Fact]
    public void EffectMaterial_ReadsTheEffectReferenceAndPolymorphicParameters()
    {
        byte[] asset = XnbAssetWriter.Build(
            typeReaders:
            [
                "Microsoft.Xna.Framework.Content.EffectMaterialReader",
                "Microsoft.Xna.Framework.Content.DictionaryReader`2[[System.String][System.Object]]",
                "Microsoft.Xna.Framework.Content.StringReader",
                "Microsoft.Xna.Framework.Content.SingleReader",
                "Microsoft.Xna.Framework.Content.Vector3Reader",
                "Microsoft.Xna.Framework.Content.ExternalReferenceReader",
            ],
            writeRoot: writer =>
            {
                writer.Write7BitEncodedInt(1);          // root: the material
                writer.Write("..\\Effects\\Normal");    // effect reference, up one directory
                writer.Write7BitEncodedInt(2);          // parameters: the dictionary
                writer.Write(3);                        // three entries
                writer.Write7BitEncodedInt(3); writer.Write("Shininess");
                writer.Write7BitEncodedInt(4); writer.Write(12.5f);
                writer.Write7BitEncodedInt(3); writer.Write("LightDirection");
                writer.Write7BitEncodedInt(5); writer.Write(0f); writer.Write(-1f); writer.Write(0f);
                writer.Write7BitEncodedInt(3); writer.Write("NormalMap");
                writer.Write7BitEncodedInt(6); writer.Write("rock_norm");
            });

        var data = Assert.IsType<XnbEffectMaterialData>(XnbAssetWriter.ReadRoot(asset, "Models\\Sub\\Ship"));

        Assert.Equal("Models\\Effects\\Normal", data.EffectReference);
        Assert.Equal(3, data.Parameters.Count);
        Assert.Equal(12.5f, Assert.IsType<float>(data.Parameters["Shininess"]));
        Assert.Equal(new Vector3(0f, -1f, 0f), Assert.IsType<Vector3>(data.Parameters["LightDirection"]));

        // The texture parameter stays an external reference rather than becoming a string, so the
        // builder can tell it from a genuine string-valued parameter. Its name is resolved against
        // the *material's* asset, which is the model, not against the effect it references.
        var reference = Assert.IsType<XnbExternalReference>(data.Parameters["NormalMap"]);
        Assert.Equal("Models\\Sub\\rock_norm", reference.AssetName);
    }

    /// <summary>An empty reference is "no effect", not an asset named after the directory. XNA
    /// returns <c>default(T)</c> without asking the content manager at all.</summary>
    [Fact]
    public void EffectMaterial_EmptyEffectReference_IsNoReference()
    {
        byte[] asset = XnbAssetWriter.Build(
            typeReaders:
            [
                "Microsoft.Xna.Framework.Content.EffectMaterialReader",
                "Microsoft.Xna.Framework.Content.DictionaryReader`2[[System.String][System.Object]]",
            ],
            writeRoot: writer =>
            {
                writer.Write7BitEncodedInt(1);
                writer.Write(string.Empty);
                writer.Write7BitEncodedInt(2);
                writer.Write(0);
            });

        var data = Assert.IsType<XnbEffectMaterialData>(XnbAssetWriter.ReadRoot(asset, "Models\\Sub\\Ship"));

        Assert.Null(data.EffectReference);
        Assert.Empty(data.Parameters);
    }

    /// <summary>A <c>BasicEffect</c>'s texture reference is resolved the same way, against the
    /// referring asset. It used to be kept as the raw relative string and never resolved, so every
    /// textured model built an effect with no texture.</summary>
    [Fact]
    public void BasicEffect_ResolvesItsTextureReferenceAgainstTheReferringAsset()
    {
        byte[] asset = XnbAssetWriter.Build(
            typeReaders: ["Microsoft.Xna.Framework.Content.BasicEffectReader"],
            writeRoot: writer =>
            {
                writer.Write7BitEncodedInt(1);
                writer.Write("Textures\\hull");
                writer.Write(1f); writer.Write(2f); writer.Write(3f);   // diffuse
                writer.Write(4f); writer.Write(5f); writer.Write(6f);   // emissive
                writer.Write(7f); writer.Write(8f); writer.Write(9f);   // specular
                writer.Write(16f);                                      // specular power
                writer.Write(0.5f);                                     // alpha
                writer.Write(true);                                     // vertex colour
            });

        var data = Assert.IsType<XnbBasicEffectData>(XnbAssetWriter.ReadRoot(asset, "Models\\Ship"));

        Assert.Equal("Models\\Textures\\hull", data.TextureReference);
        Assert.Equal(new Vector3(1f, 2f, 3f), data.DiffuseColor);
        Assert.Equal(new Vector3(4f, 5f, 6f), data.EmissiveColor);
        Assert.Equal(new Vector3(7f, 8f, 9f), data.SpecularColor);
        Assert.Equal(16f, data.SpecularPower);
        Assert.Equal(0.5f, data.Alpha);
        Assert.True(data.VertexColorEnabled);
    }

    /// <summary>A reference in an asset at the content root has no directory to resolve against and
    /// stays as written -- the case where <c>GetPathToReference</c> finds no separator.</summary>
    [Fact]
    public void BasicEffect_ReferenceFromARootLevelAsset_KeepsItsOwnName()
    {
        byte[] asset = XnbAssetWriter.Build(
            typeReaders: ["Microsoft.Xna.Framework.Content.BasicEffectReader"],
            writeRoot: writer =>
            {
                writer.Write7BitEncodedInt(1);
                writer.Write("hull");
                writer.Write(0f); writer.Write(0f); writer.Write(0f);
                writer.Write(0f); writer.Write(0f); writer.Write(0f);
                writer.Write(0f); writer.Write(0f); writer.Write(0f);
                writer.Write(0f);
                writer.Write(1f);
                writer.Write(false);
            });

        var data = Assert.IsType<XnbBasicEffectData>(XnbAssetWriter.ReadRoot(asset, "Ship"));

        Assert.Equal("hull", data.TextureReference);
    }
}
