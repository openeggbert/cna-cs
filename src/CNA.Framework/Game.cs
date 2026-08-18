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
public abstract class Game : IDisposable
{
    /// <summary>Matches real XNA's own default <c>TargetElapsedTime</c> (1/60 second, expressed in
    /// 100ns ticks) -- also the exact value the real C API's own lifecycle smoke test uses for the
    /// same field, confirmed by reading it directly.</summary>
    private const long DefaultTargetElapsedTimeTicks = 166_667;

    private const int CallbackErrorBufferSize = 512;

    private readonly CnaHandle _nativeHandle;
    private readonly byte[] _callbackErrorBuffer = GC.AllocateArray<byte>(CallbackErrorBufferSize, pinned: true);
    private GCHandle _selfHandle;
    private bool _graphicsDeviceInitialized;
    private bool _disposed;

    protected unsafe Game()
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

        CnaResult result = Native.cna_game_create(in createInfo, out _nativeHandle);
        if (result.IsFailure())
        {
            _selfHandle.Free();
            CnaException.ThrowIfFailed(result, "cna_game_create");
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

    /// <summary>Hands control to native CNA. Blocks until the game exits.</summary>
    public void Run()
    {
        CnaResult result = Native.cna_game_run(_nativeHandle);
        CnaException.ThrowIfFailed(result, "cna_game_run");
    }

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
    /// Covariant-return factory hook: CNA.XnaCompat's <c>Game</c> overrides this to return a
    /// <c>Microsoft.Xna.Framework.Graphics.GraphicsDevice</c> instead, so <see cref="GraphicsDevice"/>
    /// holds the compat-typed instance without CNA needing to know CNA.XnaCompat exists.
    /// </summary>
    protected virtual GraphicsDevice CreateGraphicsDevice() => new(GetNativeGraphicsDeviceHandle());

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
        Native.cna_game_destroy(_nativeHandle);

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
            game.EnsureGraphicsDevice();
            game.Initialize();
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
