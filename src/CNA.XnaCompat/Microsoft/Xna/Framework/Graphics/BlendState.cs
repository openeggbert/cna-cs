namespace Microsoft.Xna.Framework.Graphics;

public class BlendState : GraphicsResource
{
    internal CNA.Graphics.BlendState Framework { get; }

    public BlendState()
        : this(new CNA.Graphics.BlendState())
    {
    }

    internal BlendState(CNA.Graphics.BlendState framework)
    {
        Framework = framework;
    }

    public static readonly BlendState Opaque = new(CNA.Graphics.BlendState.CopyOf(CNA.Graphics.BlendState.Opaque));
    public static readonly BlendState AlphaBlend = new(CNA.Graphics.BlendState.CopyOf(CNA.Graphics.BlendState.AlphaBlend));
    public static readonly BlendState Additive = new(CNA.Graphics.BlendState.CopyOf(CNA.Graphics.BlendState.Additive));
    public static readonly BlendState NonPremultiplied = new(CNA.Graphics.BlendState.CopyOf(CNA.Graphics.BlendState.NonPremultiplied));

    public BlendFunction AlphaBlendFunction { get => (BlendFunction)(int)Framework.AlphaBlendFunction; set => Framework.AlphaBlendFunction = (CNA.Graphics.BlendFunction)(int)value; }
    public Blend AlphaDestinationBlend { get => (Blend)(int)Framework.AlphaDestinationBlend; set => Framework.AlphaDestinationBlend = (CNA.Graphics.Blend)(int)value; }
    public Blend AlphaSourceBlend { get => (Blend)(int)Framework.AlphaSourceBlend; set => Framework.AlphaSourceBlend = (CNA.Graphics.Blend)(int)value; }
    public BlendFunction ColorBlendFunction { get => (BlendFunction)(int)Framework.ColorBlendFunction; set => Framework.ColorBlendFunction = (CNA.Graphics.BlendFunction)(int)value; }
    public Blend ColorDestinationBlend { get => (Blend)(int)Framework.ColorDestinationBlend; set => Framework.ColorDestinationBlend = (CNA.Graphics.Blend)(int)value; }
    public Blend ColorSourceBlend { get => (Blend)(int)Framework.ColorSourceBlend; set => Framework.ColorSourceBlend = (CNA.Graphics.Blend)(int)value; }
    public ColorWriteChannels ColorWriteChannels { get => (ColorWriteChannels)(int)Framework.ColorWriteChannels; set => Framework.ColorWriteChannels = (CNA.Graphics.ColorWriteChannels)(int)value; }
    public ColorWriteChannels ColorWriteChannels1 { get => (ColorWriteChannels)(int)Framework.ColorWriteChannels1; set => Framework.ColorWriteChannels1 = (CNA.Graphics.ColorWriteChannels)(int)value; }
    public ColorWriteChannels ColorWriteChannels2 { get => (ColorWriteChannels)(int)Framework.ColorWriteChannels2; set => Framework.ColorWriteChannels2 = (CNA.Graphics.ColorWriteChannels)(int)value; }
    public ColorWriteChannels ColorWriteChannels3 { get => (ColorWriteChannels)(int)Framework.ColorWriteChannels3; set => Framework.ColorWriteChannels3 = (CNA.Graphics.ColorWriteChannels)(int)value; }
    public Color BlendFactor { get => Framework.BlendFactor; set => Framework.BlendFactor = value; }
    public int MultiSampleMask { get => Framework.MultiSampleMask; set => Framework.MultiSampleMask = value; }

    protected override void Dispose(bool arg0) => base.Dispose(arg0);
}
