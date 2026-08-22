namespace Microsoft.Xna.Framework.Graphics;

// XNA's runtime metadata uses an internal interface to drive Dynamic*Buffer's virtual/final
// implementation flags. Keeping the same internal seam reproduces those flags without adding a
// game-visible type to the Microsoft.Xna.Framework contract.
internal interface IDynamicGraphicsResource
{
    bool IsContentLost { get; }

    event EventHandler<EventArgs>? ContentLost;
}
