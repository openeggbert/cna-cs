using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Microsoft.Xna.Framework.Graphics;

internal static class CompatTextureDataType
{
    internal static uint Of<T>() where T : struct
    {
        Type type = typeof(T);
        if (type == typeof(Color)) return CNA.Graphics.TextureDataType.Color;
        if (type == typeof(byte)) return CNA.Graphics.TextureDataType.Byte;
        if (type == typeof(float)) return CNA.Graphics.TextureDataType.Single;
        if (type == typeof(ushort)) return CNA.Graphics.TextureDataType.UShort;
        if (type == typeof(Vector2)) return CNA.Graphics.TextureDataType.Vector2Type;
        if (type == typeof(Vector4)) return CNA.Graphics.TextureDataType.Vector4Type;
        if (type == typeof(Bgr565)) return CNA.Graphics.TextureDataType.Bgr565;
        if (type == typeof(Bgra5551)) return CNA.Graphics.TextureDataType.Bgra5551;
        if (type == typeof(Bgra4444)) return CNA.Graphics.TextureDataType.Bgra4444;
        if (type == typeof(NormalizedByte2)) return CNA.Graphics.TextureDataType.NormalizedByte2;
        if (type == typeof(NormalizedByte4)) return CNA.Graphics.TextureDataType.NormalizedByte4;
        if (type == typeof(Rgba1010102)) return CNA.Graphics.TextureDataType.Rgba1010102;
        if (type == typeof(Rg32)) return CNA.Graphics.TextureDataType.Rg32;
        if (type == typeof(Rgba64)) return CNA.Graphics.TextureDataType.Rgba64;
        if (type == typeof(Alpha8)) return CNA.Graphics.TextureDataType.Alpha8;
        if (type == typeof(HalfSingle)) return CNA.Graphics.TextureDataType.HalfSingle;
        if (type == typeof(HalfVector2)) return CNA.Graphics.TextureDataType.HalfVector2;
        if (type == typeof(HalfVector4)) return CNA.Graphics.TextureDataType.HalfVector4;

        throw new NotSupportedException(
            $"{type} is not one of the element types supported by CNA texture transfers.");
    }
}
