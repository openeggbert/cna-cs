namespace Microsoft.Xna.Framework.Graphics;

public class SamplerState : GraphicsResource
{
    internal CNA.Graphics.SamplerState Framework { get; }

    public SamplerState()
        : this(new CNA.Graphics.SamplerState())
    {
    }

    internal SamplerState(CNA.Graphics.SamplerState framework)
    {
        Framework = framework;
    }

    public static readonly SamplerState AnisotropicClamp = new(CNA.Graphics.SamplerState.CopyOf(CNA.Graphics.SamplerState.AnisotropicClamp));
    public static readonly SamplerState AnisotropicWrap = new(CNA.Graphics.SamplerState.CopyOf(CNA.Graphics.SamplerState.AnisotropicWrap));
    public static readonly SamplerState LinearClamp = new(CNA.Graphics.SamplerState.CopyOf(CNA.Graphics.SamplerState.LinearClamp));
    public static readonly SamplerState LinearWrap = new(CNA.Graphics.SamplerState.CopyOf(CNA.Graphics.SamplerState.LinearWrap));
    public static readonly SamplerState PointClamp = new(CNA.Graphics.SamplerState.CopyOf(CNA.Graphics.SamplerState.PointClamp));
    public static readonly SamplerState PointWrap = new(CNA.Graphics.SamplerState.CopyOf(CNA.Graphics.SamplerState.PointWrap));

    public TextureAddressMode AddressU { get => (TextureAddressMode)(int)Framework.AddressU; set => Framework.AddressU = (CNA.Graphics.TextureAddressMode)(int)value; }
    public TextureAddressMode AddressV { get => (TextureAddressMode)(int)Framework.AddressV; set => Framework.AddressV = (CNA.Graphics.TextureAddressMode)(int)value; }
    public TextureAddressMode AddressW { get => (TextureAddressMode)(int)Framework.AddressW; set => Framework.AddressW = (CNA.Graphics.TextureAddressMode)(int)value; }
    public TextureFilter Filter { get => (TextureFilter)(int)Framework.Filter; set => Framework.Filter = (CNA.Graphics.TextureFilter)(int)value; }
    public int MaxAnisotropy { get => Framework.MaxAnisotropy; set => Framework.MaxAnisotropy = value; }
    public int MaxMipLevel { get => Framework.MaxMipLevel; set => Framework.MaxMipLevel = value; }
    public float MipMapLevelOfDetailBias { get => Framework.MipMapLevelOfDetailBias; set => Framework.MipMapLevelOfDetailBias = value; }

    protected override void Dispose(bool arg0) => base.Dispose(arg0);
}
