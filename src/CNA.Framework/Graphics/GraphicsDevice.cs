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
}
