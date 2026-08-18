namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>TextureCube</c>. See <see cref="Texture3D"/>'s own doc comment
/// for the pattern.</summary>
public class TextureCube : CNA.Graphics.TextureCube
{
    public TextureCube(GraphicsDevice graphicsDevice, int size)
        : base(graphicsDevice, size)
    {
    }

    public TextureCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
        : base(graphicsDevice, size, mipMap, (CNA.Graphics.SurfaceFormat)(int)format)
    {
    }

    /// <summary>Forwards an already-created handle, for <see cref="RenderTargetCube"/> -- see the
    /// base class's own equivalent constructor.</summary>
    protected TextureCube(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : base(graphicsDevice, nativeHandleValue)
    {
    }

    public new GraphicsDevice GraphicsDevice => (GraphicsDevice)base.GraphicsDevice;

    public new SurfaceFormat Format => (SurfaceFormat)(int)base.Format;

    /// <summary>See <see cref="Texture3D"/> for why the colour arrays convert element-wise.</summary>
    public void SetData(CubeMapFace face, Color[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        base.SetData((CNA.Graphics.CubeMapFace)(int)face, Convert(data));
    }

    public void SetData(CubeMapFace face, int level, Rectangle? rectangle, Color[] data, int startIndex, int elementCount)
    {
        ArgumentNullException.ThrowIfNull(data);
        base.SetData(
            (CNA.Graphics.CubeMapFace)(int)face, level,
            rectangle is null ? null : (CNA.Rectangle)rectangle.Value,
            Convert(data), startIndex, elementCount);
    }

    public Color[] GetData(CubeMapFace face) => Convert(base.GetData((CNA.Graphics.CubeMapFace)(int)face));

    public Color[] GetData(CubeMapFace face, int level, Rectangle? rectangle) =>
        Convert(base.GetData(
            (CNA.Graphics.CubeMapFace)(int)face, level,
            rectangle is null ? null : (CNA.Rectangle)rectangle.Value));

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
