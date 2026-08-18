namespace CNA.Interop;

/// <summary>The <c>CNA_TEXTURE_IMAGE_FORMAT_*</c> identities (<c>texture.h</c>): the two encodings
/// <c>cna_texture2d_copy_encoded</c> and <c>cna_texture2d_save_file</c> accept, which are also the
/// two real XNA's <c>SaveAsPng</c>/<c>SaveAsJpeg</c> offer.</summary>
internal enum CnaTextureImageFormat : uint
{
    Png = 0,
    Jpeg = 1,
}
