namespace CNA;

/// <summary>Matches real XNA's <c>IGameComponent</c>: the minimum a <see cref="Game"/> component
/// must offer. <see cref="IUpdateable"/> and <see cref="IDrawable"/> add the per-frame halves on
/// top; a component may implement any combination.</summary>
public interface IGameComponent
{
    void Initialize();
}

/// <summary>Matches real XNA's <c>IUpdateable</c>. The two change events exist so a
/// <see cref="Game"/> can keep its update ordering correct when a component's
/// <see cref="UpdateOrder"/> or <see cref="Enabled"/> changes after it was added -- without them a
/// game would have to re-sort every frame.</summary>
public interface IUpdateable
{
    bool Enabled { get; }

    int UpdateOrder { get; }

    event EventHandler<EventArgs>? EnabledChanged;

    event EventHandler<EventArgs>? UpdateOrderChanged;

    void Update(GameTime gameTime);
}

/// <summary>Matches real XNA's <c>IDrawable</c>. See <see cref="IUpdateable"/> for why the change
/// events are part of the contract rather than an implementation detail.</summary>
public interface IDrawable
{
    bool Visible { get; }

    int DrawOrder { get; }

    event EventHandler<EventArgs>? VisibleChanged;

    event EventHandler<EventArgs>? DrawOrderChanged;

    void Draw(GameTime gameTime);
}
