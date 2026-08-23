namespace Microsoft.Xna.Framework.Graphics;

public class BlendState : GraphicsResource
{
    private bool _isBound;

    internal CNA.Graphics.BlendState Framework { get; }

    public BlendState()
        : this(new CNA.Graphics.BlendState())
    {
    }

    internal BlendState(CNA.Graphics.BlendState framework)
    {
        Framework = framework;
    }

    private BlendState(CNA.Graphics.BlendState framework, string name)
        : this(framework)
    {
        Name = name;
        _isBound = true;
    }

    public static readonly BlendState Opaque = new(
        CNA.Graphics.BlendState.CopyOf(CNA.Graphics.BlendState.Opaque), "BlendState.Opaque");
    public static readonly BlendState AlphaBlend = new(
        CNA.Graphics.BlendState.CopyOf(CNA.Graphics.BlendState.AlphaBlend), "BlendState.AlphaBlend");
    public static readonly BlendState Additive = new(
        CNA.Graphics.BlendState.CopyOf(CNA.Graphics.BlendState.Additive), "BlendState.Additive");
    public static readonly BlendState NonPremultiplied = new(
        CNA.Graphics.BlendState.CopyOf(CNA.Graphics.BlendState.NonPremultiplied), "BlendState.NonPremultiplied");

    public BlendFunction AlphaBlendFunction { get => Framework.AlphaBlendFunction.ToCompat(); set { ThrowIfBound(); Framework.AlphaBlendFunction = value.ToFramework(); } }
    public Blend AlphaDestinationBlend { get => (Blend)(int)Framework.AlphaDestinationBlend; set { ThrowIfBound(); Framework.AlphaDestinationBlend = (CNA.Graphics.Blend)(int)value; } }
    public Blend AlphaSourceBlend { get => (Blend)(int)Framework.AlphaSourceBlend; set { ThrowIfBound(); Framework.AlphaSourceBlend = (CNA.Graphics.Blend)(int)value; } }
    public BlendFunction ColorBlendFunction { get => Framework.ColorBlendFunction.ToCompat(); set { ThrowIfBound(); Framework.ColorBlendFunction = value.ToFramework(); } }
    public Blend ColorDestinationBlend { get => (Blend)(int)Framework.ColorDestinationBlend; set { ThrowIfBound(); Framework.ColorDestinationBlend = (CNA.Graphics.Blend)(int)value; } }
    public Blend ColorSourceBlend { get => (Blend)(int)Framework.ColorSourceBlend; set { ThrowIfBound(); Framework.ColorSourceBlend = (CNA.Graphics.Blend)(int)value; } }
    public ColorWriteChannels ColorWriteChannels { get => (ColorWriteChannels)(int)Framework.ColorWriteChannels; set { ThrowIfBound(); Framework.ColorWriteChannels = (CNA.Graphics.ColorWriteChannels)(int)value; } }
    public ColorWriteChannels ColorWriteChannels1 { get => (ColorWriteChannels)(int)Framework.ColorWriteChannels1; set { ThrowIfBound(); Framework.ColorWriteChannels1 = (CNA.Graphics.ColorWriteChannels)(int)value; } }
    public ColorWriteChannels ColorWriteChannels2 { get => (ColorWriteChannels)(int)Framework.ColorWriteChannels2; set { ThrowIfBound(); Framework.ColorWriteChannels2 = (CNA.Graphics.ColorWriteChannels)(int)value; } }
    public ColorWriteChannels ColorWriteChannels3 { get => (ColorWriteChannels)(int)Framework.ColorWriteChannels3; set { ThrowIfBound(); Framework.ColorWriteChannels3 = (CNA.Graphics.ColorWriteChannels)(int)value; } }
    public Color BlendFactor { get => Framework.BlendFactor.ToCompat(); set { ThrowIfBound(); Framework.BlendFactor = value.ToFramework(); } }
    public int MultiSampleMask { get => Framework.MultiSampleMask; set { ThrowIfBound(); Framework.MultiSampleMask = value; } }

    internal void Bind(GraphicsDevice graphicsDevice)
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(BlendState));
        }

        AttachGraphicsDevice(graphicsDevice);
        _isBound = true;
    }

    private void ThrowIfBound()
    {
        if (_isBound)
        {
            throw new InvalidOperationException("The BlendState cannot be modified after it has been bound to a GraphicsDevice.");
        }
    }

    protected override void Dispose(bool arg0) => base.Dispose(arg0);
}
