using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CNA.Content;
using CNA.Graphics;
using CNA.Interop;
using CNA.Media;

namespace CNA;

/// <summary>
/// Base class for a CNA game. Native CNA owns window creation, platform event pumping, timing,
/// and the frame lifecycle; it calls back into this class at coarse per-frame boundaries through
/// <see cref="CnaManagedGameCallbacks"/>. See the "Game inheritance requires a callback bridge"
/// design in ../../cnabinding/analysis_binding.md §20-§21.
/// </summary>
public abstract class Game : IDisposable
{
    private readonly CnaHandle _nativeHandle;
    private GCHandle _selfHandle;
    private bool _graphicsDeviceInitialized;
    private bool _disposed;

    protected Game()
    {
        _selfHandle = GCHandle.Alloc(this);

        CnaResult result;
        unsafe
        {
            var callbacks = new CnaManagedGameCallbacks
            {
                Initialize = &OnInitialize,
                LoadContent = &OnLoadContent,
                Update = &OnUpdate,
                Draw = &OnDraw,
                UnloadContent = &OnUnloadContent,
            };

            result = Native.cna_managed_game_create(in callbacks, GCHandle.ToIntPtr(_selfHandle), out _nativeHandle);
        }

        if (result.IsFailure())
        {
            _selfHandle.Free();
            CnaException.ThrowIfFailed(result, "cna_managed_game_create");
        }

        Content = CreateContentManager();
    }

    /// <summary>
    /// The raw native game handle value, for the small set of CNA types (currently none
    /// outside this file) that need it. Deliberately typed as <see cref="nint"/>, not
    /// <see cref="CnaHandle"/>, so it can appear in <c>protected</c> members that CNA.XnaCompat's
    /// subclass overrides touch -- see <see cref="GetNativeGraphicsDeviceHandle"/> and
    /// <see cref="GetNativeContentHandle"/> below, and docs/architecture.md.
    /// </summary>
    internal nint NativeHandle => _nativeHandle.Value;

    public ContentManager Content { get; }

    public GraphicsDevice GraphicsDevice { get; protected set; } = null!;

    /// <summary>Hands control to native CNA. Blocks until the game exits.</summary>
    public void Run()
    {
        CnaResult result = Native.cna_managed_game_run(_nativeHandle);
        CnaException.ThrowIfFailed(result, "cna_managed_game_run");
    }

    public void Exit() => Native.cna_managed_game_exit(_nativeHandle);

    protected virtual void Initialize()
    {
    }

    protected virtual void LoadContent()
    {
    }

    /// <summary>
    /// Calls <see cref="MediaPlayer.Update"/> so song-end detection/queue auto-advance actually
    /// runs somewhere -- the closest equivalent this project can offer to real XNA's own automatic
    /// per-frame <c>FrameworkDispatcher.Update()</c> call, which this project doesn't implement
    /// (see <see cref="MediaPlayer"/>'s own doc comment). A game overriding this method should
    /// call <c>base.Update(gameTime)</c>, standard XNA practice, to keep getting this for free.
    /// </summary>
    protected virtual void Update(GameTime gameTime)
    {
        MediaPlayer.Update();
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
    /// </summary>
    protected nint GetNativeGraphicsDeviceHandle() => Native.cna_game_get_graphics_device(_nativeHandle).Value;

    /// <summary>Same rationale as <see cref="GetNativeGraphicsDeviceHandle"/>, for content.</summary>
    protected nint GetNativeContentHandle() => Native.cna_game_get_content(_nativeHandle).Value;

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

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Native.cna_managed_game_release(_nativeHandle);

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

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnInitialize(nint context)
    {
        if (TryResolve(context, out Game game))
        {
            game.EnsureGraphicsDevice();
            game.Initialize();
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnLoadContent(nint context)
    {
        if (TryResolve(context, out Game game))
        {
            game.LoadContent();
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnUpdate(nint context, CnaGameTime nativeGameTime)
    {
        if (TryResolve(context, out Game game))
        {
            game.Update(GameTime.FromNative(nativeGameTime));
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnDraw(nint context, CnaGameTime nativeGameTime)
    {
        if (TryResolve(context, out Game game))
        {
            game.Draw(GameTime.FromNative(nativeGameTime));
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnUnloadContent(nint context)
    {
        if (TryResolve(context, out Game game))
        {
            game.UnloadContent();
        }
    }
}
