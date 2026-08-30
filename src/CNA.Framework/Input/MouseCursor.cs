using CNA.Interop;

namespace CNA.Input;

/// <summary>One of the cursor images the system provides.</summary>
/// <remarks>
/// Not an XNA identity. XNA's <c>Mouse</c> has <c>GetState</c>, <c>SetPosition</c> and
/// <c>WindowHandle</c>, and nothing at all about the cursor's appearance -- a Windows Phone-era
/// framework had no reason to. MonoGame added <c>Mouse.SetCursor</c>, so a game ported from
/// MonoGame calls it, and CNA has the whole surface behind it.
/// </remarks>
public enum MouseCursorStock : uint
{
    Arrow = 0,
    Crosshair = 1,
    Hand = 2,
    IBeam = 3,
    No = 4,
    SizeAll = 5,
    SizeNesw = 6,
    SizeNs = 7,
    SizeNwse = 8,
    SizeWe = 9,
    Wait = 10,
    WaitArrow = 11,
}

/// <summary>
/// A mouse cursor image, either one of the system's or one drawn from a texture.
///
/// <b>Ownership differs between the two.</b> A stock cursor names something the system owns: two
/// requests for the same identity name the same underlying cursor, and disposing one must not
/// destroy it. A texture cursor is this object's, and is destroyed with it. That distinction is the
/// only real complexity here and it is the reason this is a class rather than a handle.
/// </summary>
public sealed class MouseCursor : IDisposable
{
    private readonly NativeResourceHandle? _owned;
    private readonly nint _handleValue;
    private bool _disposed;

    private MouseCursor(nint handleValue, bool owned)
    {
        _handleValue = handleValue;
        _owned = owned
            ? new NativeResourceHandle(handleValue, h => Native.cna_mouse_cursor_destroy(new CnaHandle(h)).IsSuccess())
            : null;
    }

    /// <summary>One of the system's own cursors. Not owned, so disposing this leaves the system's
    /// cursor alone.</summary>
    public static MouseCursor FromStock(MouseCursorStock stock)
    {
        CnaResult result = Native.cna_mouse_cursor_get_stock_ext(
            CnaAmbientGame.Current, (uint)stock, out CnaHandle cursor);
        CnaException.ThrowIfFailed(result, nameof(FromStock));
        return new MouseCursor(cursor.AsNint, owned: false);
    }

    /// <summary>
    /// A cursor drawn from a texture, with a hot spot inside it.
    ///
    /// The pixels are copied, so the texture may be disposed immediately afterwards -- which is
    /// worth stating because the opposite assumption is the natural one and would leak.
    /// </summary>
    public static MouseCursor FromTexture(Graphics.Texture2D texture, int originX, int originY)
    {
        ArgumentNullException.ThrowIfNull(texture);

        MouseCursor cursor = FromTextureHandle(texture.NativeHandleValue, originX, originY);
        GC.KeepAlive(texture);
        return cursor;
    }

    /// <summary>
    /// The same, over a raw handle, so the strict facade's own <c>Texture2D</c> -- which derives
    /// from its own texture base and is not this one -- can reach it without naming a
    /// <c>CNA.Interop</c> type. Same seam as
    /// <c>CNA.Graphics.RenderTarget2D.GetRenderTargetProperties</c>.
    /// </summary>
    internal static MouseCursor FromTextureHandle(nint textureHandleValue, int originX, int originY)
    {
        CnaResult result = Native.cna_mouse_cursor_create_from_texture2d(
            CnaAmbientGame.Current, new CnaHandle(textureHandleValue), originX, originY,
            out CnaHandle cursor);
        CnaException.ThrowIfFailed(result, nameof(FromTexture));
        return new MouseCursor(cursor.AsNint, owned: true);
    }

    internal nint NativeHandleValue => _handleValue;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _owned?.Dispose();
        GC.SuppressFinalize(this);
    }
}
