using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// A native-backed 2D texture. Owns a <see cref="NativeResourceHandle"/> per the resource
/// lifetime pattern in ../../cnabinding/analysis_binding.md §24.
/// </summary>
public class Texture2D : IDisposable
{
    private readonly NativeResourceHandle _handle;

    public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        CnaResult result = Native.cna_texture2d_create(
            new CnaHandle(graphicsDevice.NativeHandleValue), width, height, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(Texture2D));

        _handle = new NativeResourceHandle(handle.Value, ReleaseNative);
    }

    /// <summary>
    /// Wraps an already-created native texture handle -- used by <c>ContentManager.Load&lt;T&gt;</c>,
    /// which receives the handle directly from <c>cna_content_load_texture2d</c> rather than
    /// creating a texture from scratch. <c>protected internal</c> so CNA.XnaCompat's
    /// <c>Texture2D</c> subclass constructor can forward to it -- see docs/architecture.md.
    /// </summary>
    protected internal Texture2D(nint nativeHandleValue)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, ReleaseNative);
    }

    private static void ReleaseNative(nint handleValue) => Native.cna_texture2d_release(new CnaHandle(handleValue));

    internal nint NativeHandleValue => _handle.DangerousGetHandle();

    public int Width => Native.cna_texture2d_get_width(new CnaHandle(NativeHandleValue));

    public int Height => Native.cna_texture2d_get_height(new CnaHandle(NativeHandleValue));

    public unsafe void SetData(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        fixed (byte* dataPtr = data)
        {
            CnaResult result = Native.cna_texture2d_set_data(new CnaHandle(NativeHandleValue), dataPtr, (nuint)data.Length);
            CnaException.ThrowIfFailed(result, nameof(SetData));
        }
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}
