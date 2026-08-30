namespace CNA.Content.Xnb;

/// <summary>
/// What a mesh part's effect parsed to. One of the four stock shapes the XNA 4.0 content pipeline
/// writes into a model, distinguished because each carries different fields and different external
/// references, and because the builder that turns it into a real effect has to construct a
/// different type for each.
///
/// Every subclass carries external references as **resolved asset names**, not as objects -- see
/// <see cref="XnbContentReader.ReadExternalReference"/> for why resolution stops at the name in this
/// layer.
/// </summary>
internal abstract class XnbEffectData;

/// <summary>
/// XNA's <c>EffectMaterialReader</c>: an external effect to clone, then a
/// <c>Dictionary&lt;string, object&gt;</c> of parameter values the pipeline recorded.
///
/// Reading the dictionary is the reason <c>System.Object</c> had to become a resolvable target
/// type: its values are polymorphic by construction -- a float here, a Vector3 there, a texture
/// reference somewhere else -- and the file names each one's reader inline.
/// </summary>
internal sealed class XnbEffectMaterialData : XnbEffectData
{
    internal required string? EffectReference { get; init; }

    internal required IReadOnlyDictionary<string, object?> Parameters { get; init; }
}

/// <summary>XNA's <c>EnvironmentMapEffectReader</c>, transcribed field by field. The environment map
/// is a <c>TextureCube</c> and the base texture a <c>Texture2D</c>; the two are separate references
/// read in that order.</summary>
internal sealed class XnbEnvironmentMapEffectData : XnbEffectData
{
    internal required string? TextureReference { get; init; }

    internal required string? EnvironmentMapReference { get; init; }

    internal required float EnvironmentMapAmount { get; init; }

    internal required Vector3 EnvironmentMapSpecular { get; init; }

    internal required float FresnelFactor { get; init; }

    internal required Vector3 DiffuseColor { get; init; }

    internal required Vector3 EmissiveColor { get; init; }

    internal required float Alpha { get; init; }
}

/// <summary>XNA's <c>DualTextureEffectReader</c>, transcribed field by field.</summary>
internal sealed class XnbDualTextureEffectData : XnbEffectData
{
    internal required string? TextureReference { get; init; }

    internal required string? Texture2Reference { get; init; }

    internal required Vector3 DiffuseColor { get; init; }

    internal required float Alpha { get; init; }

    internal required bool VertexColorEnabled { get; init; }
}

/// <summary>
/// The three stock-effect readers beyond <c>BasicEffectReader</c> that real model content in the
/// XNA 4.0 sample collection actually reaches: 48 assets name <c>EffectMaterialReader</c>, 7 name
/// <c>EnvironmentMapEffectReader</c> and 2 name <c>DualTextureEffectReader</c>.
///
/// They are here rather than in a table of every reader XNA declares, because a reader nothing
/// reaches is untestable and an untested reader over a byte format is a guess. <c>SkinnedEffect</c>
/// and <c>AlphaTestEffect</c> have readers in XNA and no asset in any corpus on this machine names
/// them, so they are deliberately absent.
/// </summary>
internal static class XnbEffectReaders
{
    /// <summary>XNA's <c>EffectMaterialReader.Read</c>: the effect reference, then the parameter
    /// dictionary through the ordinary object-graph route.</summary>
    internal static object ReadEffectMaterial(XnbContentReader reader)
    {
        string? effectReference = reader.ReadExternalReference();
        object? parameters = reader.ReadObject();

        // XNA declares this Dictionary<string, object> and reads it with ReadObject<T>, so a file
        // naming anything else here is malformed rather than a variant this reader should tolerate.
        if (parameters is not Dictionary<string, object> typed)
        {
            throw new ContentLoadException(
                "Corrupt .xnb file: an EffectMaterial's parameters were " +
                $"{parameters?.GetType().Name ?? "null"} rather than Dictionary<string, object>.");
        }

        return new XnbEffectMaterialData
        {
            EffectReference = effectReference,
            Parameters = typed!,
        };
    }

    /// <summary>XNA's <c>EnvironmentMapEffectReader.Read</c>. The field order is the reader's, not
    /// the effect's property order, and the two differ -- <c>Alpha</c> is last here and is not last
    /// on the type.</summary>
    internal static object ReadEnvironmentMapEffect(XnbContentReader reader)
    {
        // Locals rather than an object initializer: every one of these advances the stream, so the
        // order is the format. C# does evaluate initializer members in source order, but a reader
        // should not have to know that to see that reordering two lines here changes what is parsed.
        string? texture = reader.ReadExternalReference();
        string? environmentMap = reader.ReadExternalReference();
        float environmentMapAmount = reader.ReadSingle();
        Vector3 environmentMapSpecular = reader.ReadVector3();
        float fresnelFactor = reader.ReadSingle();
        Vector3 diffuseColor = reader.ReadVector3();
        Vector3 emissiveColor = reader.ReadVector3();
        float alpha = reader.ReadSingle();

        return new XnbEnvironmentMapEffectData
        {
            TextureReference = texture,
            EnvironmentMapReference = environmentMap,
            EnvironmentMapAmount = environmentMapAmount,
            EnvironmentMapSpecular = environmentMapSpecular,
            FresnelFactor = fresnelFactor,
            DiffuseColor = diffuseColor,
            EmissiveColor = emissiveColor,
            Alpha = alpha,
        };
    }

    /// <summary>XNA's <c>DualTextureEffectReader.Read</c>.</summary>
    internal static object ReadDualTextureEffect(XnbContentReader reader)
    {
        // Locals for the same reason as ReadEnvironmentMapEffect.
        string? texture = reader.ReadExternalReference();
        string? texture2 = reader.ReadExternalReference();
        Vector3 diffuseColor = reader.ReadVector3();
        float alpha = reader.ReadSingle();
        bool vertexColorEnabled = reader.ReadBoolean();

        return new XnbDualTextureEffectData
        {
            TextureReference = texture,
            Texture2Reference = texture2,
            DiffuseColor = diffuseColor,
            Alpha = alpha,
            VertexColorEnabled = vertexColorEnabled,
        };
    }
}
