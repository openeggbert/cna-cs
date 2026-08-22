namespace Microsoft.Xna.Framework.Graphics;

public class OcclusionQuery : GraphicsResource
{
    private readonly CNA.Graphics.OcclusionQuery _query;

    public OcclusionQuery(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
        _query = new CNA.Graphics.OcclusionQuery(graphicsDevice.Framework);
    }

    public bool IsComplete => _query.IsComplete;

    public int PixelCount => _query.PixelCount;

    public void Begin() => _query.Begin();

    public void End() => _query.End();

    protected override void Dispose(bool arg0)
    {
        if (!IsDisposed)
        {
            _query?.Dispose();
        }

        base.Dispose(arg0);
    }
}
