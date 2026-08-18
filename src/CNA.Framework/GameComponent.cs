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
    private int _suppressedFailureCount;

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

            // Real XNA -- and runtime_components.h, which says "setting it to what it already is
            // does not" raise the event -- only signal an actual change. Raising unconditionally
            // makes a component that re-asserts its flags every frame fire an event every frame.
            if (Enabled == value)
            {
                return;
            }

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

            if (UpdateOrder == value)
            {
                return;
            }

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

        // Rethrows the ORIGINAL exception, not a fresh wrapper around it. Wrapping looks more
        // informative but is a trap here: this method is reachable from inside a callback (a
        // component that reads Enabled from its own Update), so the wrapper would be caught by
        // that callback's own handler and re-captured, growing one InnerException layer per frame
        // without bound. A code-review pass found that. The explanation lives in Data instead,
        // which survives the rethrow and costs nothing.
        int suppressed = _suppressedFailureCount;
        _suppressedFailureCount = 0;

        exception.Data["CnaGameComponent"] =
            $"Thrown from a {GetType().Name} callback. The CNA game-component ABI has no error " +
            "channel (its handlers return void), so it was captured and rethrown at the next " +
            "managed-initiated call on this component." +
            (suppressed > 0 ? $" {suppressed} later failure(s) on this component were dropped." : string.Empty);
        throw exception;
    }

    /// <summary>Disposes the component, then surfaces any callback failure that never got the
    /// chance to be rethrown. A component that only overrides <c>Update</c>/<c>Draw</c> and never
    /// reads <see cref="Enabled"/>/<see cref="UpdateOrder"/> would otherwise capture its first
    /// exception and silently discard it forever -- the exact "fail silently" outcome this
    /// machinery exists to avoid, found by a code-review pass.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        ThrowPendingException();
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

    /// <summary>
    /// Never throws. <see cref="GCHandle.Target"/> throws on a freed handle and a cast of a
    /// recycled slot throws too -- and every caller is an <c>UnmanagedCallersOnly</c> wrapper, so a
    /// throw here would unwind into native code, which is undefined behaviour (CoreCLR fails fast).
    /// An earlier version resolved outside the wrappers' <see langword="try"/> blocks and had
    /// exactly that hole; a code-review pass found it. Returning <see langword="false"/> makes a
    /// stale callback a no-op, which is the only safe answer at this boundary.
    /// </summary>
    private static bool TryResolve(nint context, out GameComponent component)
    {
        component = null!;

        if (context == 0)
        {
            return false;
        }

        try
        {
            GCHandle handle = GCHandle.FromIntPtr(context);
            if (!handle.IsAllocated)
            {
                return false;
            }

            if (handle.Target is not GameComponent resolved)
            {
                return false;
            }

            component = resolved;
            return true;
        }
        catch (Exception)
        {
            // A freed or recycled GCHandle. Nothing can be reported from here -- the callback
            // signatures return void -- and letting it escape would kill the process.
            return false;
        }
    }

    /// <summary>Records <paramref name="exception"/> if nothing is pending yet. Keeps the *first*
    /// failure rather than the latest: once a component is broken, later exceptions are usually
    /// consequences of the first, and the first is the one worth reporting. Every later one is
    /// counted, so <see cref="Dispose()"/> can say how many were dropped rather than leaving the
    /// impression there was only ever one.</summary>
    private void Capture(Exception exception)
    {
        if (PendingException is null)
        {
            PendingException = exception;
        }
        else
        {
            _suppressedFailureCount++;
        }
    }

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
            // Deliberately NOT Dispose(true): the header documents this handler as running "before
            // its handle is released", and cna_game_component_dispose disposes "without releasing
            // its handle" -- so calling cna_game_component_destroy from here would release a handle
            // native is about to keep using. Only the managed-side bookkeeping runs; the handle
            // stays native's to release. A code-review pass found this re-entrant release.
            component.OnNativeDisposed();
        }
        catch (Exception ex)
        {
            component.Capture(ex);
        }
    }

    /// <summary>Managed-side teardown for a disposal native initiated -- see
    /// <see cref="OnDispose"/>. Marks the component disposed and frees the GC root, without
    /// touching the native handle.</summary>
    private void OnNativeDisposed()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }
}
