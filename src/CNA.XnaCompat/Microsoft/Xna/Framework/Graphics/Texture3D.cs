namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>Texture3D</c>. A pure subclass -- <c>Width</c>/<c>Height</c>/
/// <c>Depth</c>/<c>LevelCount</c>/<c>Dispose</c> are all inherited unchanged from
/// <see cref="CNA.Graphics.Texture3D"/>; only the <see cref="SurfaceFormat"/>-typed members need
/// re-typing, since that enum is duplicated per namespace (see its own doc comment).</summary>
public class Texture3D : CNA.Graphics.Texture3D
{
    public Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth)
        : base(graphicsDevice, width, height, depth)
    {
    }

    public Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth, bool mipMap, SurfaceFormat format)
        : base(graphicsDevice, width, height, depth, mipMap, (CNA.Graphics.SurfaceFormat)(int)format)
    {
    }

    public new GraphicsDevice GraphicsDevice => (GraphicsDevice)base.GraphicsDevice;

    public new SurfaceFormat Format => (SurfaceFormat)(int)base.Format;

    /// <summary>Converts element-wise before forwarding -- <see cref="Color"/> here is this
    /// namespace's own type, which converts per element but not array-to-array (see that struct's
    /// own conversion operators), the same limitation <c>Texture2D.SetData</c> documents.</summary>
    public void SetData(Color[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        base.SetData(Convert(data));
    }

    public void SetData(
        int level, int left, int top, int right, int bottom, int front, int back,
        Color[] data, int startIndex, int elementCount)
    {
        ArgumentNullException.ThrowIfNull(data);
        base.SetData(level, left, top, right, bottom, front, back, Convert(data), startIndex, elementCount);
    }

    public new Color[] GetData() => Convert(base.GetData());

    public new Color[] GetData(int level, int left, int top, int right, int bottom, int front, int back) =>
        Convert(base.GetData(level, left, top, right, bottom, front, back));

    private static CNA.Color[] Convert(Color[] source)
    {
        var result = new CNA.Color[source.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = source[i];
        }

        return result;
    }

    private static Color[] Convert(CNA.Color[] source)
    {
        var result = new Color[source.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = source[i];
        }

        return result;
    }

}
