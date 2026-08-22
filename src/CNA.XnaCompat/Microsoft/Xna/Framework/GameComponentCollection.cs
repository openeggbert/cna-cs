using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <see cref="Collection{T}"/> of <see cref="IGameComponent"/> values. Native
/// CNA components remain an implementation detail: ordinary interface implementations receive a
/// private adapter, while compat <see cref="GameComponent"/> instances reuse their owned adapter.
/// </summary>
public sealed class GameComponentCollection : Collection<IGameComponent>
{
    private readonly Game? _game;
    private readonly CNA.GameComponentCollection? _native;
    private readonly Dictionary<IGameComponent, CNA.GameComponent> _adapters =
        new(ReferenceEqualityComparer.Instance);

    public GameComponentCollection()
    {
    }

    internal GameComponentCollection(Game game, CNA.GameComponentCollection native)
    {
        _game = game;
        _native = native;
    }

    public event EventHandler<GameComponentCollectionEventArgs>? ComponentAdded;

    public event EventHandler<GameComponentCollectionEventArgs>? ComponentRemoved;

    protected override void InsertItem(int index, IGameComponent item)
    {
        ArgumentNullException.ThrowIfNull(item);
        // Publish managed membership before native invokes Initialize synchronously.
        base.InsertItem(index, item);
        if (_native is null)
        {
            ComponentAdded?.Invoke(this, new GameComponentCollectionEventArgs(item));
            return;
        }

        bool alreadyAdapted = _adapters.ContainsKey(item);
        CNA.GameComponent adapter;
        try
        {
            adapter = GetAdapter(item);
            _native.Insert(index, adapter);
        }
        catch
        {
            base.RemoveItem(index);
            if (!alreadyAdapted && _adapters.Remove(item, out CNA.GameComponent? failedAdapter) &&
                item is not GameComponent)
            {
                failedAdapter.Dispose();
            }

            throw;
        }

        ComponentAdded?.Invoke(this, new GameComponentCollectionEventArgs(item));
    }

    protected override void RemoveItem(int index)
    {
        IGameComponent item = this[index];
        if (_native is not null && !_native.Remove(GetAdapter(item)))
        {
            throw new InvalidOperationException("The native CNA component collection did not contain the managed component.");
        }

        base.RemoveItem(index);
        ComponentRemoved?.Invoke(this, new GameComponentCollectionEventArgs(item));
    }

    protected override void SetItem(int index, IGameComponent item)
    {
        ArgumentNullException.ThrowIfNull(item);
        IGameComponent previous = this[index];
        if (_native is null)
        {
            base.SetItem(index, item);
            ComponentRemoved?.Invoke(this, new GameComponentCollectionEventArgs(previous));
            ComponentAdded?.Invoke(this, new GameComponentCollectionEventArgs(item));
            return;
        }

        CNA.GameComponent previousAdapter = GetAdapter(previous);
        CNA.GameComponent replacementAdapter = GetAdapter(item);

        if (!_native.Remove(previousAdapter))
        {
            throw new InvalidOperationException("The native CNA component collection did not contain the replaced component.");
        }

        try
        {
            _native.Insert(index, replacementAdapter);
        }
        catch
        {
            _native.Insert(index, previousAdapter);
            throw;
        }

        base.SetItem(index, item);
        ComponentRemoved?.Invoke(this, new GameComponentCollectionEventArgs(previous));
        ComponentAdded?.Invoke(this, new GameComponentCollectionEventArgs(item));
    }

    protected override void ClearItems()
    {
        IGameComponent[] removed = this.ToArray();
        _native?.Clear();
        base.ClearItems();
        foreach (IGameComponent item in removed)
        {
            ComponentRemoved?.Invoke(this, new GameComponentCollectionEventArgs(item));
        }
    }

    private CNA.GameComponent GetAdapter(IGameComponent component)
    {
        if (_adapters.TryGetValue(component, out CNA.GameComponent? adapter))
        {
            return adapter;
        }

        adapter = component switch
        {
            GameComponent gameComponent => gameComponent.Inner,
            IDrawable => new InterfaceDrawableAdapter(_game!, component),
            _ => new InterfaceComponentAdapter(_game!, component),
        };
        _adapters.Add(component, adapter);
        return adapter;
    }

    private sealed class InterfaceComponentAdapter : CNA.GameComponent
    {
        private readonly IGameComponent _component;
        private readonly IUpdateable? _updateable;

        internal InterfaceComponentAdapter(Game game, IGameComponent component)
            : base(game)
        {
            _component = component;
            _updateable = component as IUpdateable;
            if (_updateable is not null)
            {
                Enabled = _updateable.Enabled;
                UpdateOrder = _updateable.UpdateOrder;
                _updateable.EnabledChanged += OnEnabledChanged;
                _updateable.UpdateOrderChanged += OnUpdateOrderChanged;
            }
        }

        public override void Initialize() => _component.Initialize();

        public override void Update(CNA.GameTime gameTime) =>
            _updateable?.Update(GameTime.FromFramework(gameTime));

        protected override void Dispose(bool disposing)
        {
            if (_updateable is not null)
            {
                _updateable.EnabledChanged -= OnEnabledChanged;
                _updateable.UpdateOrderChanged -= OnUpdateOrderChanged;
            }

            base.Dispose(disposing);
        }

        private void OnEnabledChanged(object? sender, EventArgs eventArgs) => Enabled = _updateable!.Enabled;

        private void OnUpdateOrderChanged(object? sender, EventArgs eventArgs) => UpdateOrder = _updateable!.UpdateOrder;
    }

    private sealed class InterfaceDrawableAdapter : CNA.DrawableGameComponent
    {
        private readonly IGameComponent _component;
        private readonly IUpdateable? _updateable;
        private readonly IDrawable _drawable;

        internal InterfaceDrawableAdapter(Game game, IGameComponent component)
            : base(game)
        {
            _component = component;
            _updateable = component as IUpdateable;
            _drawable = (IDrawable)component;
            if (_updateable is not null)
            {
                Enabled = _updateable.Enabled;
                UpdateOrder = _updateable.UpdateOrder;
                _updateable.EnabledChanged += OnEnabledChanged;
                _updateable.UpdateOrderChanged += OnUpdateOrderChanged;
            }

            Visible = _drawable.Visible;
            DrawOrder = _drawable.DrawOrder;
            _drawable.VisibleChanged += OnVisibleChanged;
            _drawable.DrawOrderChanged += OnDrawOrderChanged;
        }

        public override void Initialize() => _component.Initialize();

        public override void Update(CNA.GameTime gameTime) =>
            _updateable?.Update(GameTime.FromFramework(gameTime));

        public override void Draw(CNA.GameTime gameTime) =>
            _drawable.Draw(GameTime.FromFramework(gameTime));

        protected override void Dispose(bool disposing)
        {
            if (_updateable is not null)
            {
                _updateable.EnabledChanged -= OnEnabledChanged;
                _updateable.UpdateOrderChanged -= OnUpdateOrderChanged;
            }

            _drawable.VisibleChanged -= OnVisibleChanged;
            _drawable.DrawOrderChanged -= OnDrawOrderChanged;
            base.Dispose(disposing);
        }

        private void OnEnabledChanged(object? sender, EventArgs eventArgs) => Enabled = _updateable!.Enabled;

        private void OnUpdateOrderChanged(object? sender, EventArgs eventArgs) => UpdateOrder = _updateable!.UpdateOrder;

        private void OnVisibleChanged(object? sender, EventArgs eventArgs) => Visible = _drawable.Visible;

        private void OnDrawOrderChanged(object? sender, EventArgs eventArgs) => DrawOrder = _drawable.DrawOrder;
    }
}
