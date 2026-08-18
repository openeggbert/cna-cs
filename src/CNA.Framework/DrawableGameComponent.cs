using CNA.Graphics;
using CNA.Interop;

namespace CNA;

/// <summary>Matches real XNA's <c>DrawableGameComponent</c>: a <see cref="GameComponent"/> that
/// also draws. Created through <c>cna_drawable_game_component_create</c> rather than the plain
/// route -- the two produce different native components, and only the drawable one is asked to
/// draw or load content, so this is a genuine constructor-time distinction rather than a managed
/// flag.</summary>
public class DrawableGameComponent : GameComponent, IDrawable
{
    public DrawableGameComponent(Game game)
        : base(game, drawable: true)
    {
    }

    public GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

    public bool Visible
    {
        get
        {
            ThrowPendingException();
            CnaResult result = Native.cna_drawable_game_component_get_visible(NativeHandle, out byte value);
            CnaException.ThrowIfFailed(result, nameof(Visible));
            return value != 0;
        }
        set
        {
            ThrowPendingException();
            CnaResult result = Native.cna_drawable_game_component_set_visible(NativeHandle, (byte)(value ? 1 : 0));
            CnaException.ThrowIfFailed(result, nameof(Visible));
            VisibleChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int DrawOrder
    {
        get
        {
            ThrowPendingException();
            CnaResult result = Native.cna_drawable_game_component_get_draw_order(NativeHandle, out int value);
            CnaException.ThrowIfFailed(result, nameof(DrawOrder));
            return value;
        }
        set
        {
            ThrowPendingException();
            CnaResult result = Native.cna_drawable_game_component_set_draw_order(NativeHandle, value);
            CnaException.ThrowIfFailed(result, nameof(DrawOrder));
            DrawOrderChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler<EventArgs>? VisibleChanged;

    public event EventHandler<EventArgs>? DrawOrderChanged;

    public virtual void Draw(GameTime gameTime)
    {
    }

    protected internal virtual void LoadContent()
    {
    }

    protected internal virtual void UnloadContent()
    {
    }
}
