namespace CNA.Graphics;

/// <summary>Values 0-19 match real XNA's <c>SurfaceFormat</c> exactly. Values 20-26 are CNA's own
/// extensions beyond XNA (every one suffixed <c>_EXT</c> in the C API, mirrored here with an
/// <c>Ext</c> suffix): they are included deliberately rather than trimmed to XNA's 20, so a value
/// coming back from native always lands on a defined member instead of an out-of-range cast that
/// no <c>switch</c> or <c>ToString</c> would handle. Confirmed against the real, shipped
/// openeggbert/cna C API's own <c>CNA_SURFACE_FORMAT_*</c> constants
/// (<c>graphics.h:231-286</c>).</summary>
public enum SurfaceFormat
{
    Color = 0,
    Bgr565 = 1,
    Bgra5551 = 2,
    Bgra4444 = 3,
    Dxt1 = 4,
    Dxt3 = 5,
    Dxt5 = 6,
    NormalizedByte2 = 7,
    NormalizedByte4 = 8,
    Rgba1010102 = 9,
    Rg32 = 10,
    Rgba64 = 11,
    Alpha8 = 12,
    Single = 13,
    Vector2 = 14,
    Vector4 = 15,
    HalfSingle = 16,
    HalfVector2 = 17,
    HalfVector4 = 18,
    HdrBlendable = 19,
    ColorBgraExt = 20,
    ColorSrgbExt = 21,
    Dxt5SrgbExt = 22,
    Bc7Ext = 23,
    Bc7SrgbExt = 24,
    ByteExt = 25,
    UShortExt = 26,
}
