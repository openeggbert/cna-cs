namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>SamplerState</c>. See <see cref="BlendState"/>'s own doc comment
/// for the subclass/copy-constructor pattern the six static presets use.</summary>
public class SamplerState : CNA.Graphics.SamplerState
{
    public SamplerState()
    {
    }

    internal SamplerState(CNA.Graphics.SamplerState copyFrom)
        : base(copyFrom)
    {
    }

    public static new SamplerState AnisotropicClamp { get; } = new(CNA.Graphics.SamplerState.AnisotropicClamp);
    public static new SamplerState AnisotropicWrap { get; } = new(CNA.Graphics.SamplerState.AnisotropicWrap);
    public static new SamplerState LinearClamp { get; } = new(CNA.Graphics.SamplerState.LinearClamp);
    public static new SamplerState LinearWrap { get; } = new(CNA.Graphics.SamplerState.LinearWrap);
    public static new SamplerState PointClamp { get; } = new(CNA.Graphics.SamplerState.PointClamp);
    public static new SamplerState PointWrap { get; } = new(CNA.Graphics.SamplerState.PointWrap);

    public new TextureAddressMode AddressU
    {
        get => (TextureAddressMode)(int)base.AddressU;
        set => base.AddressU = (CNA.Graphics.TextureAddressMode)(int)value;
    }

    public new TextureAddressMode AddressV
    {
        get => (TextureAddressMode)(int)base.AddressV;
        set => base.AddressV = (CNA.Graphics.TextureAddressMode)(int)value;
    }

    public new TextureAddressMode AddressW
    {
        get => (TextureAddressMode)(int)base.AddressW;
        set => base.AddressW = (CNA.Graphics.TextureAddressMode)(int)value;
    }

    public new TextureFilter Filter
    {
        get => (TextureFilter)(int)base.Filter;
        set => base.Filter = (CNA.Graphics.TextureFilter)(int)value;
    }
}
