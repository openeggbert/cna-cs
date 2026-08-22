namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible drawable component with the required
/// <c>DrawableGameComponent : GameComponent</c> public relationship.</summary>
public class DrawableGameComponent : GameComponent, IDrawable
{
    public DrawableGameComponent(Game game)
        : base(game, drawable: true)
    {
        DrawableInner.VisibleChanged += (_, eventArgs) => OnVisibleChanged(this, eventArgs);
        DrawableInner.DrawOrderChanged += (_, eventArgs) => OnDrawOrderChanged(this, eventArgs);
    }

    private CNA.DrawableGameComponent DrawableInner => (CNA.DrawableGameComponent)Inner;

    public Graphics.GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

    public bool Visible
    {
        get => DrawableInner.Visible;
        set => DrawableInner.Visible = value;
    }

    public int DrawOrder
    {
        get => DrawableInner.DrawOrder;
        set => DrawableInner.DrawOrder = value;
    }

    public event EventHandler<EventArgs>? VisibleChanged;

    public event EventHandler<EventArgs>? DrawOrderChanged;

    public override void Initialize() => base.Initialize();

    public virtual void Draw(GameTime gameTime)
    {
    }

    protected virtual void LoadContent()
    {
    }

    protected virtual void UnloadContent()
    {
    }

    protected override void Dispose(bool disposing) => base.Dispose(disposing);

    internal void InvokeLoadContent() => LoadContent();

    internal void InvokeUnloadContent() => UnloadContent();

    protected virtual void OnDrawOrderChanged(object sender, EventArgs args) =>
        DrawOrderChanged?.Invoke(this, args);

    protected virtual void OnVisibleChanged(object sender, EventArgs args) =>
        VisibleChanged?.Invoke(this, args);
}
