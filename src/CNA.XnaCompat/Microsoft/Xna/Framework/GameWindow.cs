namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>GameWindow</c>. Subclasses <see cref="CNA.GameWindow"/>, so
/// everything whose type does not diverge between namespaces -- <c>Title</c>,
/// <c>AllowUserResizing</c>, the screen-device-change pair, the three events -- is inherited
/// unchanged. Only <see cref="ClientBounds"/> and <see cref="CurrentOrientation"/> need re-typing,
/// because <c>Rectangle</c> and <c>DisplayOrientation</c> are separate types per namespace.
/// Exists primarily for the covariant-return factory override on <see cref="Game.Window"/> -- see
/// that property's own doc comment.</summary>
public class GameWindow : CNA.GameWindow
{
    internal GameWindow(nint nativeGameHandleValue)
        : base(nativeGameHandleValue)
    {
    }

    public new Rectangle ClientBounds => base.ClientBounds;

    public new DisplayOrientation CurrentOrientation => (DisplayOrientation)(int)base.CurrentOrientation;
}
