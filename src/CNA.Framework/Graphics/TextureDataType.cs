using CNA.Graphics.PackedVector;

namespace CNA.Graphics;

/// <summary>
/// Maps a managed element type onto <c>texture.h</c>'s <c>CNA_TextureDataType</c> -- the tag that
/// selects which native overload a texture transfer runs.
///
/// The ABI reads into a destination typed by this tag rather than by byte count, which is what made
/// a generic <c>GetData&lt;T&gt;</c> look unreachable: nothing here can invent a tag for an
/// arbitrary element type. What it can do is refuse the ones it does not know, by name,
/// and let <c>cna_texture_validate_get_data_format</c> check the rest against the surface format.
/// Those two together are the safety the throwing version said was missing.
/// </summary>
internal static class TextureDataType
{
    public const uint Color = 0;
    public const uint Bgr565 = 1;
    public const uint Bgra5551 = 2;
    public const uint Bgra4444 = 3;
    public const uint Byte = 4;
    public const uint NormalizedByte2 = 5;
    public const uint NormalizedByte4 = 6;
    public const uint Rgba1010102 = 7;
    public const uint Rg32 = 8;
    public const uint Rgba64 = 9;
    public const uint Alpha8 = 10;
    public const uint Single = 11;
    public const uint Vector2Type = 12;
    public const uint Vector4Type = 13;
    public const uint HalfSingle = 14;
    public const uint HalfVector2 = 15;
    public const uint HalfVector4 = 16;
    public const uint UShort = 17;

    /// <summary>
    /// The tag that names <paramref name="format"/> itself, with its size in bytes.
    ///
    /// This is the untagged fallback's whole trick: transferring a surface through its own format's
    /// tag converts nothing, so the bytes that cross the boundary are the surface's bytes, which is
    /// what XNA copies for any element type.
    ///
    /// A compressed format has no per-pixel tag and reports <see cref="Byte"/> with a unit of one,
    /// which is the shape the native side special-cases: for a byte-tagged transfer of a compressed
    /// texture it counts whole 4x4 blocks rather than pixels. Reading DXT blocks as bytes is what
    /// XNA does too.
    /// </summary>
    /// <exception cref="NotSupportedException">For a surface format with no transfer tag, so there
    /// is nothing to fall back to.</exception>
    public static (uint Tag, int UnitBytes) ForSurfaceFormat(SurfaceFormat format)
    {
        uint tag = format switch
        {
            SurfaceFormat.Color or SurfaceFormat.ColorBgraExt or SurfaceFormat.ColorSrgbExt => Color,
            SurfaceFormat.Bgr565 => Bgr565,
            SurfaceFormat.Bgra5551 => Bgra5551,
            SurfaceFormat.Bgra4444 => Bgra4444,
            SurfaceFormat.NormalizedByte2 => NormalizedByte2,
            SurfaceFormat.NormalizedByte4 => NormalizedByte4,
            SurfaceFormat.Rgba1010102 => Rgba1010102,
            SurfaceFormat.Rg32 => Rg32,
            SurfaceFormat.Rgba64 => Rgba64,
            SurfaceFormat.Alpha8 => Alpha8,
            SurfaceFormat.Single => Single,
            SurfaceFormat.Vector2 => Vector2Type,
            SurfaceFormat.Vector4 => Vector4Type,
            SurfaceFormat.HalfSingle => HalfSingle,
            SurfaceFormat.HalfVector2 => HalfVector2,
            SurfaceFormat.HalfVector4 or SurfaceFormat.HdrBlendable => HalfVector4,
            SurfaceFormat.ByteExt => Byte,
            SurfaceFormat.UShortExt => UShort,

            // Compressed formats have no per-texel tag. The byte tag is the route, and the block
            // arithmetic behind it is the native side's, not this binding's.
            SurfaceFormat.Dxt1 or SurfaceFormat.Dxt3 or SurfaceFormat.Dxt5
                or SurfaceFormat.Dxt5SrgbExt or SurfaceFormat.Bc7Ext or SurfaceFormat.Bc7SrgbExt => Byte,

            _ => throw new NotSupportedException(
                $"Surface format {format} has no CNA_TextureDataType tag, so there is no transfer " +
                "route for it."),
        };

        // A compressed format's "unit" is a whole block, and the byte-tagged compressed route
        // counts bytes rather than blocks, so the caller's window is already in the right units.
        int unitBytes = tag == Byte && format is SurfaceFormat.Dxt1 or SurfaceFormat.Dxt3
            or SurfaceFormat.Dxt5 or SurfaceFormat.Dxt5SrgbExt or SurfaceFormat.Bc7Ext
            or SurfaceFormat.Bc7SrgbExt
            ? 1
            : TextureTransferPlan.UnitBytes(format);

        return (tag, unitBytes);
    }
}
