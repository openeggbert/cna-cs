using CNA.Interop;

namespace CNA.Graphics;

public class GraphicsDevice
{
    /// <summary>
    /// The raw native device handle value. <c>protected internal</c> (not <c>internal</c>) so
    /// CNA.XnaCompat's <c>GraphicsDevice</c> subclass constructor can forward it to <c>base()</c>
    /// without CNA.XnaCompat ever naming <see cref="CnaHandle"/> -- see docs/architecture.md.
    /// </summary>
    protected internal nint NativeHandleValue { get; }

    protected internal GraphicsDevice(nint nativeHandleValue)
    {
        NativeHandleValue = nativeHandleValue;
    }

    public void Clear(Color color)
    {
        CnaResult result = Native.cna_graphics_device_clear(new CnaHandle(NativeHandleValue), color.ToNative());
        CnaException.ThrowIfFailed(result, nameof(Clear));
    }

    /// <summary>
    /// Sets the active render target, or restores the back buffer when <paramref name="renderTarget"/>
    /// is <c>null</c>. Takes <see cref="Texture2D"/> rather than the stricter
    /// <see cref="RenderTarget2D"/> -- a deliberate, documented looseness (real XNA's signature
    /// is <c>SetRenderTarget(RenderTarget2D)</c>) that lets CNA.XnaCompat's <c>RenderTarget2D</c>
    /// (which inherits from CNA.XnaCompat's own <c>Texture2D</c>, not this project's
    /// <see cref="RenderTarget2D"/> -- see that type's doc comment) upcast straight into this
    /// parameter with no override needed, matching every other XnaCompat <c>Draw</c>/<c>Clear</c>
    /// overload's "inherited unchanged, converts through implicit operators" pattern. Passing a
    /// texture that was never created as a render target is a caller error this method does not
    /// (and, without a real native ABI to validate against, currently cannot) catch.
    /// </summary>
    public void SetRenderTarget(Texture2D? renderTarget)
    {
        CnaHandle handle = renderTarget is null ? CnaHandle.Zero : new CnaHandle(renderTarget.NativeHandleValue);
        CnaResult result = Native.cna_graphics_device_set_render_target(new CnaHandle(NativeHandleValue), handle);
        CnaException.ThrowIfFailed(result, nameof(SetRenderTarget));
    }
}
