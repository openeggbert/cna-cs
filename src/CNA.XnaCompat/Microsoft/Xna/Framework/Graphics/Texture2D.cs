namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>Texture2D</c>. <c>Width</c>/<c>Height</c>/<c>SetData</c>/
/// <c>Dispose</c> are inherited unchanged from <see cref="CNA.Graphics.Texture2D"/>.</summary>
public class Texture2D : CNA.Graphics.Texture2D
{
    public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
        : base(graphicsDevice, width, height)
    {
    }

    /// <summary>Wraps an already-loaded native handle -- used by <c>ContentManager</c>.</summary>
    protected internal Texture2D(nint nativeHandleValue)
        : base(nativeHandleValue)
    {
    }

    /// <summary>A distinct overload, not an override -- <see cref="Color"/> here is this
    /// namespace's own type, which has no implicit array-to-array conversion to
    /// <c>CNA.Color[]</c> even though individual elements convert (see that struct's own
    /// conversion operators), so the base <see cref="CNA.Graphics.Texture2D.SetData(CNA.Color[])"/>
    /// overload can't bind directly the way <c>SetData(byte[])</c> does.</summary>
    public void SetData(Color[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var converted = new CNA.Color[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            converted[i] = data[i];
        }

        base.SetData(converted);
    }
}
