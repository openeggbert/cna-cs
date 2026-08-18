namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>GameWindow</c>. A pure subclass -- <c>Title</c> is inherited
/// unchanged from <see cref="CNA.GameWindow"/> (a plain <see cref="string"/>, no type divergence
/// between namespaces), same as <see cref="Graphics.IndexBuffer"/>'s <c>SetData</c>/<c>GetData</c>.
/// Exists purely for the covariant-return factory override on <see cref="Game.Window"/> -- see
/// that property's own doc comment.</summary>
public class GameWindow : CNA.GameWindow
{
    internal GameWindow(nint nativeGameHandleValue)
        : base(nativeGameHandleValue)
    {
    }
}
