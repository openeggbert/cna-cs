namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible <c>IGameComponent</c>. Distinct from
/// <see cref="CNA.IGameComponent"/> rather than an alias: <see cref="IUpdateable"/> and
/// <see cref="IDrawable"/> below take this namespace's <see cref="GameTime"/>, so the three
/// interfaces have to be declared together per namespace. <see cref="Initialize"/> itself has no
/// divergent type, so a component may implement both namespaces' versions if it wants.</summary>
public interface IGameComponent
{
    void Initialize();
}

/// <summary>XNA 4.0-compatible <c>IUpdateable</c>. See <see cref="CNA.IUpdateable"/> for why the
/// change events are part of the contract.</summary>
public interface IUpdateable
{
    bool Enabled { get; }

    int UpdateOrder { get; }

    event EventHandler<EventArgs>? EnabledChanged;

    event EventHandler<EventArgs>? UpdateOrderChanged;

    void Update(GameTime gameTime);
}

/// <summary>XNA 4.0-compatible <c>IDrawable</c>.</summary>
public interface IDrawable
{
    bool Visible { get; }

    int DrawOrder { get; }

    event EventHandler<EventArgs>? VisibleChanged;

    event EventHandler<EventArgs>? DrawOrderChanged;

    void Draw(GameTime gameTime);
}
