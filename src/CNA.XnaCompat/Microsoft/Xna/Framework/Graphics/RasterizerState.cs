namespace Microsoft.Xna.Framework.Graphics;

public class RasterizerState : GraphicsResource
{
    private bool _isBound;

    internal CNA.Graphics.RasterizerState Framework { get; }

    public RasterizerState()
        : this(new CNA.Graphics.RasterizerState())
    {
    }

    internal RasterizerState(CNA.Graphics.RasterizerState framework)
    {
        Framework = framework;
    }

    private RasterizerState(CNA.Graphics.RasterizerState framework, string name)
        : this(framework)
    {
        Name = name;
        _isBound = true;
    }

    public static readonly RasterizerState CullClockwise = new(
        CNA.Graphics.RasterizerState.CopyOf(CNA.Graphics.RasterizerState.CullClockwise), "RasterizerState.CullClockwise");
    public static readonly RasterizerState CullCounterClockwise = new(
        CNA.Graphics.RasterizerState.CopyOf(CNA.Graphics.RasterizerState.CullCounterClockwise), "RasterizerState.CullCounterClockwise");
    public static readonly RasterizerState CullNone = new(
        CNA.Graphics.RasterizerState.CopyOf(CNA.Graphics.RasterizerState.CullNone), "RasterizerState.CullNone");

    public CullMode CullMode { get => (CullMode)(int)Framework.CullMode; set { ThrowIfBound(); Framework.CullMode = (CNA.Graphics.CullMode)(int)value; } }
    public FillMode FillMode { get => (FillMode)(int)Framework.FillMode; set { ThrowIfBound(); Framework.FillMode = (CNA.Graphics.FillMode)(int)value; } }
    public float DepthBias { get => Framework.DepthBias; set { ThrowIfBound(); Framework.DepthBias = value; } }
    public float SlopeScaleDepthBias { get => Framework.SlopeScaleDepthBias; set { ThrowIfBound(); Framework.SlopeScaleDepthBias = value; } }
    public bool MultiSampleAntiAlias { get => Framework.MultiSampleAntiAlias; set { ThrowIfBound(); Framework.MultiSampleAntiAlias = value; } }
    public bool ScissorTestEnable { get => Framework.ScissorTestEnable; set { ThrowIfBound(); Framework.ScissorTestEnable = value; } }

    internal void Bind(GraphicsDevice graphicsDevice)
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(RasterizerState));
        }

        AttachGraphicsDevice(graphicsDevice);
        _isBound = true;
    }

    private void ThrowIfBound()
    {
        if (_isBound)
        {
            throw new InvalidOperationException("The RasterizerState cannot be modified after it has been bound to a GraphicsDevice.");
        }
    }

    protected override void Dispose(bool arg0) => base.Dispose(arg0);
}
