namespace Microsoft.Xna.Framework;

/// <summary>
/// The system window associated with a <see cref="Game"/>.
///
/// CNA owns the platform-window lifetime, but that fact is intentionally private. XNA's public
/// contract is an abstract base class, so the native window is projected through the private
/// implementation created by <see cref="Game"/> instead of leaking <c>CNA.GameWindow</c> into a
/// game's base-type graph.
/// </summary>
public abstract class GameWindow
{
    private string _title = string.Empty;

    internal GameWindow()
    {
    }

    internal void Attach(CNA.GameWindow backend)
    {
        ArgumentNullException.ThrowIfNull(backend);
        _title = backend.Title;
    }

    public abstract bool AllowUserResizing { get; set; }

    public abstract Rectangle ClientBounds { get; }

    public abstract DisplayOrientation CurrentOrientation { get; }

    public abstract IntPtr Handle { get; }

    public abstract string ScreenDeviceName { get; }

    public string Title
    {
        get => _title;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_title == value)
            {
                return;
            }

            SetTitle(value);
            _title = value;
        }
    }

    public event EventHandler<EventArgs>? ClientSizeChanged;

    public event EventHandler<EventArgs>? OrientationChanged;

    public event EventHandler<EventArgs>? ScreenDeviceNameChanged;

    public abstract void BeginScreenDeviceChange(bool willBeFullScreen);

    public void EndScreenDeviceChange(string screenDeviceName) =>
        EndScreenDeviceChange(screenDeviceName, ClientBounds.Width, ClientBounds.Height);

    public abstract void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight);

    protected void OnActivated()
    {
    }

    protected void OnClientSizeChanged() => ClientSizeChanged?.Invoke(this, EventArgs.Empty);

    protected void OnDeactivated()
    {
    }

    protected void OnOrientationChanged() => OrientationChanged?.Invoke(this, EventArgs.Empty);

    protected void OnPaint()
    {
    }

    protected void OnScreenDeviceNameChanged() => ScreenDeviceNameChanged?.Invoke(this, EventArgs.Empty);

    protected internal abstract void SetSupportedOrientations(DisplayOrientation orientations);

    protected abstract void SetTitle(string title);
}
