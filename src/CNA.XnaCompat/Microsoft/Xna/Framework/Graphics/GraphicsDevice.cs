namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>GraphicsDevice</c>. A pure subclass -- <c>Clear(Color)</c> is inherited
/// unchanged from <see cref="CNA.Graphics.GraphicsDevice"/> and resolves correctly
/// against this namespace's <see cref="Color"/> argument through that struct's implicit
/// conversion operator, so no override is needed here. See docs/architecture.md.
/// </summary>
public class GraphicsDevice : CNA.Graphics.GraphicsDevice
{
    protected internal GraphicsDevice(nint nativeHandleValue)
        : base(nativeHandleValue)
    {
    }
}
