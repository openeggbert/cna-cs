using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// A native-backed 2D texture, now matching the real, shipped openeggbert/cna C API
/// (<c>graphics.h</c>) rather than a self-designed guess -- see <c>NEXT.md</c>'s native-ABI-migration
/// entry, step 5. <c>cna_texture2d_create</c> here (<c>graphics.h</c>) is a different, simpler
/// function from the several routes in <c>texture.h</c> -- this is the one that actually allocates
/// an empty, dimensions-only, device-attached texture the way this constructor needs.
///
/// <see cref="ReleaseNative"/>/<see cref="Width"/>/<see cref="Height"/> are overridable so
/// <see cref="RenderTarget2D"/> -- a real, *separate* resource type upstream with its own
/// create/get_info/destroy routes, not a texture created with special usage flags -- can override
/// them to call its own native functions while still sharing the disposal and
/// <c>NativeHandleValue</c> machinery it inherits, matching real XNA's own
/// <c>RenderTarget2D : Texture2D</c> inheritance.
///
/// Phase 8 WP3 reparented this onto <see cref="Texture"/> (and through it
/// <see cref="GraphicsResource"/>), matching real XNA's own
/// <c>Texture2D : Texture : GraphicsResource</c> chain. Handle ownership, disposal, and
/// <c>ReleaseNative</c> all moved up to <see cref="Texture"/>; what stays here is what is genuinely
/// 2D-specific (<see cref="Width"/>/<see cref="Height"/>, which come from <c>graphics.h</c>'s own
/// <c>CNA_Texture2DInfo</c> rather than the dimensionless shared <c>CNA_TextureInfo</c>, and the
/// data-transfer methods).
/// </summary>
public class Texture2D : Texture
{
    public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
        : base(graphicsDevice, CreateNativeTexture2DHandle(graphicsDevice, width, height))
    {
    }

    /// <summary>Creates the native texture handle without wrapping it. <c>internal</c> (visible to
    /// CNA.XnaCompat through the assembly's <c>InternalsVisibleTo</c> grant) for exactly the reason
    /// <see cref="RenderTarget2D.CreateNativeHandle"/> already is: CNA.XnaCompat's own
    /// <c>Texture2D</c> derives from CNA.XnaCompat's <c>Texture</c> -- so that
    /// <c>Texture t = someTexture2D;</c> compiles in game code, as it does in real XNA -- and
    /// therefore cannot inherit this type's implementation, but must still make the identical
    /// native call.</summary>
    internal static nint CreateNativeTexture2DHandle(GraphicsDevice graphicsDevice, int width, int height)
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

        return handle.AsNint;
    }

    /// <summary>
    /// Wraps an already-created native texture handle -- used by <c>ContentManager.Load&lt;T&gt;</c>,
    /// which receives the handle directly from <c>cna_content_manager_load_texture2d</c> rather than
    /// creating a texture from scratch. <c>protected internal</c> so CNA.XnaCompat's
    /// <c>Texture2D</c> subclass constructor can forward to it -- see docs/architecture.md.
    ///
    /// Gained its <paramref name="graphicsDevice"/> parameter in Phase 8 WP3: every
    /// <see cref="GraphicsResource"/> has a non-null <c>GraphicsDevice</c> in real XNA, so the
    /// device can no longer be omitted here. That is why <c>ContentManager.Load&lt;Texture2D&gt;</c>
    /// now requires a device to have been assigned, the same way its <c>Load&lt;Model&gt;</c> path
    /// already did.
    /// </summary>
    protected internal Texture2D(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : base(graphicsDevice, nativeHandleValue)
    {
    }

    /// <summary>Wraps a borrowed handle -- see <see cref="Texture"/>'s equivalent
    /// constructor.</summary>
    private protected Texture2D(GraphicsDevice graphicsDevice, nint nativeHandleValue, bool ownsHandle)
        : base(graphicsDevice, nativeHandleValue, ownsHandle)
    {
    }

    /// <summary>Builds a non-owning wrapper over a texture handle whose real owner is something
    /// else. <c>internal</c> because only this assembly knows which native calls hand out borrowed
    /// handles -- see <c>VideoPlayer.GetTexture</c>.</summary>
    internal static Texture2D CreateBorrowed(GraphicsDevice graphicsDevice, nint nativeHandleValue) =>
        new(graphicsDevice, nativeHandleValue, ownsHandle: false);

    /// <summary>Matches <c>cna_texture2d_destroy</c> exactly (<c>graphics.h:702</c>) -- confirmed
    /// real (an earlier, name-only <c>nm -D</c> pass had guessed this correctly by coincidence, but
    /// a later, more careful pass reading <c>texture.h</c> alone could not find it there and flagged
    /// the claim as unconfirmed; it exists, just in <c>graphics.h</c>, not <c>texture.h</c>).
    /// Overrides <see cref="Texture.ReleaseNative"/> (abstract there -- there is no shared
    /// <c>cna_texture_destroy</c>) and stays overridable itself so <see cref="RenderTarget2D"/> can
    /// release through <c>cna_render_target_destroy</c> instead.</summary>
    protected override void ReleaseNative(nint handleValue) => ReleaseNativeTexture2D(handleValue);

    /// <summary>Sourced from <c>cna_texture2d_get_info</c> (<c>graphics.h</c>'s own
    /// <c>CNA_Texture2DInfo</c>, which reports real width/height) -- not <c>texture.h</c>'s
    /// differently-shaped <c>CNA_TextureInfo</c>, which has no dimensions at all. <see langword="virtual"/>
    /// so <see cref="RenderTarget2D"/> can source its own dimensions from
    /// <c>cna_render_target_get_info</c> instead.</summary>
    public virtual int Width => GetTexture2DDimensions(NativeHandleValue).Width;

    public virtual int Height => GetTexture2DDimensions(NativeHandleValue).Height;

    /// <summary>Returns a plain tuple rather than <see cref="CnaTexture2DInfo"/> so CNA.XnaCompat
    /// can call it without naming a CNA.Interop type -- identical in shape and rationale to
    /// <see cref="RenderTarget2D.GetDimensions"/>. Named for the concrete type (not just
    /// <c>GetDimensions</c>) so it does not collide with <see cref="RenderTarget2D"/>'s own
    /// same-purpose helper, which reads a different native info struct.</summary>
    internal static (int Width, int Height) GetTexture2DDimensions(nint handleValue)
    {
        var info = new CnaTexture2DInfo();
        CnaResult result = Native.cna_texture2d_get_info(new CnaHandle(handleValue), ref info);
        CnaException.ThrowIfFailed(result, "cna_texture2d_get_info");
        return ((int)info.Width, (int)info.Height);
    }

    internal static void ReleaseNativeTexture2D(nint handleValue) => Native.cna_texture2d_destroy(new CnaHandle(handleValue));

    /// <summary>The shared body of both <c>SetData</c> overloads' native call, reusable by
    /// CNA.XnaCompat's parallel <c>Texture2D</c> -- see <see cref="CreateNativeTexture2DHandle"/>.</summary>
    internal static unsafe void SetDataRgba8(nint handleValue, ReadOnlySpan<byte> data)
    {
        if (data.Length % 4 != 0)
        {
            throw new ArgumentException("data.Length must be a multiple of 4 (one CNA_Color per pixel, 4 bytes each).", nameof(data));
        }

        fixed (byte* dataPtr = data)
        {
            CnaResult result = Native.cna_texture2d_set_data_rgba8(
                new CnaHandle(handleValue), (CnaColor*)dataPtr, (ulong)(data.Length / 4));
            CnaException.ThrowIfFailed(result, nameof(SetData));
        }
    }

    /// <summary>Matches <c>cna_texture2d_set_data_rgba8</c> exactly (<c>graphics.h:674</c>) --
    /// takes a <c>const CNA_Color*</c> pixel array plus a pixel count, not the old guessed shape's
    /// raw byte pointer plus byte length. <paramref name="data"/>'s bytes are reinterpreted as
    /// <see cref="CnaColor"/> elements (4 bytes each, the same RGBA8 byte layout either way) rather
    /// than re-encoding them, since this method's own public contract was already "raw RGBA8
    /// bytes" before this migration -- not a behavior change, just how those same bytes now reach
    /// native.</summary>
    public void SetData(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        SetDataRgba8(NativeHandleValue, data);
    }

    /// <summary>Convenience overload matching real XNA's common <c>SetData&lt;Color&gt;</c> usage
    /// (the general <c>SetData&lt;T&gt;</c> generic itself isn't implemented -- no caller in this
    /// project needs any other <c>T</c>). Goes straight through
    /// <c>cna_texture2d_set_data_rgba8</c>'s own <c>const CNA_Color*</c> pixel-array shape rather
    /// than routing through <see cref="SetData(byte[])"/>, since a <see cref="Color"/> array is
    /// already exactly that layout.</summary>
    public void SetData(Color[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        SetDataRgba8(NativeHandleValue, PackColors(data));
    }

    /// <summary>Packs a managed <see cref="Color"/> array into the RGBA8 byte layout
    /// <c>cna_texture2d_set_data_rgba8</c> expects. <c>internal</c> so CNA.XnaCompat's parallel
    /// <c>Texture2D</c> can reuse the packing instead of writing its own loop.</summary>
    internal static byte[] PackColors(ReadOnlySpan<Color> data)
    {
        var packed = new byte[data.Length * 4];
        for (int i = 0; i < data.Length; i++)
        {
            packed[(i * 4) + 0] = data[i].R;
            packed[(i * 4) + 1] = data[i].G;
            packed[(i * 4) + 2] = data[i].B;
            packed[(i * 4) + 3] = data[i].A;
        }

        return packed;
    }

}
