using CNA.Interop;

namespace CNA.Storage;

/// <summary>
/// Matches real XNA's <c>StorageDevice</c>: a place saved games can live, selected by the player.
///
/// XNA's API here is <c>BeginShowSelector</c>/<c>EndShowSelector</c> and
/// <c>BeginOpenContainer</c>/<c>EndOpenContainer</c> -- an async shape inherited from the Xbox 360,
/// where showing a storage selector really did block on a system UI. CNA completes both
/// synchronously and says so explicitly ("no operation handle is invented for work that never
/// pends", <c>storage.h:125-127</c>).
///
/// Both shapes are offered rather than picking one: the <c>Begin</c>/<c>End</c> pairs exist so XNA
/// source compiles unchanged (returning an already-completed
/// <see cref="CompletedAsyncResult{T}"/>), and the plain synchronous methods exist because that is
/// what the operation actually is, and forcing new code through a fake async dance would be
/// theatre.
/// </summary>
public class StorageDevice
{
    private readonly NativeResourceHandle _handle;

    private StorageDevice(nint nativeHandleValue)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, h => Native.cna_storage_device_destroy(new CnaHandle(h)));
    }

    /// <summary>
    /// The native handle, read out of the owning <see cref="NativeResourceHandle"/>. Every caller
    /// pairs it with <see cref="GC.KeepAlive(object)"/> after the native call: once the handle
    /// value has been read this object can be unreachable, and an unreachable
    /// <see cref="System.Runtime.InteropServices.SafeHandle"/> may have its critical finalizer run
    /// <c>destroy</c> while the call is still in flight. Defeating exactly that is what
    /// <see cref="System.Runtime.InteropServices.SafeHandle"/> is for, so reading the handle
    /// without keeping its owner alive gives the guarantee up -- see <c>plan.md</c> WP17.
    /// </summary>
    private CnaHandle NativeHandle => new(_handle.DangerousGetHandle());

    /// <summary>
    /// Raised when the set of storage devices changes. Matches real XNA's static
    /// <c>DeviceChanged</c>.
    ///
    /// Process-wide, like the media-player events: <c>cna_storage_device_subscribe_device_changed</c>
    /// takes no device and no game handle, so the subscription belongs to the process and is held
    /// for it. Taken on the first <c>+=</c>.
    /// </summary>
    public static event EventHandler<EventArgs>? DeviceChanged
    {
        add
        {
            lock (DeviceChangedLock)
            {
                _deviceChangedBridge ??= NativeEventBridge.Subscribe(
                    () => _deviceChanged?.Invoke(null, EventArgs.Empty),
                    (callback, context) =>
                    {
                        CnaResult result = Native.cna_storage_device_subscribe_device_changed(
                            callback, context, out CnaHandle registration);
                        CnaException.ThrowIfFailed(result, nameof(DeviceChanged));
                        return registration;
                    },
                    registration => Native.cna_storage_device_unsubscribe_device_changed(registration));

                _deviceChanged += value;
            }
        }
        remove
        {
            lock (DeviceChangedLock)
            {
                _deviceChanged -= value;
            }
        }
    }

    private static readonly object DeviceChangedLock = new();
    private static NativeEventBridge? _deviceChangedBridge;
    private static EventHandler<EventArgs>? _deviceChanged;

    public bool IsConnected
    {
        get
        {
            CnaResult result = Native.cna_storage_device_get_is_connected(NativeHandle, out byte value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(IsConnected));
            return value != 0;
        }
    }

    public long FreeSpace
    {
        get
        {
            CnaResult result = Native.cna_storage_device_get_free_space(NativeHandle, out long value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(FreeSpace));
            return value;
        }
    }

    public long TotalSpace
    {
        get
        {
            CnaResult result = Native.cna_storage_device_get_total_space(NativeHandle, out long value);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(TotalSpace));
            return value;
        }
    }

    /// <summary>Shows the storage selector and returns the chosen device. The synchronous form --
    /// see this class's own doc comment for why both exist.</summary>
    public static StorageDevice ShowSelector()
    {
        CnaResult result = Native.cna_storage_device_show_selector(0, 0, out CnaHandle device);
        CnaException.ThrowIfFailed(result, nameof(ShowSelector));
        return new StorageDevice(device.AsNint);
    }

    /// <summary>Shows the selector for one player. Real XNA's per-player overload -- the ABI takes
    /// a <c>CNA_PlayerIndex</c>, which is what makes this a distinct route rather than an argument
    /// this layer could drop.</summary>
    public static StorageDevice ShowSelector(PlayerIndex player)
    {
        CnaResult result = Native.cna_storage_device_show_selector_for_player(
            (uint)player, 0, 0, out CnaHandle device);
        CnaException.ThrowIfFailed(result, nameof(ShowSelector));
        return new StorageDevice(device.AsNint);
    }

    /// <summary>Shows the selector, requiring the device to have room for
    /// <paramref name="sizeInBytes"/> across <paramref name="directoryCount"/> directories.</summary>
    public static StorageDevice ShowSelector(int sizeInBytes, int directoryCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeInBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(directoryCount);

        CnaResult result = Native.cna_storage_device_show_selector_with_space(
            sizeInBytes, directoryCount, 0, 0, out CnaHandle device);
        CnaException.ThrowIfFailed(result, nameof(ShowSelector));
        return new StorageDevice(device.AsNint);
    }

    /// <summary>Both of the above at once.</summary>
    public static StorageDevice ShowSelector(PlayerIndex player, int sizeInBytes, int directoryCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeInBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(directoryCount);

        CnaResult result = Native.cna_storage_device_show_selector_for_player_with_space(
            (uint)player, sizeInBytes, directoryCount, 0, 0, out CnaHandle device);
        CnaException.ThrowIfFailed(result, nameof(ShowSelector));
        return new StorageDevice(device.AsNint);
    }

    /// <summary>Matches real XNA's <c>BeginShowSelector(PlayerIndex, AsyncCallback, object)</c>.
    /// Completes before returning, like every other selector route here.</summary>
    public static IAsyncResult BeginShowSelector(PlayerIndex player, AsyncCallback? callback, object? state)
    {
        var asyncResult = new CompletedAsyncResult<StorageDevice>(ShowSelector(player), state);
        callback?.Invoke(asyncResult);
        return asyncResult;
    }

    /// <summary>Matches real XNA's <c>BeginShowSelector(int, int, AsyncCallback, object)</c>.</summary>
    public static IAsyncResult BeginShowSelector(
        int sizeInBytes, int directoryCount, AsyncCallback? callback, object? state)
    {
        var asyncResult = new CompletedAsyncResult<StorageDevice>(
            ShowSelector(sizeInBytes, directoryCount), state);
        callback?.Invoke(asyncResult);
        return asyncResult;
    }

    /// <summary>Matches real XNA's four-argument <c>BeginShowSelector</c>.</summary>
    public static IAsyncResult BeginShowSelector(
        PlayerIndex player, int sizeInBytes, int directoryCount, AsyncCallback? callback, object? state)
    {
        var asyncResult = new CompletedAsyncResult<StorageDevice>(
            ShowSelector(player, sizeInBytes, directoryCount), state);
        callback?.Invoke(asyncResult);
        return asyncResult;
    }

    /// <summary>Matches real XNA's <c>BeginShowSelector</c>. Completes before returning; the
    /// callback (when supplied) is invoked synchronously, and
    /// <see cref="IAsyncResult.CompletedSynchronously"/> is <see langword="true"/> so a caller can
    /// tell.</summary>
    public static IAsyncResult BeginShowSelector(AsyncCallback? callback, object? state)
    {
        var asyncResult = new CompletedAsyncResult<StorageDevice>(ShowSelector(), state);
        callback?.Invoke(asyncResult);
        return asyncResult;
    }

    public static StorageDevice EndShowSelector(IAsyncResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result is not CompletedAsyncResult<StorageDevice> completed)
        {
            throw new ArgumentException(
                "The IAsyncResult did not come from StorageDevice.BeginShowSelector.", nameof(result));
        }

        return completed.Result;
    }

    public StorageContainer OpenContainer(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);

        CnaHandle container = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            displayName, view => Native.cna_storage_container_open(NativeHandle, view, 0, 0, out container));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(OpenContainer));
        return new StorageContainer(container.AsNint, this);
    }

    public IAsyncResult BeginOpenContainer(string displayName, AsyncCallback? callback, object? state)
    {
        var asyncResult = new CompletedAsyncResult<StorageContainer>(OpenContainer(displayName), state);
        callback?.Invoke(asyncResult);
        return asyncResult;
    }

    public StorageContainer EndOpenContainer(IAsyncResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result is not CompletedAsyncResult<StorageContainer> completed)
        {
            throw new ArgumentException(
                "The IAsyncResult did not come from StorageDevice.BeginOpenContainer.", nameof(result));
        }

        return completed.Result;
    }

    public void DeleteContainer(string titleName)
    {
        ArgumentNullException.ThrowIfNull(titleName);

        CnaResult result = CnaStringMarshal.WithStringView(
            titleName, view => Native.cna_storage_device_delete_container(NativeHandle, view));
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(DeleteContainer));
    }
}
