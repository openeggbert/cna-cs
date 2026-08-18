using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// A texture that can be set as the active render target via
/// <see cref="GraphicsDevice.SetRenderTarget"/>. Subclasses <see cref="Texture2D"/> (matching
/// real XNA's <c>RenderTarget2D : Texture2D</c>), but -- now that the real, shipped
/// openeggbert/cna C API confirms <c>RenderTarget2D</c> is its own real resource type upstream
/// (<c>render_target.h</c>: its own <c>create</c>/<c>get_info</c>/<c>destroy</c> routes, none of
/// them <c>cna_texture2d_*</c>) -- overrides <see cref="Texture2D.ReleaseNative"/>/
/// <see cref="Texture2D.Width"/>/<see cref="Texture2D.Height"/> to call those real functions
/// instead of the plain-texture ones it inherits. See <c>NEXT.md</c>'s native-ABI-migration entry,
/// step 5, for why this was previously self-designed against a texture-shaped handle.
///
/// <see cref="ReleaseNativeRenderTarget"/>/<see cref="GetDimensions"/> are <c>internal static</c>
/// (not just the override bodies) specifically so CNA.XnaCompat's own
/// <c>RenderTarget2D</c> -- which inherits from CNA.XnaCompat's own <c>Texture2D</c>, not this
/// type, so it cannot inherit these overrides -- can call the same real native functions without
/// ever naming a CNA.Interop type itself (<see cref="GetDimensions"/> returns a plain tuple, not
/// <see cref="CnaRenderTargetInfo"/>, for exactly that reason -- see docs/architecture.md).
/// </summary>
public class RenderTarget2D : Texture2D
{
    public RenderTarget2D(GraphicsDevice graphicsDevice, int width, int height)
        : base(graphicsDevice, CreateNativeHandle(graphicsDevice, width, height))
    {
    }

    /// <summary>
    /// Creates the native render-target handle without wrapping it. <c>internal</c> (visible to
    /// CNA.XnaCompat via the assembly's <c>InternalsVisibleTo</c> grant) so CNA.XnaCompat's
    /// <c>RenderTarget2D</c> can reuse this same native call while still inheriting from
    /// CNA.XnaCompat's own <c>Texture2D</c> -- matching real XNA's <c>RenderTarget2D : Texture2D</c>
    /// ancestry so <c>Texture2D t = someRenderTarget;</c> compiles in game code -- instead of from
    /// this type, which the usual "derive from the CNA.Framework type, forward a protected internal
    /// ctor" trick would require.
    /// </summary>
    internal static nint CreateNativeHandle(GraphicsDevice graphicsDevice, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        var createInfo = new CnaRenderTarget2DCreateInfo
        {
            Width = (uint)width,
            Height = (uint)height,
            MipMap = 0,
            ColorFormat = 0, // CNA_SURFACE_FORMAT_COLOR
            DepthStencilFormat = 0, // CNA_DEPTH_FORMAT_NONE
            MultiSampleCount = 0,
            Usage = 0, // CNA_RENDER_TARGET_USAGE_DISCARD_CONTENTS
        };

        CnaResult result = Native.cna_render_target2d_create(graphicsDevice.ResolveNativeDeviceHandle(), in createInfo, out CnaHandle handle);
        CnaException.ThrowIfFailed(result, nameof(RenderTarget2D));

        return handle.AsNint;
    }

    /// <summary>Matches <c>cna_render_target_destroy</c> exactly (<c>render_target.h:277</c>).
    /// <c>internal static</c>, not just this override's body -- see this class's own doc comment
    /// for why CNA.XnaCompat's parallel <c>RenderTarget2D</c> needs to call it directly too.</summary>
    internal static void ReleaseNativeRenderTarget(nint handleValue) => Native.cna_render_target_destroy(new CnaHandle(handleValue));

    protected override void ReleaseNative(nint handleValue) => ReleaseNativeRenderTarget(handleValue);

    /// <summary>Matches <c>cna_render_target_get_info</c> exactly (<c>render_target.h:199</c>) --
    /// <c>texture.h</c>'s/<c>graphics.h</c>'s own texture-info routes don't apply to a render
    /// target's real handle type. Returns a plain tuple (not <see cref="CnaRenderTargetInfo"/>) --
    /// see this class's own doc comment.</summary>
    internal static (int Width, int Height) GetDimensions(nint handleValue)
    {
        var info = new CnaRenderTargetInfo();
        CnaResult result = Native.cna_render_target_get_info(new CnaHandle(handleValue), ref info);
        CnaException.ThrowIfFailed(result, "cna_render_target_get_info");
        return ((int)info.Width, (int)info.Height);
    }

    public override int Width => GetDimensions(NativeHandleValue).Width;

    public override int Height => GetDimensions(NativeHandleValue).Height;
}
