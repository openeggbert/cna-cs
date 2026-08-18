namespace Microsoft.Xna.Framework.Storage;

/// <summary>XNA 4.0-compatible <c>StorageDevice</c>. A thin re-typing facade over
/// <see cref="CNA.Storage.StorageDevice"/>, which cannot be subclassed here because its only
/// constructor is private (instances come from the selector) -- so the static entry points forward
/// and the instance ones wrap. See the base type's doc comment for why both the
/// <c>Begin</c>/<c>End</c> pairs and the synchronous methods exist.</summary>
public class StorageDevice
{
    private readonly CNA.Storage.StorageDevice _device;

    internal StorageDevice(CNA.Storage.StorageDevice device)
    {
        _device = device;
    }

    public bool IsConnected => _device.IsConnected;

    public long FreeSpace => _device.FreeSpace;

    public long TotalSpace => _device.TotalSpace;

    public static StorageDevice ShowSelector() => new(CNA.Storage.StorageDevice.ShowSelector());

    public static IAsyncResult BeginShowSelector(AsyncCallback? callback, object? state) =>
        CNA.Storage.StorageDevice.BeginShowSelector(callback, state);

    public static StorageDevice EndShowSelector(IAsyncResult result) =>
        new(CNA.Storage.StorageDevice.EndShowSelector(result));

    public StorageContainer OpenContainer(string displayName) => new(_device.OpenContainer(displayName), this);

    public IAsyncResult BeginOpenContainer(string displayName, AsyncCallback? callback, object? state) =>
        _device.BeginOpenContainer(displayName, callback, state);

    public StorageContainer EndOpenContainer(IAsyncResult result) => new(_device.EndOpenContainer(result), this);

    public void DeleteContainer(string titleName) => _device.DeleteContainer(titleName);
}
