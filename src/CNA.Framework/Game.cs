using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CNA.Content;
using CNA.Graphics;
using CNA.Interop;

namespace CNA;

/// <summary>
/// Base class for a CNA game. Native CNA owns window creation, platform event pumping, timing,
/// and the frame lifecycle; it calls back into this class at coarse per-frame boundaries through
/// <see cref="CnaManagedGameCallbacks"/> (the main lifecycle table) and
/// <see cref="CnaGameFrameHooks"/> (a second, optional table this class uses only for
/// <see cref="Initialize"/> today) -- see <c>NEXT.md</c>'s native-ABI-migration entry for why this
/// class now builds two native tables and makes two native calls during construction instead of
/// one.
///
/// Every real lifecycle callback returns <c>CNA_Result</c> and can report a diagnostic through an
/// <c>out_error</c> parameter -- a managed exception must never unwind across the
/// <c>UnmanagedCallersOnly</c> boundary that is undefined behavior, so every callback wrapper below
/// catches, converts to <c>CNA_RESULT_CALLBACK</c>, and reports the exception's message through
/// <see cref="ReportCallbackFailure"/>, which encodes it into <see cref="_callbackErrorBuffer"/>: a
/// buffer allocated once, pinned for this <see cref="Game"/> instance's whole lifetime via
/// <see cref="GC.AllocateArray{T}(int, bool)"/>. That pinning choice is deliberate, not incidental
/// -- <c>CNA_CallbackError.message</c> is only guaranteed valid until the *callback* returns, but a
/// `fixed`-pinned pointer built inside a *callee* method (rather than the
/// <c>UnmanagedCallersOnly</c> wrapper itself) stops being reachable the instant that callee
/// returns, since nothing keeps the byte array rooted after its only reference (a local variable)
/// goes out of scope -- the array could be collected before native ever reads the pointer. A
/// buffer pinned for the whole <see cref="Game"/> instance's lifetime sidesteps that lifetime
/// question entirely instead of relying on a `fixed` block's lexical scope lining up exactly right.
/// </summary>
/// <remarks>
/// Concrete, as in XNA -- <c>Game</c> there is an ordinary class, and <c>new Game()</c> is legal
/// even though almost every game subclasses it. This was <see langword="abstract"/> with no
/// abstract members, which is a divergence that compiles fine and only bites a caller that tries
/// to instantiate it. Found while writing an integration test that wanted a bare game to read
/// launch parameters from.
/// </remarks>
public class Game : IDisposable
{
    /// <summary>Matches real XNA's own default <c>TargetElapsedTime</c> (1/60 second, expressed in
    /// 100ns ticks) -- also the exact value the real C API's own lifecycle smoke test uses for the
    /// same field, confirmed by reading it directly.</summary>
    private const long DefaultTargetElapsedTimeTicks = 166_667;

    private const int CallbackErrorBufferSize = 512;

    private readonly CnaHandle _nativeHandle;
    private readonly byte[] _callbackErrorBuffer = GC.AllocateArray<byte>(CallbackErrorBufferSize, pinned: true);
    /// <summary>Why the previous game could not be released, if it could not. Static because the
    /// consequence is global: native allows one C-owned game at a time, so a game that fails to
    /// release blocks the next one -- and the next one is where the message is finally useful.</summary>
    private static string? _lastDestroyFailure;

    /// <summary>
    /// The handle of a game whose destroy was refused, kept so the attempt can be repeated.
    ///
    /// Safe to retry precisely because the refusal is a precondition failure: native answers
    /// INVALID_STATE when a resource created against the game is still alive, and nothing has been
    /// released at that point. Retrying a destroy that *succeeded* would be undefined -- so this is
    /// only ever set when the call failed.
    /// </summary>
    private static CnaHandle _undestroyedGame = CnaHandle.Zero;

    private GCHandle _selfHandle;
    private bool _graphicsDeviceInitialized;
    private bool _disposed;

    /// <summary>Public, as in XNA, now that this class is concrete. It was <c>protected</c>, which
    /// is the matching half of the same divergence.</summary>
    public unsafe Game()
    {
        _selfHandle = GCHandle.Alloc(this);
        nint context = GCHandle.ToIntPtr(_selfHandle);

        var callbacks = new CnaManagedGameCallbacks
        {
            LoadContent = &OnLoadContent,
            Update = &OnUpdate,
            Draw = &OnDraw,
            UnloadContent = &OnUnloadContent,
            Context = context,
        };

        var createInfo = new CnaGameCreateInfo
        {
            IsFixedTimeStep = 1,
            TargetElapsedTimeTicks = DefaultTargetElapsedTimeTicks,
            WindowTitle = default,
            Callbacks = &callbacks,
        };

        // Before the first real native call, and cheap after the first game: a library from a
        // different ABI generation would otherwise be used anyway and fail later as a garbled
        // struct or a wrong handle, rather than as a message naming the mismatch.
        CnaAbi.EnsureCompatible();
        NativeResourceHandle.DrainPendingReleasesForCurrentThread();

        CnaResult result = Native.cna_game_create(in createInfo, out _nativeHandle);

        // A previous game that refused to be destroyed still holds the one C-owned slot, and the
        // usual reason is timing rather than a real leak: wrappers whose XNA counterparts are not
        // IDisposable -- EffectTechnique, EffectPass and friends -- are reclaimed by SafeHandle's
        // critical finalizer, which had not run yet when the game was disposed.
        //
        // So reclaim and try once more, rather than reporting a failure whose cause has already
        // resolved itself. Retrying the *destroy* is safe here for the reason _undestroyedGame
        // documents: it only holds a handle whose destroy was refused, and a refusal releases
        // nothing.
        if (result.IsFailure() && _undestroyedGame != CnaHandle.Zero)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            NativeResourceHandle.DrainPendingReleasesForCurrentThread();

            if (Native.cna_game_destroy(_undestroyedGame).IsSuccess())
            {
                _undestroyedGame = CnaHandle.Zero;
                _lastDestroyFailure = null;
                result = Native.cna_game_create(in createInfo, out _nativeHandle);
            }
        }

        if (result.IsFailure())
        {
            _selfHandle.Free();
            CnaException.ThrowIfFailed(
                result,
                _lastDestroyFailure is { } previous ? $"cna_game_create ({previous})" : "cna_game_create");
        }

        var hooks = new CnaGameFrameHooks
        {
            Initialize = &OnInitialize,
            Context = context,
        };

        result = Native.cna_game_set_frame_hooks_ext(_nativeHandle, in hooks);
        if (result.IsFailure())
        {
            Native.cna_game_destroy(_nativeHandle);
            _selfHandle.Free();
            CnaException.ThrowIfFailed(result, "cna_game_set_frame_hooks_ext");
        }

        CnaAmbientGame.Current = _nativeHandle;
        Content = CreateContentManager();
    }

    /// <summary>
    /// The raw native game handle value, for the small set of CNA types (currently none
    /// outside this file) that need it. Deliberately typed as <see cref="nint"/>, not
    /// <see cref="CnaHandle"/>, so it can appear in <c>protected</c> members that CNA.XnaCompat's
    /// subclass overrides touch -- see <see cref="GetNativeGraphicsDeviceHandle"/> and
    /// <see cref="GetNativeContentHandle"/> below, and docs/architecture.md. <see cref="CnaHandle"/>'s
    /// own backing field is a fixed-width <see cref="ulong"/> (matching the real ABI's
    /// <c>CNA_Handle</c> exactly), so this narrows with an explicit, checked-at-the-boundary cast
    /// rather than an implicit one -- see <see cref="CnaHandle"/>'s own doc comment.
    /// </summary>
    internal nint NativeHandle => _nativeHandle.AsNint;

    /// <summary>Matches real XNA's <c>Game.Services</c>: the shared service container components
    /// use to find each other. Created eagerly rather than lazily because
    /// <see cref="GraphicsDeviceManager"/> registers itself into it from its own constructor,
    /// which a game runs before anything reads this.</summary>
    public GameServiceContainer Services { get; } = new();

    private GameComponentCollection? _components;

    /// <summary>Matches real XNA's <c>Game.Components</c>. A view over the native collection the
    /// game already owns -- see <see cref="GameComponentCollection"/> for why it is not a managed
    /// list.</summary>
    public GameComponentCollection Components => _components ??= new GameComponentCollection(this);

    public ContentManager Content { get; }

    public GraphicsDevice GraphicsDevice { get; protected set; } = null!;

    private GameWindow? _window;

    /// <summary>Covariant-return factory hook, same pattern as
    /// <see cref="CreateGraphicsDevice"/>/<see cref="CreateContentManager"/> -- CNA.XnaCompat's
    /// <c>Game</c> overrides <see cref="CreateWindow"/> to return a
    /// <c>Microsoft.Xna.Framework.GameWindow</c> instead.</summary>
    public GameWindow Window => _window ??= CreateWindow();

    protected virtual GameWindow CreateWindow() => new(NativeHandle);

    /// <summary>Matches <c>cna_game_get_is_mouse_visible</c>/<c>_set_is_mouse_visible</c> exactly
    /// (<c>runtime.h:300,309</c>) -- both take "Active owned or callback-borrowed" handles, so this
    /// is safe to call any time, including from a game's own constructor (see
    /// <c>HelloGame</c> in cna-cs-template, which does exactly that).</summary>
    public bool IsMouseVisible
    {
        get
        {
            CnaResult result = Native.cna_game_get_is_mouse_visible(_nativeHandle, out byte visible);
            CnaException.ThrowIfFailed(result, nameof(IsMouseVisible));
            return visible != 0;
        }
        set
        {
            CnaResult result = Native.cna_game_set_is_mouse_visible(_nativeHandle, (byte)(value ? 1 : 0));
            CnaException.ThrowIfFailed(result, nameof(IsMouseVisible));
        }
    }

    /// <summary>Whether this game currently has focus. XNA games use it to pause when the player
    /// alt-tabs away.</summary>
    public bool IsActive => ReadBool(Native.cna_game_get_is_active, nameof(IsActive));

    /// <summary>Whether <c>Update</c> is called at a fixed rate (<see cref="TargetElapsedTime"/>)
    /// or as fast as the loop runs.</summary>
    public bool IsFixedTimeStep
    {
        get => ReadBool(Native.cna_game_get_is_fixed_time_step, nameof(IsFixedTimeStep));
        set
        {
            CnaResult result = Native.cna_game_set_is_fixed_time_step(_nativeHandle, value ? (byte)1 : (byte)0);
            CnaException.ThrowIfFailed(result, nameof(IsFixedTimeStep));
        }
    }

    /// <summary>The interval between fixed-step updates. Only meaningful while
    /// <see cref="IsFixedTimeStep"/> is set.</summary>
    public TimeSpan TargetElapsedTime
    {
        get => TimeSpan.FromTicks(ReadTicks(Native.cna_game_get_target_elapsed_time_ticks, nameof(TargetElapsedTime)));
        set
        {
            CnaResult result = Native.cna_game_set_target_elapsed_time_ticks(_nativeHandle, value.Ticks);
            CnaException.ThrowIfFailed(result, nameof(TargetElapsedTime));
        }
    }

    /// <summary>How long the loop sleeps between frames while the game is not
    /// <see cref="IsActive"/> -- how an XNA game stops burning CPU in the background.</summary>
    public TimeSpan InactiveSleepTime
    {
        get => TimeSpan.FromTicks(ReadTicks(Native.cna_game_get_inactive_sleep_time_ticks, nameof(InactiveSleepTime)));
        set
        {
            CnaResult result = Native.cna_game_set_inactive_sleep_time_ticks(_nativeHandle, value.Ticks);
            CnaException.ThrowIfFailed(result, nameof(InactiveSleepTime));
        }
    }

    /// <summary>
    /// The launch parameters the game was started with -- real XNA's
    /// <c>Dictionary&lt;string, string&gt;</c>, materialised from native.
    ///
    /// This used to parse <see cref="Environment.GetCommandLineArgs"/> instead, because the ABI
    /// addressed a parameter <b>by key only</b> and a count with no key list cannot build a map.
    /// Parsing the process command line answered correctly for the launch set and diverged the
    /// moment anything called <see cref="AddLaunchParameter"/> at run time -- a parameter added
    /// there reached native and never appeared here.
    ///
    /// <c>cna_game_launch_parameters_get_key_size</c>/<c>_copy_key</c> closed that. Native is now
    /// the single source, so a run-time addition shows up like any other entry and
    /// <see cref="ContainsLaunchParameter"/>/<see cref="GetLaunchParameter"/> can no longer
    /// disagree with this dictionary.
    ///
    /// Keys come back sorted by name, ordinal ascending -- the ABI sorts deliberately, because the
    /// underlying container is a hash map whose traversal order is unspecified and where a single
    /// insertion can rehash and reorder everything. Sorting makes an index a function of the key
    /// set alone. Adding a parameter still invalidates indices taken before it, which is why this
    /// reads the count and walks it in one go rather than caching anything.
    /// </summary>
    public unsafe LaunchParameters LaunchParameters
    {
        get
        {
            CnaResult countResult = Native.cna_game_launch_parameters_get_count(_nativeHandle, out ulong count);
            CnaException.ThrowIfFailed(countResult, nameof(LaunchParameters));

            var parameters = new LaunchParameters();
            for (ulong i = 0; i < count; i++)
            {
                ulong index = i;
                string key = NativeStringReader.ReadIndexed(
                    static (CnaHandle game, ulong at, out ulong bytes) =>
                        Native.cna_game_launch_parameters_get_key_size(game, at, out bytes),
                    static (CnaHandle game, ulong at, byte* destination, ulong capacity, out ulong bytes) =>
                        Native.cna_game_launch_parameters_copy_key(game, at, destination, capacity, out bytes),
                    _nativeHandle,
                    index,
                    nameof(LaunchParameters));

                parameters[key] = GetLaunchParameter(key) ?? string.Empty;
            }

            return parameters;
        }
    }

    /// <summary>Discards the time accumulated since the last frame, so the next
    /// <c>Update</c> does not try to catch up. What a game calls after a long load, to stop the
    /// fixed-step loop from firing a burst of updates.</summary>
    public void ResetElapsedTime() => Invoke(Native.cna_game_reset_elapsed_time, nameof(ResetElapsedTime));

    /// <summary>Skips this frame's <c>Draw</c>. Real XNA's own way to say "nothing changed".</summary>
    public void SuppressDraw() => Invoke(Native.cna_game_suppress_draw, nameof(SuppressDraw));

    /// <summary>Runs one iteration of the loop by hand. For a host that drives the game itself
    /// rather than handing control to <see cref="Run"/>.</summary>
    public void Tick() => Invoke(Native.cna_game_tick, nameof(Tick));

    /// <summary>Raised when the game gains focus. See
    /// <see cref="GraphicsDeviceManager.DeviceCreated"/> for why the native subscription is taken on
    /// the first <c>+=</c> and held until disposal.</summary>
    public event EventHandler<EventArgs>? Activated
    {
        add { EnsureSubscribed(CnaGameEvent.Activated); _activated += value; }
        remove => _activated -= value;
    }

    /// <summary>Raised when the game loses focus.</summary>
    public event EventHandler<EventArgs>? Deactivated
    {
        add { EnsureSubscribed(CnaGameEvent.Deactivated); _deactivated += value; }
        remove => _deactivated -= value;
    }

    /// <summary>Raised as the game is disposed.</summary>
    public event EventHandler<EventArgs>? Disposed
    {
        add { EnsureSubscribed(CnaGameEvent.Disposed); _disposedEvent += value; }
        remove => _disposedEvent -= value;
    }

    /// <summary>Raised when the game is exiting, before the loop stops.</summary>
    public event EventHandler<EventArgs>? Exiting
    {
        add { EnsureSubscribed(CnaGameEvent.Exiting); _exiting += value; }
        remove => _exiting -= value;
    }

    private EventHandler<EventArgs>? _activated;
    private EventHandler<EventArgs>? _deactivated;
    private EventHandler<EventArgs>? _disposedEvent;
    private EventHandler<EventArgs>? _exiting;

    private readonly NativeEventBridge?[] _eventBridges =
        new NativeEventBridge?[(int)CnaGameEvent.Exiting + 1];

    private void EnsureSubscribed(CnaGameEvent which)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        int index = (int)which;
        if (_eventBridges[index] is not null)
        {
            return;
        }

        _eventBridges[index] = NativeEventBridge.Subscribe(
            () => RaiseGameEvent(which),
            (callback, context) =>
            {
                CnaResult result = Native.cna_game_subscribe(
                    _nativeHandle, (uint)which, callback, context, out CnaHandle registration);
                CnaException.ThrowIfFailed(result, nameof(EnsureSubscribed));
                return registration;
            },
            registration => Native.cna_game_unsubscribe(registration));
    }

    private void RaiseGameEvent(CnaGameEvent which)
    {
        EventHandler<EventArgs>? handler = which switch
        {
            CnaGameEvent.Activated => _activated,
            CnaGameEvent.Deactivated => _deactivated,
            CnaGameEvent.Disposed => _disposedEvent,
            CnaGameEvent.Exiting => _exiting,
            _ => null,
        };

        handler?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Whether native holds a launch parameter with this key. Authoritative where
    /// <see cref="LaunchParameters"/> is not -- see its own doc comment.</summary>
    public bool ContainsLaunchParameter(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        byte present = 0;
        CnaResult result = CnaStringMarshal.WithStringView(
            key, view => Native.cna_game_launch_parameters_contains_key(_nativeHandle, view, out present));
        CnaException.ThrowIfFailed(result, nameof(ContainsLaunchParameter));
        return present != 0;
    }

    /// <summary>One launch parameter's value, or <see langword="null"/> when the key is
    /// absent.</summary>
    public unsafe string? GetLaunchParameter(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!ContainsLaunchParameter(key))
        {
            return null;
        }

        ulong byteCount = 0;
        CnaResult sizeResult = CnaStringMarshal.WithStringView(
            key, view => Native.cna_game_launch_parameters_get_value_size(_nativeHandle, view, out byteCount));
        CnaException.ThrowIfFailed(sizeResult, nameof(GetLaunchParameter));

        if (byteCount == 0)
        {
            return string.Empty;
        }

        byte[] buffer = new byte[byteCount];
        ulong written = 0;
        fixed (byte* bufferPtr = buffer)
        {
            byte* pinned = bufferPtr;
            CnaResult copyResult = CnaStringMarshal.WithStringView(
                key,
                view => Native.cna_game_launch_parameters_copy_value(_nativeHandle, view, pinned, byteCount, out written));
            CnaException.ThrowIfFailed(copyResult, nameof(GetLaunchParameter));
        }

        return System.Text.Encoding.UTF8.GetString(buffer, 0, (int)written);
    }

    /// <summary>Adds a launch parameter. <c>CNAEXT</c> -- real XNA's <c>LaunchParameters</c> is
    /// populated only by the platform, but the ABI exposes an add route and a host driving the game
    /// itself has no other way to supply one.</summary>
    public void AddLaunchParameter(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        CnaResult result = CnaStringMarshal.WithStringView(
            key, keyView => CnaStringMarshal.WithStringView(
                value, valueView => Native.cna_game_launch_parameters_add(_nativeHandle, keyView, valueView)));
        CnaException.ThrowIfFailed(result, nameof(AddLaunchParameter));
    }

    private delegate CnaResult BoolGetter(CnaHandle game, out byte outValue);

    private delegate CnaResult TicksGetter(CnaHandle game, out long outTicks);

    private delegate CnaResult VoidCall(CnaHandle game);

    private bool ReadBool(BoolGetter getter, string context)
    {
        CnaResult result = getter(_nativeHandle, out byte value);
        CnaException.ThrowIfFailed(result, context);
        return value != 0;
    }

    private long ReadTicks(TicksGetter getter, string context)
    {
        CnaResult result = getter(_nativeHandle, out long ticks);
        CnaException.ThrowIfFailed(result, context);
        return ticks;
    }

    private void Invoke(VoidCall call, string context)
    {
        NativeResourceHandle.DrainPendingReleasesForCurrentThread();
        CnaResult result = call(_nativeHandle);
        CnaException.ThrowIfFailed(result, context);
    }

    /// <summary>Hands control to native CNA. Blocks until the game exits.</summary>
    public void Run()
    {
        NativeResourceHandle.DrainPendingReleasesForCurrentThread();
        CnaResult result = Native.cna_game_run(_nativeHandle);
        NativeResourceHandle.DrainPendingReleasesForCurrentThread();
        CnaException.ThrowIfFailed(result, "cna_game_run");
    }

    /// <summary>Runs a single frame -- update and draw -- without entering the loop. Matches real
    /// XNA's <c>RunOneFrame</c>, and is what a host driving the game itself uses alongside
    /// <see cref="Tick"/>.</summary>
    public void RunOneFrame() => Invoke(Native.cna_game_run_one_frame, nameof(RunOneFrame));

    public void Exit()
    {
        CnaResult result = Native.cna_game_request_exit(_nativeHandle);
        CnaException.ThrowIfFailed(result, "cna_game_request_exit");
    }

    protected virtual void Initialize()
    {
    }

    protected virtual void LoadContent()
    {
    }

    /// <summary>
    /// Calls <see cref="FrameworkDispatcher.Update"/>, matching real XNA's own automatic
    /// per-frame dispatch -- which is what makes song-end detection and media-queue auto-advance
    /// actually run. Before Phase 8 WP7 this called <c>MediaPlayer.Update()</c> directly and was
    /// documented as a stand-in for the <c>FrameworkDispatcher</c> this project did not yet have;
    /// that type now exists, so this is the real thing rather than an approximation. A game
    /// overriding this method should call <c>base.Update(gameTime)</c>, standard XNA practice, to
    /// keep getting it for free.
    /// </summary>
    protected virtual void Update(GameTime gameTime)
    {
        FrameworkDispatcher.Update();
    }

    protected virtual void Draw(GameTime gameTime)
    {
    }

    protected virtual void UnloadContent()
    {
    }

    /// <summary>
    /// Fetches the graphics device handle for this game from native CNA. Returns a raw
    /// <see cref="nint"/>, not a <see cref="CnaHandle"/>, specifically so CNA.XnaCompat's
    /// <c>Game.CreateGraphicsDevice()</c> override can call it without ever naming a
    /// CNA.Interop type (see plan.md invariant #5).
    ///
    /// Real <c>cna_game_get_graphics_device</c> only succeeds from inside an active lifecycle
    /// callback, and the handle it returns is documented as valid only until that callback
    /// returns -- confirmed directly against <c>LifecycleSmoke.c</c>, not just inferred from the
    /// header doc comment: calling it outside a callback returns <c>CNA_RESULT_INVALID_STATE</c>,
    /// and a handle captured during one callback becomes <c>CNA_RESULT_INVALID_HANDLE</c> once a
    /// later, separate callback invocation begins. This method is still safe to call here, since
    /// <see cref="EnsureGraphicsDevice"/> only ever calls it from inside <see cref="OnInitialize"/>,
    /// itself a real, active lifecycle callback -- but the <see cref="GraphicsDevice"/> wrapper it
    /// builds from the result then caches that one-callback-scoped handle for the rest of the
    /// game's life, which the real ABI does not actually guarantee stays valid. That is a known,
    /// deliberate gap this step of the native-ABI migration does not yet close -- fixing it needs
    /// <see cref="GraphicsDevice"/>'s own methods to re-fetch a fresh device handle on every call
    /// instead, which touches that whole class's native surface together with the rest of its own
    /// signature corrections (see <c>NEXT.md</c>'s native-ABI-migration entry, step 3).
    /// </summary>
    protected nint GetNativeGraphicsDeviceHandle()
    {
        CnaResult result = Native.cna_game_get_graphics_device(_nativeHandle, out CnaHandle device);
        CnaException.ThrowIfFailed(result, "cna_game_get_graphics_device");
        return device.AsNint;
    }

    /// <summary>Same rationale as <see cref="GetNativeGraphicsDeviceHandle"/>'s first paragraph, for
    /// content -- but unlike the graphics device handle, the real content-manager handle is safe to
    /// resolve once and keep: <c>content.h</c>'s own doc comment says a borrowed manager "answers
    /// the same handle every time it is asked" with no per-callback validity caveat, confirmed by
    /// reading <c>RuntimeGameSmoke.c</c>'s <c>validate_content_manager</c> directly.</summary>
    protected nint GetNativeContentHandle()
    {
        CnaResult result = Native.cna_game_get_content_manager_ext(_nativeHandle, out CnaHandle contentManager);
        CnaException.ThrowIfFailed(result, "cna_game_get_content_manager_ext");
        return contentManager.AsNint;
    }

    /// <summary>
    /// Covariant-return factory hook used by CNA.XnaCompat. Passes the <em>game</em> handle, not the
    /// device handle.
    ///
    /// This read <c>new(GetNativeGraphicsDeviceHandle())</c> until the first integration test ran,
    /// and that was wrong in a way nothing managed could see: both handles are <see cref="nint"/>,
    /// so it compiled, and <see cref="GraphicsDevice"/>'s parameter is *named*
    /// <c>nativeGameHandleValue</c> because every one of its methods re-resolves a fresh device via
    /// <c>cna_game_get_graphics_device(gameHandle)</c>. Feeding it a device handle meant every
    /// single call became <c>cna_game_get_graphics_device(deviceHandle)</c> and answered
    /// <c>INVALID_HANDLE</c>. The entire graphics API -- Clear, SetData, SpriteBatch, all of it --
    /// could not work, and 701 managed tests said nothing.
    /// </summary>
    protected virtual GraphicsDevice CreateGraphicsDevice() => new(NativeHandle);

    /// <summary>Same rationale as <see cref="CreateGraphicsDevice"/>, for <see cref="Content"/>.</summary>
    protected virtual ContentManager CreateContentManager() => new(GetNativeContentHandle());

    private void EnsureGraphicsDevice()
    {
        if (_graphicsDeviceInitialized)
        {
            return;
        }

        GraphicsDevice = CreateGraphicsDevice();
        Content.GraphicsDevice = GraphicsDevice;
        _graphicsDeviceInitialized = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Deliberately does not check <c>cna_game_destroy</c>'s <see cref="CnaResult"/> the way every
    /// other native call in this file does -- <see cref="Dispose(bool)"/> must not throw (the usual
    /// .NET guideline, doubly true here since disposal can run during exception unwinding in a
    /// <see langword="using"/> block), so a destroy failure is deliberately swallowed rather than
    /// surfaced, matching this method's behavior before this migration (which ignored the native
    /// call's return value entirely, since the prior guessed shape had none to check).
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Before cna_game_destroy: the component ABI states "Every component must be released
        // before its game is destroyed", and a component cannot release itself -- its native
        // callback context is a strong GCHandle, so it is permanently reachable and unfinalizable.
        _components?.DisposeAllKnownComponents();

        // Same reason, different subsystem: a Microphone.BufferReady registration is taken against
        // this game, so one left alive would leave native holding a context pointer into a freed
        // GCHandle. Swallowed rather than surfaced, for the reason in this method's doc comment --
        // it must not throw.
        try
        {
            Audio.Microphone.ReleaseAllSubscriptions();
        }
        catch (Exception)
        {
            // A handler that threw and was never observed. Nothing can be reported from disposal.
        }

        // MediaPlayer's queue handle was taken against this game. Its two events are static and
        // deliberately survive -- see ReleaseGameScopedState.
        try
        {
            Media.MediaPlayer.ReleaseGameScopedState();
        }
        catch (Exception)
        {
        }

        // Same reason again: the window's event registrations name this game.
        try
        {
            _window?.ReleaseSubscriptions();
        }
        catch (Exception)
        {
        }

        // And this game's own four.
        for (int i = 0; i < _eventBridges.Length; i++)
        {
            _eventBridges[i]?.Dispose();
            _eventBridges[i] = null;
        }

        // Reconcile with native's ordering rule before destroying: a game cannot be destroyed
        // while a resource created against it is still alive.
        //
        // Several native-backed wrappers are deliberately never disposed by callers, because their
        // XNA counterparts are not IDisposable and no ported game would dispose them --
        // EffectTechnique, EffectPass and the collections behind them are the clearest case. They
        // are reclaimed by SafeHandle's critical finalizer, which is fine for memory and says
        // nothing about ordering: the GC has no idea native cares.
        //
        // So the finalizers are forced, and the destroy is retried, because one round is not
        // enough -- an object can become unreachable *during* a finalizer pass and need the next
        // one. Bounded at three rounds: if it still refuses, something is genuinely still alive and
        // looping would only hide it.
        //
        // Measured without this: `effect.CurrentTechnique.Passes[0].Apply()`, ordinary XNA written
        // once per frame, left live handles behind; cna_game_destroy answered INVALID_STATE; the
        // slot stayed occupied; and the next game failed to create with an error naming neither the
        // cause nor the culprit. Every integration test passed alone and up to nineteen failed
        // together, varying with test order.
        // Finalizers first, then a single destroy. Deliberately not retried around the destroy:
        // a second cna_game_destroy on a handle the first call may already have consumed is
        // undefined, and measuring it showed exactly that -- failures rose from six to nearly forty
        // and drifted between runs.
        if (disposing)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        NativeResourceHandle.DrainPendingReleasesForCurrentThread();

        CnaResult destroyResult = Native.cna_game_destroy(_nativeHandle);

        // Recorded rather than thrown -- disposal must not throw -- and read by the constructor,
        // which is where the information is finally actionable. Discarding it is what made the
        // failure baffling in the first place.
        if (destroyResult.IsFailure())
        {
            _undestroyedGame = _nativeHandle;
            _lastDestroyFailure =
                $"a previous game failed to release: cna_game_destroy answered {destroyResult}, which " +
                "native reports when a resource created against that game outlived it";
        }
        else
        {
            _undestroyedGame = CnaHandle.Zero;
            _lastDestroyFailure = null;
        }

        if (CnaAmbientGame.Current == _nativeHandle)
        {
            CnaAmbientGame.Current = CnaHandle.Zero;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private static bool TryResolve(nint context, out Game game)
    {
        if (context == 0)
        {
            game = null!;
            return false;
        }

        GCHandle handle = GCHandle.FromIntPtr(context);
        game = (Game)handle.Target!;
        return game is not null;
    }

    /// <summary>
    /// Encodes <paramref name="exception"/>'s message into <see cref="_callbackErrorBuffer"/> and
    /// points <paramref name="outError"/> at it, truncating if the message doesn't fit -- see this
    /// class's own doc comment for why that buffer must be pinned for the whole
    /// <see cref="Game"/> instance's lifetime rather than built fresh per call. Always returns
    /// <see cref="CnaResult.Callback"/>, so every wrapper below can simply
    /// <c>return ReportCallbackFailure(...)</c> from its <see langword="catch"/> block.
    /// </summary>
    private unsafe CnaResult ReportCallbackFailure(CnaCallbackError* outError, Exception exception)
    {
        if (outError is not null)
        {
            string message = exception.Message;
            int byteCount = Encoding.UTF8.GetByteCount(message);
            int written;
            if (byteCount <= _callbackErrorBuffer.Length)
            {
                written = Encoding.UTF8.GetBytes(message, _callbackErrorBuffer);
            }
            else
            {
                int charsToTry = Math.Min(message.Length, _callbackErrorBuffer.Length);
                while (charsToTry > 0 && Encoding.UTF8.GetByteCount(message.AsSpan(0, charsToTry)) > _callbackErrorBuffer.Length)
                {
                    charsToTry--;
                }

                written = Encoding.UTF8.GetBytes(message.AsSpan(0, charsToTry), _callbackErrorBuffer);
            }

            fixed (byte* bufferPtr = _callbackErrorBuffer)
            {
                outError->Message = new CnaStringView(written == 0 ? null : bufferPtr, (ulong)written);
            }
        }

        return CnaResult.Callback;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe CnaResult OnInitialize(CnaHandle nativeGame, CnaGameTime* gameTime, nint context, CnaCallbackError* outError)
    {
        if (!TryResolve(context, out Game game))
        {
            return CnaResult.Success;
        }

        try
        {
            game.RunInitializeOnce();
            return CnaResult.Success;
        }
        catch (Exception ex)
        {
            return game.ReportCallbackFailure(outError, ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe CnaResult OnLoadContent(CnaHandle nativeGame, CnaGameTime* gameTime, nint context, CnaCallbackError* outError)
    {
        if (!TryResolve(context, out Game game))
        {
            return CnaResult.Success;
        }

        try
        {
            game.LoadContent();
            return CnaResult.Success;
        }
        catch (Exception ex)
        {
            return game.ReportCallbackFailure(outError, ex);
        }
    }

    private bool _initialized;

    /// <summary>
    /// Runs <see cref="Initialize"/> exactly once.
    ///
    /// The once-guard is all that remains of a workaround. The first integration test found native
    /// delivering <c>LoadContent -> Initialize -> Update</c>: the C glue called the canonical
    /// <c>Game::Initialize()</c> -- which ends by calling <c>LoadContent()</c>, exactly as XNA does
    /// -- and only then invoked the <c>initialize</c> frame hook. That contradicted the header's
    /// own promise ("invoked once while the game initializes, before content loads"), so it was
    /// reported rather than adopted, and this method ran <see cref="Initialize"/> from whichever
    /// callback arrived first.
    ///
    /// Fixed upstream in CBIND-063 -- the hook now runs before the base, and the delivered order is
    /// <c>initialize, load_content, begin_run, update, draw</c>. The <c>LoadContent</c> side of the
    /// workaround is gone; the guard stays because "exactly once" is worth asserting on its own,
    /// and <c>Game_CallsInitializeOnce_BeforeLoadContent</c> now measures native's real order
    /// rather than this binding's correction of it.
    /// </summary>
    private void RunInitializeOnce()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        EnsureGraphicsDevice();
        Initialize();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe CnaResult OnUpdate(CnaHandle nativeGame, CnaGameTime* gameTime, nint context, CnaCallbackError* outError)
    {
        if (!TryResolve(context, out Game game) || gameTime is null)
        {
            return CnaResult.Success;
        }

        try
        {
            game.Update(GameTime.FromNative(*gameTime));
            return CnaResult.Success;
        }
        catch (Exception ex)
        {
            return game.ReportCallbackFailure(outError, ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe CnaResult OnDraw(CnaHandle nativeGame, CnaGameTime* gameTime, nint context, CnaCallbackError* outError)
    {
        if (!TryResolve(context, out Game game) || gameTime is null)
        {
            return CnaResult.Success;
        }

        try
        {
            game.Draw(GameTime.FromNative(*gameTime));
            return CnaResult.Success;
        }
        catch (Exception ex)
        {
            return game.ReportCallbackFailure(outError, ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe CnaResult OnUnloadContent(CnaHandle nativeGame, CnaGameTime* gameTime, nint context, CnaCallbackError* outError)
    {
        if (!TryResolve(context, out Game game))
        {
            return CnaResult.Success;
        }

        try
        {
            game.UnloadContent();
            return CnaResult.Success;
        }
        catch (Exception ex)
        {
            return game.ReportCallbackFailure(outError, ex);
        }
    }
}
