using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// A texture that can be set as the active render target via
/// <see cref="GraphicsDevice.SetRenderTarget"/>. Subclasses <see cref="Texture2D"/> (matching
/// real XNA's <c>RenderTarget2D : Texture2D</c>) rather than duplicating <c>Width</c>/
/// <c>Height</c>/<c>Dispose</c>: the native handle this wraps is texture-shaped, just created
/// through a render-target-specific factory function, so ordinary <c>Texture2D</c> release/getter
/// calls work on it unchanged. No ABI shape for render targets exists anywhere in the analysis
/// docs -- this is self-designed for this repository, following the general handle/result
/// conventions used everywhere else. See NEXT.md for the full caveat.
/// </summary>
public class RenderTarget2D : Texture2D
{
    public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height)
        : base(CreateNativeHandle(graphicsDevice, width, height))
    {
    }

    /// <summary>
    /// Creates the native render-target-usage texture handle without wrapping it.
    /// <c>internal</c> (visible to CNA.XnaCompat via the assembly's
    /// <c>InternalsVisibleTo</c> grant) so CNA.XnaCompat's <c>RenderTarget2D</c> can reuse this
    /// same native call while still inheriting from CNA.XnaCompat's own <c>Texture2D</c> --
    /// matching real XNA's <c>RenderTarget2D : Texture2D</c> ancestry so
    /// <c>Texture2D t = someRenderTarget;</c> compiles in game code -- instead of from this type,
    /// which the usual "derive from the CNA.Framework type, forward a protected internal ctor"
    /// trick would require.
    /// </summary>
    internal static nint CreateNativeHandle(GraphicsDevice graphicsDevice, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        CnaResult result = Native.cna_render_target2d_create(
            new CnaHandle(graphicsDevice.NativeHandleValue), width, height, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(RenderTarget2D));

        return handle.Value;
    }
}
