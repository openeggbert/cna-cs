using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>TextureCube</c>: six square faces of equal <see cref="Size"/>, addressed
/// by <see cref="CubeMapFace"/>. Its own real resource type upstream
/// (<c>texture_volume.h</c>) -- see <see cref="Texture3D"/>'s doc comment, which describes the
/// same relationship. <c>SetData</c>/<c>GetData</c> go through a <c>CNA_TextureCubeTransfer</c>
/// descriptor, which is the 2D shape plus a face selector -- so unlike <see cref="Texture3D"/> it
/// keeps the "whole surface" form, expressed by leaving the rectangle flag clear rather than by
/// naming the full extent.
/// </summary>
public class TextureCube : Texture
{
    public TextureCube(GraphicsDevice graphicsDevice, int size)
        : this(graphicsDevice, size, mipMap: false, SurfaceFormat.Color)
    {
    }

    public TextureCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
        : base(graphicsDevice, CreateNativeHandle(graphicsDevice, size, mipMap, format))
    {
    }

    /// <summary>Wraps an already-created native handle. <c>protected</c> so
    /// <see cref="RenderTargetCube"/> -- which creates its handle through an entirely different
    /// native route (<c>cna_render_target_cube_create</c>) -- can still reuse this type's
    /// accessors, matching real XNA's <c>RenderTargetCube : TextureCube</c>.</summary>
    protected TextureCube(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : base(graphicsDevice, nativeHandleValue)
    {
    }

    private static nint CreateNativeHandle(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        var createInfo = new CnaTextureCubeCreateInfo
        {
            Size = (uint)size,
            MipMap = (byte)(mipMap ? 1 : 0),
            Format = (uint)format,
        };

        CnaResult result = Native.cna_texturecube_create(graphicsDevice.ResolveNativeDeviceHandle(), in createInfo, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(TextureCube));
        return handle.AsNint;
    }

    protected override void ReleaseNative(nint handleValue) => Native.cna_texturecube_destroy(new CnaHandle(handleValue));

    /// <summary>Width and height of each square face. Real XNA spells this <c>Size</c> on
    /// <c>TextureCube</c> (not <c>Width</c>/<c>Height</c>), matching the C API's own field
    /// name.</summary>
    public int Size => (int)GetInfo().Size;

    public override int LevelCount => (int)GetInfo().LevelCount;

    public override SurfaceFormat Format => (SurfaceFormat)GetInfo().Format;

    private CnaTextureCubeInfo GetInfo()
    {
        var info = new CnaTextureCubeInfo();
        CnaResult result = Native.cna_texturecube_get_info(new CnaHandle(NativeHandleValue), ref info);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, "cna_texturecube_get_info");
        return info;
    }

    /// <summary>Writes a whole face at mip level zero.</summary>
    public void SetData(CubeMapFace face, Color[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        SetData(face, 0, null, data, 0, data.Length);
    }

    /// <summary>Matches real XNA's rectangle-taking <c>SetData</c>. A <see langword="null"/>
    /// <paramref name="rectangle"/> means the whole face, which the descriptor expresses by
    /// leaving its own rectangle flag clear rather than by naming the full extent.</summary>
    public unsafe void SetData(CubeMapFace face, int level, Rectangle? rectangle, Color[] data, int startIndex, int elementCount)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, data.Length - startIndex);

        CnaTextureCubeTransfer transfer = BuildTransfer(face, level, rectangle);
        transfer.StartIndex = (ulong)startIndex;
        transfer.ElementCount = (ulong)elementCount;

        CnaColor[] pixels = Texture3D.ToNativeColors(data);
        fixed (CnaColor* pixelsPtr = pixels)
        {
            CnaResult result = Native.cna_texturecube_set_data(
                new CnaHandle(NativeHandleValue), in transfer, pixelsPtr, (ulong)pixels.Length);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(SetData));
        }
    }

    /// <summary>Reads a whole face at mip level zero.</summary>
    public Color[] GetData(CubeMapFace face) => GetData(face, 0, null);

    /// <summary>Sizes the array from what native reports it needs before reading -- the C API
    /// performs no partial write on an insufficient buffer, so asking first is the only correct
    /// order.</summary>
    public unsafe Color[] GetData(CubeMapFace face, int level, Rectangle? rectangle)
    {
        CnaTextureCubeTransfer transfer = BuildTransfer(face, level, rectangle);

        CnaResult sizeResult = Native.cna_texturecube_get_data(
            new CnaHandle(NativeHandleValue), in transfer, null, 0, out ulong required);
        GC.KeepAlive(this);

        if (sizeResult.IsFailure() && sizeResult != CnaResult.BufferTooSmall)
        {
            CnaException.ThrowIfFailed(sizeResult, nameof(GetData));
        }

        if (required == 0)
        {
            return [];
        }

        transfer.ElementCount = required;
        var pixels = new CnaColor[required];
        fixed (CnaColor* pixelsPtr = pixels)
        {
            CnaResult result = Native.cna_texturecube_get_data(
                new CnaHandle(NativeHandleValue), in transfer, pixelsPtr, required, out ulong written);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(GetData));
            return Texture3D.FromNativeColors(pixels, (int)written);
        }
    }

    private static CnaTextureCubeTransfer BuildTransfer(CubeMapFace face, int level, Rectangle? rectangle) => new()
    {
        Face = (uint)face,
        Level = level,
        HasRectangle = (byte)(rectangle is null ? 0 : 1),
        Rectangle = rectangle?.ToNative() ?? default,
    };

}
