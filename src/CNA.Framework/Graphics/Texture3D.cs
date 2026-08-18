using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>Texture3D</c>. A volume texture: <see cref="Width"/>/<see cref="Height"/>/
/// <see cref="Depth"/> plus everything <see cref="Texture"/> already provides.
///
/// Its own real resource type upstream (<c>texture_volume.h</c>: its own
/// <c>create</c>/<c>get_info</c>/<c>destroy</c> triple), not a 2D texture with extra flags -- the
/// same relationship <see cref="RenderTarget2D"/> has to <see cref="Texture2D"/>.
///
/// <c>SetData</c>/<c>GetData</c> go through a <c>CNA_Texture3DTransfer</c> descriptor -- a mip
/// level plus an explicit texel box -- which the 2D path has no equivalent of. Unlike the cube and
/// 2D forms there is no "whole surface" flag: a volume transfer always names its box, so the
/// convenience overloads below fill it in from the texture's own dimensions.
/// </summary>
public class Texture3D : Texture
{
    /// <summary>Wraps an already-created native handle. <c>private protected</c> rather than public:
    /// a caller has no way to obtain a bare handle, and the only producer inside this assembly is
    /// <see cref="EffectParameter"/> rewrapping a texture read back out of a shader
    /// parameter.</summary>
    private protected Texture3D(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : base(graphicsDevice, nativeHandleValue)
    {
    }

    public Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth)
        : this(graphicsDevice, width, height, depth, mipMap: false, SurfaceFormat.Color)
    {
    }

    public Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth, bool mipMap, SurfaceFormat format)
        : base(graphicsDevice, CreateNativeHandle(graphicsDevice, width, height, depth, mipMap, format))
    {
    }

    private static nint CreateNativeHandle(
        GraphicsDevice graphicsDevice, int width, int height, int depth, bool mipMap, SurfaceFormat format)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(depth);

        var createInfo = new CnaTexture3DCreateInfo
        {
            Width = (uint)width,
            Height = (uint)height,
            Depth = (uint)depth,
            MipMap = (byte)(mipMap ? 1 : 0),
            Format = (uint)format,
        };

        CnaResult result = Native.cna_texture3d_create(graphicsDevice.ResolveNativeDeviceHandle(), in createInfo, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(Texture3D));
        return handle.AsNint;
    }

    protected override void ReleaseNative(nint handleValue) => Native.cna_texture3d_destroy(new CnaHandle(handleValue));

    public int Width => (int)GetInfo().Width;

    public int Height => (int)GetInfo().Height;

    public int Depth => (int)GetInfo().Depth;

    /// <summary>Overridden to read <c>cna_texture3d_get_info</c> rather than the shared
    /// <c>cna_texture_get_info</c> the base uses. Both would answer correctly -- the shared one
    /// accepts a Texture3D handle -- but this way a single native call already being made for
    /// dimensions also serves these, instead of a second round trip.</summary>
    public override int LevelCount => (int)GetInfo().LevelCount;

    public override SurfaceFormat Format => (SurfaceFormat)GetInfo().Format;

    private CnaTexture3DInfo GetInfo()
    {
        var info = new CnaTexture3DInfo();
        CnaResult result = Native.cna_texture3d_get_info(new CnaHandle(NativeHandleValue), ref info);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, "cna_texture3d_get_info");
        return info;
    }

    /// <summary>Writes the whole volume at mip level zero.</summary>
    public void SetData(Color[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        SetData(0, 0, 0, 0, Width, Height, Depth, data, 0, data.Length);
    }

    /// <summary>Matches real XNA's box-taking <c>SetData</c>. The box is half-open in each axis,
    /// as in XNA: <paramref name="right"/>/<paramref name="bottom"/>/<paramref name="back"/> are
    /// exclusive.</summary>
    public unsafe void SetData(
        int level, int left, int top, int right, int bottom, int front, int back,
        Color[] data, int startIndex, int elementCount)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, data.Length - startIndex);

        var transfer = new CnaTexture3DTransfer
        {
            Level = level,
            Left = left,
            Top = top,
            Right = right,
            Bottom = bottom,
            Front = front,
            Back = back,
            StartIndex = (ulong)startIndex,
            ElementCount = (ulong)elementCount,
        };

        CnaColor[] pixels = ToNativeColors(data);
        fixed (CnaColor* pixelsPtr = pixels)
        {
            CnaResult result = Native.cna_texture3d_set_data(
                new CnaHandle(NativeHandleValue), in transfer, pixelsPtr, (ulong)pixels.Length);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(SetData));
        }
    }

    /// <summary>Reads the whole volume at mip level zero.</summary>
    public Color[] GetData() => GetData(0, 0, 0, 0, Width, Height, Depth);

    /// <summary>Matches real XNA's box-taking <c>GetData</c>. The array is sized from what native
    /// reports it needs, then trimmed to what it actually wrote -- the C API performs no partial
    /// write on an insufficient buffer, so asking first is the only correct order.</summary>
    public unsafe Color[] GetData(int level, int left, int top, int right, int bottom, int front, int back)
    {
        var transfer = new CnaTexture3DTransfer
        {
            Level = level,
            Left = left,
            Top = top,
            Right = right,
            Bottom = bottom,
            Front = front,
            Back = back,
        };

        CnaResult sizeResult = Native.cna_texture3d_get_data(
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
            CnaResult result = Native.cna_texture3d_get_data(
                new CnaHandle(NativeHandleValue), in transfer, pixelsPtr, required, out ulong written);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(GetData));
            return FromNativeColors(pixels, (int)written);
        }
    }

    /// <summary>Shared by the 3D and cube transfer paths -- both take a <c>CNA_Color</c> array, so
    /// the conversion is identical and lives in one place.</summary>
    internal static CnaColor[] ToNativeColors(Color[] data)
    {
        var pixels = new CnaColor[data.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = data[i].ToNative();
        }

        return pixels;
    }

    internal static Color[] FromNativeColors(CnaColor[] pixels, int count)
    {
        var colors = new Color[Math.Min(count, pixels.Length)];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.FromNative(pixels[i]);
        }

        return colors;
    }


    /// <summary>Wraps a handle this assembly already owns. Exists because
    /// <see cref="EffectParameter"/> reads a retained texture handle back out of a shader parameter
    /// and has to rewrap it; the raw-handle constructor itself stays <c>protected</c>.</summary>
    internal static Texture3D FromNativeHandle(GraphicsDevice graphicsDevice, nint nativeHandleValue) =>
        new(graphicsDevice, nativeHandleValue);
}
