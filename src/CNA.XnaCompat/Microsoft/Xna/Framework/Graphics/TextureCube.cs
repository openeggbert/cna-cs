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
}
