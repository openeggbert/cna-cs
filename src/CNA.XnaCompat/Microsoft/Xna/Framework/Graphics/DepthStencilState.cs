namespace Microsoft.Xna.Framework.Graphics;

public class DepthStencilState : GraphicsResource
{
    private bool _isBound;

    internal CNA.Graphics.DepthStencilState Framework { get; }

    public DepthStencilState()
        : this(new CNA.Graphics.DepthStencilState())
    {
    }

    internal DepthStencilState(CNA.Graphics.DepthStencilState framework)
    {
        Framework = framework;
    }

    private DepthStencilState(CNA.Graphics.DepthStencilState framework, string name)
        : this(framework)
    {
        Name = name;
        _isBound = true;
    }

    public static readonly DepthStencilState Default = new(
        CNA.Graphics.DepthStencilState.CopyOf(CNA.Graphics.DepthStencilState.Default), "DepthStencilState.Default");
    public static readonly DepthStencilState DepthRead = new(
        CNA.Graphics.DepthStencilState.CopyOf(CNA.Graphics.DepthStencilState.DepthRead), "DepthStencilState.DepthRead");
    public static readonly DepthStencilState None = new(
        CNA.Graphics.DepthStencilState.CopyOf(CNA.Graphics.DepthStencilState.None), "DepthStencilState.None");

    public bool DepthBufferEnable { get => Framework.DepthBufferEnable; set { ThrowIfBound(); Framework.DepthBufferEnable = value; } }
    public bool DepthBufferWriteEnable { get => Framework.DepthBufferWriteEnable; set { ThrowIfBound(); Framework.DepthBufferWriteEnable = value; } }
    public bool StencilEnable { get => Framework.StencilEnable; set { ThrowIfBound(); Framework.StencilEnable = value; } }
    public bool TwoSidedStencilMode { get => Framework.TwoSidedStencilMode; set { ThrowIfBound(); Framework.TwoSidedStencilMode = value; } }
    public CompareFunction DepthBufferFunction { get => (CompareFunction)(int)Framework.DepthBufferFunction; set { ThrowIfBound(); Framework.DepthBufferFunction = (CNA.Graphics.CompareFunction)(int)value; } }
    public CompareFunction StencilFunction { get => (CompareFunction)(int)Framework.StencilFunction; set { ThrowIfBound(); Framework.StencilFunction = (CNA.Graphics.CompareFunction)(int)value; } }
    public int StencilMask { get => Framework.StencilMask; set { ThrowIfBound(); Framework.StencilMask = value; } }
    public int StencilWriteMask { get => Framework.StencilWriteMask; set { ThrowIfBound(); Framework.StencilWriteMask = value; } }
    public int ReferenceStencil { get => Framework.ReferenceStencil; set { ThrowIfBound(); Framework.ReferenceStencil = value; } }
    public StencilOperation StencilFail { get => (StencilOperation)(int)Framework.StencilFail; set { ThrowIfBound(); Framework.StencilFail = (CNA.Graphics.StencilOperation)(int)value; } }
    public StencilOperation StencilDepthBufferFail { get => (StencilOperation)(int)Framework.StencilDepthBufferFail; set { ThrowIfBound(); Framework.StencilDepthBufferFail = (CNA.Graphics.StencilOperation)(int)value; } }
    public StencilOperation StencilPass { get => (StencilOperation)(int)Framework.StencilPass; set { ThrowIfBound(); Framework.StencilPass = (CNA.Graphics.StencilOperation)(int)value; } }
    public CompareFunction CounterClockwiseStencilFunction { get => (CompareFunction)(int)Framework.CounterClockwiseStencilFunction; set { ThrowIfBound(); Framework.CounterClockwiseStencilFunction = (CNA.Graphics.CompareFunction)(int)value; } }
    public StencilOperation CounterClockwiseStencilFail { get => (StencilOperation)(int)Framework.CounterClockwiseStencilFail; set { ThrowIfBound(); Framework.CounterClockwiseStencilFail = (CNA.Graphics.StencilOperation)(int)value; } }
    public StencilOperation CounterClockwiseStencilDepthBufferFail { get => (StencilOperation)(int)Framework.CounterClockwiseStencilDepthBufferFail; set { ThrowIfBound(); Framework.CounterClockwiseStencilDepthBufferFail = (CNA.Graphics.StencilOperation)(int)value; } }
    public StencilOperation CounterClockwiseStencilPass { get => (StencilOperation)(int)Framework.CounterClockwiseStencilPass; set { ThrowIfBound(); Framework.CounterClockwiseStencilPass = (CNA.Graphics.StencilOperation)(int)value; } }

    internal void Bind(GraphicsDevice graphicsDevice)
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(DepthStencilState));
        }

        AttachGraphicsDevice(graphicsDevice);
        _isBound = true;
    }

    private void ThrowIfBound()
    {
        if (_isBound)
        {
            throw new InvalidOperationException("The DepthStencilState cannot be modified after it has been bound to a GraphicsDevice.");
        }
    }

    protected override void Dispose(bool arg0) => base.Dispose(arg0);
}
