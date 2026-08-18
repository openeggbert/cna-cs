namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>GraphicsAdapter</c>. A pure subclass -- the scalar properties
/// are inherited unchanged; only the members whose types differ per namespace need re-typing. See
/// <see cref="CNA.Graphics.GraphicsAdapter"/> for why the enumeration entry points take a device
/// rather than being static as in real XNA.</summary>
public class GraphicsAdapter : CNA.Graphics.GraphicsAdapter
{
    internal GraphicsAdapter(GraphicsDevice graphicsDevice, uint adapterIndex)
        : base(graphicsDevice, adapterIndex)
    {
    }

    public new DisplayMode CurrentDisplayMode => DisplayMode.FromFramework(base.CurrentDisplayMode);

    public new DisplayModeCollection SupportedDisplayModes => DisplayModeCollection.FromFramework(base.SupportedDisplayModes);

    public bool IsProfileSupported(GraphicsProfile profile) =>
        base.IsProfileSupported((CNA.Graphics.GraphicsProfile)(int)profile);

    /// <summary>Re-typed: <c>GraphicsProfile</c>, <c>SurfaceFormat</c> and <c>DepthFormat</c> are
    /// all separate enums per namespace.</summary>
    public bool QueryRenderTargetFormat(
        GraphicsProfile graphicsProfile,
        SurfaceFormat format,
        DepthFormat depthFormat,
        int multiSampleCount,
        out SurfaceFormat selectedFormat,
        out DepthFormat selectedDepthFormat,
        out int selectedMultiSampleCount)
    {
        bool exact = base.QueryRenderTargetFormat(
            (CNA.Graphics.GraphicsProfile)(int)graphicsProfile,
            (CNA.Graphics.SurfaceFormat)(int)format,
            (CNA.Graphics.DepthFormat)(int)depthFormat,
            multiSampleCount,
            out CNA.Graphics.SurfaceFormat nativeFormat,
            out CNA.Graphics.DepthFormat nativeDepth,
            out selectedMultiSampleCount);

        selectedFormat = (SurfaceFormat)(int)nativeFormat;
        selectedDepthFormat = (DepthFormat)(int)nativeDepth;
        return exact;
    }

    /// <summary>See <see cref="QueryRenderTargetFormat"/>.</summary>
    public bool QueryBackBufferFormat(
        GraphicsProfile graphicsProfile,
        SurfaceFormat format,
        DepthFormat depthFormat,
        int multiSampleCount,
        out SurfaceFormat selectedFormat,
        out DepthFormat selectedDepthFormat,
        out int selectedMultiSampleCount)
    {
        bool exact = base.QueryBackBufferFormat(
            (CNA.Graphics.GraphicsProfile)(int)graphicsProfile,
            (CNA.Graphics.SurfaceFormat)(int)format,
            (CNA.Graphics.DepthFormat)(int)depthFormat,
            multiSampleCount,
            out CNA.Graphics.SurfaceFormat nativeFormat,
            out CNA.Graphics.DepthFormat nativeDepth,
            out selectedMultiSampleCount);

        selectedFormat = (SurfaceFormat)(int)nativeFormat;
        selectedDepthFormat = (DepthFormat)(int)nativeDepth;
        return exact;
    }
}
