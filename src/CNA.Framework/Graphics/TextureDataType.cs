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
    /// The tag for <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">For a type the ABI has no overload for. Named
    /// explicitly rather than falling back to <see cref="Byte"/>: a raw-byte read of a
    /// <c>Vector4</c> array would succeed, return the right number of bytes, and be wrong.</exception>
    public static uint Of<T>()
        where T : unmanaged
    {
        if (typeof(T) == typeof(Color)) return Color;
        if (typeof(T) == typeof(byte)) return Byte;
        if (typeof(T) == typeof(float)) return Single;
        if (typeof(T) == typeof(ushort)) return UShort;
        if (typeof(T) == typeof(Vector2)) return Vector2Type;
        if (typeof(T) == typeof(Vector4)) return Vector4Type;
        if (typeof(T) == typeof(Bgr565)) return Bgr565;
        if (typeof(T) == typeof(Bgra5551)) return Bgra5551;
        if (typeof(T) == typeof(Bgra4444)) return Bgra4444;
        if (typeof(T) == typeof(NormalizedByte2)) return NormalizedByte2;
        if (typeof(T) == typeof(NormalizedByte4)) return NormalizedByte4;
        if (typeof(T) == typeof(Rgba1010102)) return Rgba1010102;
        if (typeof(T) == typeof(Rg32)) return Rg32;
        if (typeof(T) == typeof(Rgba64)) return Rgba64;
        if (typeof(T) == typeof(Alpha8)) return Alpha8;
        if (typeof(T) == typeof(HalfSingle)) return HalfSingle;
        if (typeof(T) == typeof(HalfVector2)) return HalfVector2;
        if (typeof(T) == typeof(HalfVector4)) return HalfVector4;

        throw new NotSupportedException(
            $"{typeof(T)} is not one of the element types CNA_TextureDataType names, so no native " +
            "transfer overload matches it. Supported: Color, byte, float, ushort, Vector2, Vector4, " +
            "and the packed-vector formats (Bgr565, Bgra5551, Bgra4444, NormalizedByte2, " +
            "NormalizedByte4, Rgba1010102, Rg32, Rgba64, Alpha8, HalfSingle, HalfVector2, " +
            "HalfVector4).");
    }
}
