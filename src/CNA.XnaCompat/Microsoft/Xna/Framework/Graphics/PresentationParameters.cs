namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>PresentationParameters</c>. See
/// <see cref="BlendState"/>'s own doc comment for the subclass/copy-constructor pattern; the
/// members re-typed here are exactly those whose enums are duplicated per namespace.</summary>
public class PresentationParameters : CNA.Graphics.PresentationParameters
{
    public PresentationParameters()
    {
    }

    internal PresentationParameters(CNA.Graphics.PresentationParameters copyFrom)
        : base(copyFrom)
    {
    }

    public new SurfaceFormat BackBufferFormat
    {
        get => (SurfaceFormat)(int)base.BackBufferFormat;
        set => base.BackBufferFormat = (CNA.Graphics.SurfaceFormat)(int)value;
    }

    public new DepthFormat DepthStencilFormat
    {
        get => (DepthFormat)(int)base.DepthStencilFormat;
        set => base.DepthStencilFormat = (CNA.Graphics.DepthFormat)(int)value;
    }

    public new PresentInterval PresentationInterval
    {
        get => (PresentInterval)(int)base.PresentationInterval;
        set => base.PresentationInterval = (CNA.Graphics.PresentInterval)(int)value;
    }

    public new DisplayOrientation DisplayOrientation
    {
        get => (DisplayOrientation)(int)base.DisplayOrientation;
        set => base.DisplayOrientation = (CNA.DisplayOrientation)(int)value;
    }

    public new RenderTargetUsage RenderTargetUsage
    {
        get => (RenderTargetUsage)(int)base.RenderTargetUsage;
        set => base.RenderTargetUsage = (CNA.Graphics.RenderTargetUsage)(int)value;
    }

    public new PresentationParameters Clone() => new(this);
}
