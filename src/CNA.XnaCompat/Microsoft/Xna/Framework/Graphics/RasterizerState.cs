namespace Microsoft.Xna.Framework.Graphics;

public class RasterizerState : GraphicsResource
{
    internal CNA.Graphics.RasterizerState Framework { get; }

    public RasterizerState()
        : this(new CNA.Graphics.RasterizerState())
    {
    }

    internal RasterizerState(CNA.Graphics.RasterizerState framework)
    {
        Framework = framework;
    }

    public static readonly RasterizerState CullClockwise = new(CNA.Graphics.RasterizerState.CopyOf(CNA.Graphics.RasterizerState.CullClockwise));
    public static readonly RasterizerState CullCounterClockwise = new(CNA.Graphics.RasterizerState.CopyOf(CNA.Graphics.RasterizerState.CullCounterClockwise));
    public static readonly RasterizerState CullNone = new(CNA.Graphics.RasterizerState.CopyOf(CNA.Graphics.RasterizerState.CullNone));

    public CullMode CullMode { get => (CullMode)(int)Framework.CullMode; set => Framework.CullMode = (CNA.Graphics.CullMode)(int)value; }
    public FillMode FillMode { get => (FillMode)(int)Framework.FillMode; set => Framework.FillMode = (CNA.Graphics.FillMode)(int)value; }
    public float DepthBias { get => Framework.DepthBias; set => Framework.DepthBias = value; }
    public float SlopeScaleDepthBias { get => Framework.SlopeScaleDepthBias; set => Framework.SlopeScaleDepthBias = value; }
    public bool MultiSampleAntiAlias { get => Framework.MultiSampleAntiAlias; set => Framework.MultiSampleAntiAlias = value; }
    public bool ScissorTestEnable { get => Framework.ScissorTestEnable; set => Framework.ScissorTestEnable = value; }

    protected override void Dispose(bool arg0) => base.Dispose(arg0);
}
