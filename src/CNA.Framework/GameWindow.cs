using System.Text;
using CNA.Interop;

namespace CNA;

/// <summary>
/// Matches real XNA's <c>GameWindow</c>.
///
/// It had exactly one member -- <see cref="Title"/> -- until a sweep of unbound header functions
/// showed the rest of <c>runtime_window.h</c> sitting there. Nothing had been decided about the
/// others; they were simply never reached.
///
/// The one genuine ABI quirk is that there is no <c>cna_game_window_set_title</c>: the only setter
/// is <c>cna_game_set_window_title</c> (<c>runtime.h:246</c>), a plain owned-handle call safe to run
/// any time, which is why this needs no lifecycle-callback dance the way
/// <c>GraphicsDevice</c>'s handle resolution does.
/// </summary>
public class GameWindow
{
    private readonly nint _nativeGameHandleValue;

    internal GameWindow(nint nativeGameHandleValue)
    {
        _nativeGameHandleValue = nativeGameHandleValue;
    }

    public string Title
    {
        get => QueryTitle();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = CnaStringMarshal.WithStringView(
                value, view => Native.cna_game_set_window_title(new CnaHandle(_nativeGameHandleValue), view));
            CnaException.ThrowIfFailed(result, nameof(Title));
        }
    }

    /// <summary>
    /// Real XNA's <c>Handle</c>: the platform's own round-trip window token, typed as
    /// <see cref="nint"/> the way XNA types it as <c>IntPtr</c>, over an ABI that carries it as a
    /// <see cref="ulong"/>.
    ///
    /// <b>This is not the native window, despite the route's name.</b> The header used to claim
    /// this and the canonical native-window accessor answered the same pointer; they do not, and
    /// upstream measured the difference under X11 -- the native accessor gives a real
    /// <c>Display*</c> and XID, this gives a platform window pointer widened to an integer. The
    /// platform layer that mints it says new interop code should not use it; its one documented use
    /// is the round trip, handing it back through <c>PresentationParameters.DeviceWindowHandle</c>
    /// to adopt an existing window.
    ///
    /// Zero means <em>this renderer never creates a window</em> -- headless, software and stub
    /// renderers all report it -- rather than that the call failed.
    /// </summary>
    public nint Handle
    {
        get
        {
            CnaResult result = Native.cna_game_window_get_native_handle_ext(NativeGame, out ulong handle);
            CnaException.ThrowIfFailed(result, nameof(Handle));
            return unchecked((nint)handle);
        }
    }

    /// <summary>Whether the user may resize the window by dragging its border. The setter can fail
    /// with a platform error on a backend that cannot honour it -- surfaced rather than
    /// swallowed, because silently ignoring it would leave a game believing it had a fixed-size
    /// window.</summary>
    public bool AllowUserResizing
    {
        get
        {
            CnaResult result = Native.cna_game_window_get_allow_user_resizing(NativeGame, out byte allowed);
            CnaException.ThrowIfFailed(result, nameof(AllowUserResizing));
            return allowed != 0;
        }
        set
        {
            CnaResult result = Native.cna_game_window_set_allow_user_resizing(
                NativeGame, value ? (byte)1 : (byte)0);
            CnaException.ThrowIfFailed(result, nameof(AllowUserResizing));
        }
    }

    /// <summary>The drawable client area, excluding any border or title bar.</summary>
    public Rectangle ClientBounds
    {
        get
        {
            CnaResult result = Native.cna_game_window_get_client_bounds(NativeGame, out CnaRect bounds);
            CnaException.ThrowIfFailed(result, nameof(ClientBounds));
            return new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
    }

    public DisplayOrientation CurrentOrientation
    {
        get
        {
            CnaResult result = Native.cna_game_window_get_current_orientation(NativeGame, out uint orientation);
            CnaException.ThrowIfFailed(result, nameof(CurrentOrientation));
            return (DisplayOrientation)orientation;
        }
    }

    /// <summary>The display the window is on. What
    /// <see cref="EndScreenDeviceChange(string,int,int)"/> takes to move it.</summary>
    public unsafe string ScreenDeviceName => NativeStringReader.Read(
        Native.cna_game_window_get_screen_device_name_size,
        Native.cna_game_window_copy_screen_device_name,
        NativeGame,
        nameof(ScreenDeviceName));

    /// <summary>Begins a move between displays or in and out of full screen. Must be paired with
    /// <see cref="EndScreenDeviceChange(string,int,int)"/>; the pair is how XNA lets a game redraw
    /// once rather than once per intermediate state.</summary>
    public void BeginScreenDeviceChange(bool willBeFullScreen)
    {
        CnaResult result = Native.cna_game_window_begin_screen_device_change(
            NativeGame, willBeFullScreen ? (byte)1 : (byte)0);
        CnaException.ThrowIfFailed(result, nameof(BeginScreenDeviceChange));
    }

    /// <summary>Ends the change, keeping the current client size. Matches real XNA's one-argument
    /// overload.</summary>
    public void EndScreenDeviceChange(string screenDeviceName) =>
        EndScreenDeviceChange(screenDeviceName, 0, 0);

    /// <summary>Ends the change and resizes. A non-positive width or height means "keep the current
    /// one", which is what the one-argument overload passes.</summary>
    public void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight)
    {
        ArgumentNullException.ThrowIfNull(screenDeviceName);

        CnaResult result = CnaStringMarshal.WithStringView(
            screenDeviceName,
            view => Native.cna_game_window_end_screen_device_change(NativeGame, view, clientWidth, clientHeight));
        CnaException.ThrowIfFailed(result, nameof(EndScreenDeviceChange));
    }

    /// <summary>Raised after the client area changes size. See
    /// <see cref="GraphicsDeviceManager.DeviceCreated"/> for why the native subscription is taken on
    /// the first <c>+=</c> and held until this window's game goes away.</summary>
    public event EventHandler<EventArgs>? ClientSizeChanged
    {
        add { EnsureSubscribed(CnaGameWindowEvent.ClientSizeChanged); _clientSizeChanged += value; }
        remove => _clientSizeChanged -= value;
    }

    public event EventHandler<EventArgs>? OrientationChanged
    {
        add { EnsureSubscribed(CnaGameWindowEvent.OrientationChanged); _orientationChanged += value; }
        remove => _orientationChanged -= value;
    }

    public event EventHandler<EventArgs>? ScreenDeviceNameChanged
    {
        add { EnsureSubscribed(CnaGameWindowEvent.ScreenDeviceNameChanged); _screenDeviceNameChanged += value; }
        remove => _screenDeviceNameChanged -= value;
    }

    private EventHandler<EventArgs>? _clientSizeChanged;
    private EventHandler<EventArgs>? _orientationChanged;
    private EventHandler<EventArgs>? _screenDeviceNameChanged;

    private readonly NativeEventBridge?[] _eventBridges =
        new NativeEventBridge?[(int)CnaGameWindowEvent.ScreenDeviceNameChanged + 1];

    private CnaHandle NativeGame => new(_nativeGameHandleValue);

    /// <summary>Subscribes to <paramref name="which"/> exactly once, indexed by the native event
    /// identity so the array position and the value handed to native cannot drift apart -- the same
    /// shape <see cref="GraphicsDeviceManager"/> uses.</summary>
    private void EnsureSubscribed(CnaGameWindowEvent which)
    {
        int index = (int)which;
        if (_eventBridges[index] is not null)
        {
            return;
        }

        _eventBridges[index] = NativeEventBridge.Subscribe(
            () => Raise(which),
            (callback, context) =>
            {
                CnaResult result = Native.cna_game_window_subscribe(
                    NativeGame, (uint)which, callback, context, out CnaHandle registration);
                CnaException.ThrowIfFailed(result, nameof(EnsureSubscribed));
                return registration;
            },
            registration => Native.cna_game_unsubscribe(registration));
    }

    private void Raise(CnaGameWindowEvent which)
    {
        EventHandler<EventArgs>? handler = which switch
        {
            CnaGameWindowEvent.ClientSizeChanged => _clientSizeChanged,
            CnaGameWindowEvent.OrientationChanged => _orientationChanged,
            CnaGameWindowEvent.ScreenDeviceNameChanged => _screenDeviceNameChanged,
            _ => null,
        };

        handler?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Releases every window subscription. Called from <see cref="Game"/>'s disposal: these
    /// registrations are taken against that game, and one outliving it would leave native calling
    /// into a freed context. Rethrows the first handler failure that was never surfaced.</summary>
    internal void ReleaseSubscriptions()
    {
        Exception? pending = null;
        for (int i = 0; i < _eventBridges.Length; i++)
        {
            NativeEventBridge? bridge = _eventBridges[i];
            _eventBridges[i] = null;
            if (bridge is null)
            {
                continue;
            }

            try
            {
                bridge.ThrowPendingException();
            }
            catch (Exception ex)
            {
                pending ??= ex;
            }

            bridge.Dispose();
        }

        if (pending is not null)
        {
            throw pending;
        }
    }

    private unsafe string QueryTitle()
    {
        CnaHandle game = new(_nativeGameHandleValue);
        CnaResult sizeResult = Native.cna_game_window_get_title_size(game, out ulong length);
        if (sizeResult.IsFailure() || length == 0)
        {
            return string.Empty;
        }

        byte[] buffer = new byte[length];
        fixed (byte* bufferPtr = buffer)
        {
            CnaResult copyResult = Native.cna_game_window_copy_title(game, bufferPtr, length, out ulong written);
            if (copyResult.IsFailure())
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(buffer, 0, (int)written);
        }
    }
}
