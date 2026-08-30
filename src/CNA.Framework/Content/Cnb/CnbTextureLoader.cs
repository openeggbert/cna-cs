using CNA.Graphics;
using CNA.Interop;

namespace CNA.Content.Cnb;

/// <summary>
/// Loads a <c>.cnb</c> texture container into a real <see cref="Texture2D"/>.
///
/// <b>Why this is a separate type from <see cref="CnbTexture"/>.</b> Decoding a container needs no
/// device; choosing which of its representations to upload needs one, because that is a question
/// about the GPU rather than about the file. Keeping the two apart is what lets the decode half stay
/// testable with no graphics device at all, which is most of what makes the CNB path testable.
///
/// <b>Why not <c>ContentManager.Load&lt;Texture2D&gt;</c>.</b> XNA has one content container and
/// this is a second one. Routing it through <c>Load&lt;T&gt;</c> would change a contract checked
/// member for member against XNA's own metadata, so a game opts in by name -- the same decision
/// <see cref="CnbDocument"/> records.
/// </summary>
public static class CnbTextureLoader
{
    /// <summary>
    /// Opens a <c>.cnb</c> file, decodes the 2D texture it holds, and uploads it.
    ///
    /// The document and the decoded description are both disposed before this returns; only the
    /// <see cref="Texture2D"/> survives, and the caller owns it.
    /// </summary>
    public static Texture2D LoadTexture2D(GraphicsDevice graphicsDevice, string path)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(path);

        using CnbDocument document = CnbDocument.Open(path);
        using CnbTexture decoded = CnbTexture.DecodeTexture2D(document);
        return Upload(graphicsDevice, decoded);
    }

    /// <summary>
    /// Uploads an already-decoded texture, choosing the first representation this device can both
    /// store and sample.
    ///
    /// <b>The choice asks the device rather than assuming.</b> Every mip level of the chosen
    /// representation is uploaded, so a file that carries mips produces a texture with mips rather
    /// than a blurry one -- and a file that carries only level 0 produces a single-level texture,
    /// because inventing the rest would be inventing image data.
    ///
    /// A texture whose every representation this device refuses is a
    /// <see cref="NotSupportedException"/> naming the formats it offered. That is deliberately not a
    /// silent fallback to the first representation: uploading bytes in a format the device did not
    /// accept produces a texture that looks like noise, which is harder to diagnose than a refusal.
    /// </summary>
    public static Texture2D Upload(GraphicsDevice graphicsDevice, CnbTexture texture)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(texture);

        if (texture.FaceCount != 1)
        {
            throw new NotSupportedException(
                $"This CNB texture has {texture.FaceCount} faces, so it is not a 2D texture.");
        }

        int representation = texture.SelectRepresentation(format => CanUpload(graphicsDevice, format));
        if (representation < 0)
        {
            throw new NotSupportedException(
                "No representation of this CNB texture can be uploaded to this device. It offers: " +
                string.Join(
                    ", ",
                    Enumerable.Range(0, texture.RepresentationCount).Select(texture.GetRepresentationFormat)) + ".");
        }

        CnbTextureFormat chosen = texture.GetRepresentationFormat(representation);
        if (!chosen.TryToSurfaceFormat(out SurfaceFormat surfaceFormat))
        {
            // Unreachable through CanUpload, which already required the mapping. Stated anyway,
            // because the predicate and this line would otherwise have to be kept in agreement by
            // memory.
            throw new NotSupportedException($"CNB format {chosen} has no CNA surface format.");
        }

        var uploaded = new Texture2D(
            graphicsDevice, texture.Width, texture.Height, mipMap: texture.MipCount > 1, surfaceFormat);

        try
        {
            for (int mipLevel = 0; mipLevel < texture.MipCount; mipLevel++)
            {
                byte[] level = texture.CopyLevel(representation, texture.LevelIndex(face: 0, mipLevel));
                uploaded.SetData(mipLevel, rect: null, level, 0, level.Length);
            }
        }
        catch
        {
            uploaded.Dispose();
            throw;
        }

        return uploaded;
    }

    /// <summary>
    /// Whether this device can hold a texture in <paramref name="format"/> and has not said it
    /// cannot sample one.
    ///
    /// <b>The asymmetry between the two conditions is the whole reason CNA reports two masks.</b>
    /// <c>TextureStorage</c> must be classified *and* supported: a format the renderer has said
    /// nothing about is not one to upload into. <c>Sampled</c> must merely not be *refused*, which
    /// is a weaker test, because it has to be: measured on OPENGLES3, the renderer classifies
    /// exactly <c>TextureStorage</c>, <c>RenderTarget</c> and <c>ColorTransfer</c> for every one of
    /// its twenty-four formats and says nothing about sampling at all. Requiring
    /// <c>IsSupported(Sampled)</c> -- which is what this first did -- refuses every format on the
    /// primary renderer, so no CNB texture would load anywhere.
    ///
    /// Treating unclassified as "no" for both would therefore have been the conservative-looking
    /// choice that produces a loader nothing works with; treating it as "yes" for both would accept
    /// <c>Alpha8</c>, which this renderer explicitly refuses. Splitting them the way the header's
    /// own rule allows -- unknown is not a refusal -- is the answer that follows from the data
    /// rather than from a preference.
    /// </summary>
    private static bool CanUpload(GraphicsDevice graphicsDevice, CnbTextureFormat format)
    {
        if (!format.TryToSurfaceFormat(out SurfaceFormat surfaceFormat))
        {
            return false;
        }

        CnaSurfaceFormatSupport support = graphicsDevice.GetCnaSurfaceFormatSupport(surfaceFormat);
        return support.IsSupported(CnaSurfaceFormatUsage.TextureStorage) &&
               !support.IsRefused(CnaSurfaceFormatUsage.Sampled);
    }
}
