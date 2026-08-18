using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// A native-backed 2D texture, now matching the real, shipped openeggbert/cna C API
/// (<c>graphics.h</c>) rather than a self-designed guess -- see <c>NEXT.md</c>'s native-ABI-migration
/// entry, step 5. <c>cna_texture2d_create</c> here (<c>graphics.h</c>) is a different, simpler
/// function from the several routes in <c>texture.h</c> -- this is the one that actually allocates
/// an empty, dimensions-only, device-attached texture the way this constructor needs.
///
/// <see cref="ReleaseNative"/>/<see cref="Width"/>/<see cref="Height"/> are <see langword="virtual"/>
/// so <see cref="RenderTarget2D"/> -- a real, *separate* resource type upstream with its own
/// create/get_info/destroy routes, not a texture created with special usage flags -- can override
/// them to call its own native functions while still sharing this class's <see cref="Dispose()"/>
/// and <see cref="NativeHandleValue"/> machinery, matching real XNA's own
/// <c>RenderTarget2D : Texture2D</c> inheritance.
/// </summary>
public class Texture2D : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        var createInfo = new CnaTexture2DCreateInfo
        {
            Width = (uint)width,
            Height = (uint)height,
            MipMap = 0,
            Format = 0, // CNA_SURFACE_FORMAT_COLOR
        };

        CnaResult result = Native.cna_texture2d_create(graphicsDevice.ResolveNativeDeviceHandle(), in createInfo, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(Texture2D));

        _handle = new NativeResourceHandle(handle.AsNint, ReleaseNative);
    }

    /// <summary>
    /// Wraps an already-created native texture handle -- used by <c>ContentManager.Load&lt;T&gt;</c>,
    /// which receives the handle directly from <c>cna_content_manager_load_texture2d</c> rather than
    /// creating a texture from scratch. <c>protected internal</c> so CNA.XnaCompat's
    /// <c>Texture2D</c> subclass constructor can forward to it -- see docs/architecture.md.
    /// </summary>
    protected internal Texture2D(nint nativeHandleValue)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, ReleaseNative);
    }

    /// <summary>Matches <c>cna_texture2d_destroy</c> exactly (<c>graphics.h:702</c>) -- confirmed
    /// real (an earlier, name-only <c>nm -D</c> pass had guessed this correctly by coincidence, but
    /// a later, more careful pass reading <c>texture.h</c> alone could not find it there and flagged
    /// the claim as unconfirmed; it exists, just in <c>graphics.h</c>, not <c>texture.h</c>).
    /// <see langword="virtual"/> so <see cref="RenderTarget2D"/> can release itself through
    /// <c>cna_render_target_destroy</c> instead -- see this class's own doc comment.</summary>
    protected virtual void ReleaseNative(nint handleValue) => Native.cna_texture2d_destroy(new CnaHandle(handleValue));

    internal nint NativeHandleValue => _handle.DangerousGetHandle();

    /// <summary>Sourced from <c>cna_texture2d_get_info</c> (<c>graphics.h</c>'s own
    /// <c>CNA_Texture2DInfo</c>, which reports real width/height) -- not <c>texture.h</c>'s
    /// differently-shaped <c>CNA_TextureInfo</c>, which has no dimensions at all. <see langword="virtual"/>
    /// so <see cref="RenderTarget2D"/> can source its own dimensions from
    /// <c>cna_render_target_get_info</c> instead.</summary>
    public virtual int Width => (int)GetInfo().Width;

    public virtual int Height => (int)GetInfo().Height;

    private CnaTexture2DInfo GetInfo()
    {
        var info = new CnaTexture2DInfo();
        CnaResult result = Native.cna_texture2d_get_info(new CnaHandle(NativeHandleValue), ref info);
        CnaException.ThrowIfFailed(result, "cna_texture2d_get_info");
        return info;
    }

    /// <summary>Matches <c>cna_texture2d_set_data_rgba8</c> exactly (<c>graphics.h:674</c>) --
    /// takes a <c>const CNA_Color*</c> pixel array plus a pixel count, not the old guessed shape's
    /// raw byte pointer plus byte length. <paramref name="data"/>'s bytes are reinterpreted as
    /// <see cref="CnaColor"/> elements (4 bytes each, the same RGBA8 byte layout either way) rather
    /// than re-encoding them, since this method's own public contract was already "raw RGBA8
    /// bytes" before this migration -- not a behavior change, just how those same bytes now reach
    /// native.</summary>
    public unsafe void SetData(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length % 4 != 0)
        {
            throw new ArgumentException("data.Length must be a multiple of 4 (one CNA_Color per pixel, 4 bytes each).", nameof(data));
        }

        fixed (byte* dataPtr = data)
        {
            CnaResult result = Native.cna_texture2d_set_data_rgba8(
                new CnaHandle(NativeHandleValue), (CnaColor*)dataPtr, (ulong)(data.Length / 4));
            CnaException.ThrowIfFailed(result, nameof(SetData));
        }
    }

    /// <summary>Convenience overload matching real XNA's common <c>SetData&lt;Color&gt;</c> usage
    /// (the general <c>SetData&lt;T&gt;</c> generic itself isn't implemented -- no caller in this
    /// project needs any other <c>T</c>). Goes straight through
    /// <c>cna_texture2d_set_data_rgba8</c>'s own <c>const CNA_Color*</c> pixel-array shape rather
    /// than routing through <see cref="SetData(byte[])"/>, since a <see cref="Color"/> array is
    /// already exactly that layout.</summary>
    public unsafe void SetData(Color[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var pixels = new CnaColor[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            pixels[i] = data[i].ToNative();
        }

        fixed (CnaColor* dataPtr = pixels)
        {
            CnaResult result = Native.cna_texture2d_set_data_rgba8(new CnaHandle(NativeHandleValue), dataPtr, (ulong)pixels.Length);
            CnaException.ThrowIfFailed(result, nameof(SetData));
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
