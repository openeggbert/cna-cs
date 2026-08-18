using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CNA.Interop;

namespace CNA;

/// <summary>
/// Matches real XNA's <c>GameComponent</c>: a self-contained piece of per-frame game logic added
/// to <see cref="Game.Components"/>, which the game then updates automatically in
/// <see cref="UpdateOrder"/> sequence.
///
/// Genuinely native-backed, not a managed reimplementation. The native game owns its own component
/// collection and drives each component through a callback table (<c>runtime_components.h</c>), so
/// a managed-only component model would compile, look right, and never run -- native would iterate
/// its own empty list. That finding is what split WP7 into a managed half (WP7a) and this one.
///
/// <b>Exception handling has no error channel here.</b> Unlike <see cref="Game"/>'s lifecycle
/// callbacks, which return <c>CNA_Result</c> and carry an <c>out_error</c>, every
/// <c>CNA_GameComponentCallbacks</c> handler returns <c>void</c>. A managed exception still must
/// not unwind across the <c>UnmanagedCallersOnly</c> boundary, so the wrappers below catch
/// everything -- but rather than swallow it (which would make a broken component fail silently
/// forever), the first exception is stashed in <see cref="PendingException"/> and rethrown from the
/// next managed-initiated call on this component. That is the closest thing to propagation this
/// ABI shape allows.
/// </summary>
public class GameComponent : IGameComponent, IUpdateable, IDisposable
{
    private readonly CnaHandle _handle;
    private GCHandle _selfHandle;
    private bool _disposed;

    public GameComponent(Game game)
        : this(game, drawable: false)
    {
    }

    private protected unsafe GameComponent(Game game, bool drawable)
    {
        ArgumentNullException.ThrowIfNull(game);
        Game = game;

        _selfHandle = GCHandle.Alloc(this);
        nint context = GCHandle.ToIntPtr(_selfHandle);

        var callbacks = new CnaGameComponentCallbacks
        {
            Initialize = &OnInitialize,
            Update = &OnUpdate,
            Draw = &OnDraw,
            LoadContent = &OnLoadContent,
            UnloadContent = &OnUnloadContent,
            Dispose = &OnDispose,
            Context = context,
        };

        CnaResult result = drawable
            ? Native.cna_drawable_game_component_create(new CnaHandle(game.NativeHandle), in callbacks, out _handle)
            : Native.cna_game_component_create(new CnaHandle(game.NativeHandle), in callbacks, out _handle);

        if (result.IsFailure())
        {
            _selfHandle.Free();
            CnaException.ThrowIfFailed(result, nameof(GameComponent));
        }
    }

    public Game Game { get; }

    internal CnaHandle NativeHandle => _handle;

    /// <summary>The first exception a callback threw, if any -- see this class's own doc comment.
    /// Cleared when rethrown.</summary>
    private Exception? PendingException { get; set; }

    public bool Enabled
    {
        get
        {
            ThrowPendingException();
            CnaResult result = Native.cna_game_component_get_enabled(_handle, out byte value);
            CnaException.ThrowIfFailed(result, nameof(Enabled));
            return value != 0;
        }
        set
        {
            ThrowPendingException();
            CnaResult result = Native.cna_game_component_set_enabled(_handle, (byte)(value ? 1 : 0));
            CnaException.ThrowIfFailed(result, nameof(Enabled));
            EnabledChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int UpdateOrder
    {
        get
        {
            ThrowPendingException();
            CnaResult result = Native.cna_game_component_get_update_order(_handle, out int value);
            CnaException.ThrowIfFailed(result, nameof(UpdateOrder));
            return value;
        }
        set
        {
            ThrowPendingException();
            CnaResult result = Native.cna_game_component_set_update_order(_handle, value);
            CnaException.ThrowIfFailed(result, nameof(UpdateOrder));
            UpdateOrderChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler<EventArgs>? EnabledChanged;

    public event EventHandler<EventArgs>? UpdateOrderChanged;

    public virtual void Initialize()
    {
    }

    public virtual void Update(GameTime gameTime)
    {
    }

    /// <summary>Rethrows and clears whatever a callback threw. Called at the top of every
    /// managed-initiated member so a failure surfaces at the next opportunity rather than being
    /// lost -- see this class's own doc comment for why it cannot be reported at the callback
    /// itself.</summary>
    private protected void ThrowPendingException()
    {
        if (PendingException is null)
        {
            return;
        }

        Exception exception = PendingException;
        PendingException = null;
        throw new InvalidOperationException(
            $"A {GetType().Name} callback threw. The CNA game-component ABI has no error channel " +
            "(its handlers return void), so the exception was captured and is rethrown here.", exception);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Native.cna_game_component_destroy(_handle);

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private static bool TryResolve(nint context, out GameComponent component)
    {
        if (context == 0)
        {
            component = null!;
            return false;
        }

        GCHandle handle = GCHandle.FromIntPtr(context);
        component = (GameComponent)handle.Target!;
        return component is not null;
    }

    /// <summary>Records <paramref name="exception"/> if nothing is pending yet. Keeps the *first*
    /// failure rather than the latest: once a component is broken, later exceptions are usually
    /// consequences of the first, and the first is the one worth reporting.</summary>
    private void Capture(Exception exception) => PendingException ??= exception;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnInitialize(nint context)
    {
        if (!TryResolve(context, out GameComponent component))
        {
            return;
        }

        try
        {
            component.Initialize();
        }
        catch (Exception ex)
        {
            component.Capture(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnUpdate(CnaGameTime* gameTime, nint context)
    {
        if (!TryResolve(context, out GameComponent component) || gameTime is null)
        {
            return;
        }

        try
        {
            component.Update(GameTime.FromNative(*gameTime));
        }
        catch (Exception ex)
        {
            component.Capture(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnDraw(CnaGameTime* gameTime, nint context)
    {
        if (!TryResolve(context, out GameComponent component) || gameTime is null)
        {
            return;
        }

        try
        {
            (component as DrawableGameComponent)?.Draw(GameTime.FromNative(*gameTime));
        }
        catch (Exception ex)
        {
            component.Capture(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnLoadContent(nint context)
    {
        if (!TryResolve(context, out GameComponent component))
        {
            return;
        }

        try
        {
            (component as DrawableGameComponent)?.LoadContent();
        }
        catch (Exception ex)
        {
            component.Capture(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnUnloadContent(nint context)
    {
        if (!TryResolve(context, out GameComponent component))
        {
            return;
        }

        try
        {
            (component as DrawableGameComponent)?.UnloadContent();
        }
        catch (Exception ex)
        {
            component.Capture(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnDispose(nint context)
    {
        if (!TryResolve(context, out GameComponent component))
        {
            return;
        }

        try
        {
            component.Dispose(true);
        }
        catch (Exception ex)
        {
            component.Capture(ex);
        }
    }
}
