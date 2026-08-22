namespace Microsoft.Xna.Framework.Graphics;

public class DepthStencilState : GraphicsResource
{
    internal CNA.Graphics.DepthStencilState Framework { get; }

    public DepthStencilState()
        : this(new CNA.Graphics.DepthStencilState())
    {
    }

    internal DepthStencilState(CNA.Graphics.DepthStencilState framework)
    {
        Framework = framework;
    }

    public static readonly DepthStencilState Default = new(CNA.Graphics.DepthStencilState.CopyOf(CNA.Graphics.DepthStencilState.Default));
    public static readonly DepthStencilState DepthRead = new(CNA.Graphics.DepthStencilState.CopyOf(CNA.Graphics.DepthStencilState.DepthRead));
    public static readonly DepthStencilState None = new(CNA.Graphics.DepthStencilState.CopyOf(CNA.Graphics.DepthStencilState.None));

    public bool DepthBufferEnable { get => Framework.DepthBufferEnable; set => Framework.DepthBufferEnable = value; }
    public bool DepthBufferWriteEnable { get => Framework.DepthBufferWriteEnable; set => Framework.DepthBufferWriteEnable = value; }
    public bool StencilEnable { get => Framework.StencilEnable; set => Framework.StencilEnable = value; }
    public bool TwoSidedStencilMode { get => Framework.TwoSidedStencilMode; set => Framework.TwoSidedStencilMode = value; }
    public CompareFunction DepthBufferFunction { get => (CompareFunction)(int)Framework.DepthBufferFunction; set => Framework.DepthBufferFunction = (CNA.Graphics.CompareFunction)(int)value; }
    public CompareFunction StencilFunction { get => (CompareFunction)(int)Framework.StencilFunction; set => Framework.StencilFunction = (CNA.Graphics.CompareFunction)(int)value; }
    public int StencilMask { get => Framework.StencilMask; set => Framework.StencilMask = value; }
    public int StencilWriteMask { get => Framework.StencilWriteMask; set => Framework.StencilWriteMask = value; }
    public int ReferenceStencil { get => Framework.ReferenceStencil; set => Framework.ReferenceStencil = value; }
    public StencilOperation StencilFail { get => (StencilOperation)(int)Framework.StencilFail; set => Framework.StencilFail = (CNA.Graphics.StencilOperation)(int)value; }
    public StencilOperation StencilDepthBufferFail { get => (StencilOperation)(int)Framework.StencilDepthBufferFail; set => Framework.StencilDepthBufferFail = (CNA.Graphics.StencilOperation)(int)value; }
    public StencilOperation StencilPass { get => (StencilOperation)(int)Framework.StencilPass; set => Framework.StencilPass = (CNA.Graphics.StencilOperation)(int)value; }
    public CompareFunction CounterClockwiseStencilFunction { get => (CompareFunction)(int)Framework.CounterClockwiseStencilFunction; set => Framework.CounterClockwiseStencilFunction = (CNA.Graphics.CompareFunction)(int)value; }
    public StencilOperation CounterClockwiseStencilFail { get => (StencilOperation)(int)Framework.CounterClockwiseStencilFail; set => Framework.CounterClockwiseStencilFail = (CNA.Graphics.StencilOperation)(int)value; }
    public StencilOperation CounterClockwiseStencilDepthBufferFail { get => (StencilOperation)(int)Framework.CounterClockwiseStencilDepthBufferFail; set => Framework.CounterClockwiseStencilDepthBufferFail = (CNA.Graphics.StencilOperation)(int)value; }
    public StencilOperation CounterClockwiseStencilPass { get => (StencilOperation)(int)Framework.CounterClockwiseStencilPass; set => Framework.CounterClockwiseStencilPass = (CNA.Graphics.StencilOperation)(int)value; }

    protected override void Dispose(bool arg0) => base.Dispose(arg0);
}
