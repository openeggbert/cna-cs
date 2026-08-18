namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>Texture2D</c>.
///
/// Derives from this namespace's own <see cref="Texture"/> rather than from
/// <see cref="CNA.Graphics.Texture2D"/>, so that real XNA's own
/// <c>Texture2D : Texture : GraphicsResource</c> chain holds here too and game code's
/// <c>Texture t = someTexture2D;</c> (or a <c>GraphicsDevice.Textures[0] = someTexture2D;</c>
/// binding, whose collection is typed on <see cref="Texture"/>) compiles. C# has single
/// inheritance, so that ancestry rules out also inheriting <see cref="CNA.Graphics.Texture2D"/>'s
/// implementation -- exactly the constraint <see cref="RenderTarget2D"/> already documents on the
/// other side of the same fork.
///
/// No logic is duplicated to pay for it: every member below is a one-line call into the same
/// <c>internal static</c> helpers on <see cref="CNA.Graphics.Texture2D"/> that its own instance
/// members call, which exist for precisely this purpose (see
/// <see cref="CNA.Graphics.Texture2D.CreateNativeTexture2DHandle"/>'s doc comment). This is the
/// pattern <see cref="CNA.Graphics.RenderTarget2D"/> established for the identical problem.
/// </summary>
public class Texture2D : Texture
{
    public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
        : base(graphicsDevice, CNA.Graphics.Texture2D.CreateNativeTexture2DHandle(graphicsDevice, width, height))
    {
    }

    /// <summary>Wraps an already-loaded native handle -- used by <c>ContentManager</c>.</summary>
    protected internal Texture2D(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : base(graphicsDevice, nativeHandleValue)
    {
    }

    protected override void ReleaseNative(nint handleValue) => CNA.Graphics.Texture2D.ReleaseNativeTexture2D(handleValue);

    public virtual int Width => CNA.Graphics.Texture2D.GetTexture2DDimensions(NativeHandleValue).Width;

    public virtual int Height => CNA.Graphics.Texture2D.GetTexture2DDimensions(NativeHandleValue).Height;

    public void SetData(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        CNA.Graphics.Texture2D.SetDataRgba8(NativeHandleValue, data);
    }

    /// <summary>Converts element-wise before packing -- <see cref="Color"/> here is this
    /// namespace's own type, which converts per element but not array-to-array (see that struct's
    /// own conversion operators).</summary>
    public void SetData(Color[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var converted = new CNA.Color[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            converted[i] = data[i];
        }

        CNA.Graphics.Texture2D.SetDataRgba8(NativeHandleValue, CNA.Graphics.Texture2D.PackColors(converted));
    }
}
