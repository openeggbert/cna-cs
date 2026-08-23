namespace Microsoft.Xna.Framework.Graphics;

public class SamplerState : GraphicsResource
{
    private bool _isBound;

    internal CNA.Graphics.SamplerState Framework { get; }

    public SamplerState()
        : this(new CNA.Graphics.SamplerState())
    {
    }

    internal SamplerState(CNA.Graphics.SamplerState framework)
    {
        Framework = framework;
    }

    private SamplerState(CNA.Graphics.SamplerState framework, string name)
        : this(framework)
    {
        Name = name;
        _isBound = true;
    }

    public static readonly SamplerState AnisotropicClamp = new(
        CNA.Graphics.SamplerState.CopyOf(CNA.Graphics.SamplerState.AnisotropicClamp), "SamplerState.AnisotropicClamp");
    public static readonly SamplerState AnisotropicWrap = new(
        CNA.Graphics.SamplerState.CopyOf(CNA.Graphics.SamplerState.AnisotropicWrap), "SamplerState.AnisotropicWrap");
    public static readonly SamplerState LinearClamp = new(
        CNA.Graphics.SamplerState.CopyOf(CNA.Graphics.SamplerState.LinearClamp), "SamplerState.LinearClamp");
    public static readonly SamplerState LinearWrap = new(
        CNA.Graphics.SamplerState.CopyOf(CNA.Graphics.SamplerState.LinearWrap), "SamplerState.LinearWrap");
    public static readonly SamplerState PointClamp = new(
        CNA.Graphics.SamplerState.CopyOf(CNA.Graphics.SamplerState.PointClamp), "SamplerState.PointClamp");
    public static readonly SamplerState PointWrap = new(
        CNA.Graphics.SamplerState.CopyOf(CNA.Graphics.SamplerState.PointWrap), "SamplerState.PointWrap");

    public TextureAddressMode AddressU { get => (TextureAddressMode)(int)Framework.AddressU; set { ThrowIfBound(); Framework.AddressU = (CNA.Graphics.TextureAddressMode)(int)value; } }
    public TextureAddressMode AddressV { get => (TextureAddressMode)(int)Framework.AddressV; set { ThrowIfBound(); Framework.AddressV = (CNA.Graphics.TextureAddressMode)(int)value; } }
    public TextureAddressMode AddressW { get => (TextureAddressMode)(int)Framework.AddressW; set { ThrowIfBound(); Framework.AddressW = (CNA.Graphics.TextureAddressMode)(int)value; } }
    public TextureFilter Filter { get => (TextureFilter)(int)Framework.Filter; set { ThrowIfBound(); Framework.Filter = (CNA.Graphics.TextureFilter)(int)value; } }
    public int MaxAnisotropy { get => Framework.MaxAnisotropy; set { ThrowIfBound(); Framework.MaxAnisotropy = value; } }
    public int MaxMipLevel { get => Framework.MaxMipLevel; set { ThrowIfBound(); Framework.MaxMipLevel = value; } }
    public float MipMapLevelOfDetailBias { get => Framework.MipMapLevelOfDetailBias; set { ThrowIfBound(); Framework.MipMapLevelOfDetailBias = value; } }

    internal void Bind(GraphicsDevice graphicsDevice)
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(SamplerState));
        }

        AttachGraphicsDevice(graphicsDevice);
        _isBound = true;
    }

    private void ThrowIfBound()
    {
        if (_isBound)
        {
            throw new InvalidOperationException("The SamplerState cannot be modified after it has been bound to a GraphicsDevice.");
        }
    }

    protected override void Dispose(bool arg0) => base.Dispose(arg0);
}
