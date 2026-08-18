namespace Microsoft.Xna.Framework.Storage;

/// <summary>XNA 4.0-compatible <c>StorageContainer</c>. A thin re-typing facade for the same
/// reason <see cref="StorageDevice"/> is one. Every file and directory member forwards unchanged --
/// they deal in <see cref="string"/>, <see cref="bool"/> and <see cref="System.IO.Stream"/>, none
/// of which differ per namespace.</summary>
public class StorageContainer : IDisposable
{
    private readonly CNA.Storage.StorageContainer _container;

    internal StorageContainer(CNA.Storage.StorageContainer container, StorageDevice storageDevice)
    {
        _container = container;
        StorageDevice = storageDevice;
    }

    public StorageDevice StorageDevice { get; }

    public string DisplayName => _container.DisplayName;

    public bool IsDisposed => _container.IsDisposed;

    public void CreateDirectory(string directory) => _container.CreateDirectory(directory);

    public void DeleteDirectory(string directory) => _container.DeleteDirectory(directory);

    public bool DirectoryExists(string directory) => _container.DirectoryExists(directory);

    public void DeleteFile(string file) => _container.DeleteFile(file);

    public bool FileExists(string file) => _container.FileExists(file);

    public string[] GetFileNames() => _container.GetFileNames();

    public string[] GetFileNames(string searchPattern) => _container.GetFileNames(searchPattern);

    public string[] GetDirectoryNames() => _container.GetDirectoryNames();

    public string[] GetDirectoryNames(string searchPattern) => _container.GetDirectoryNames(searchPattern);

    public Stream CreateFile(string file) => _container.CreateFile(file);

    public Stream OpenFile(string file, FileMode fileMode) => _container.OpenFile(file, fileMode);

    public void Dispose()
    {
        _container.Dispose();
        GC.SuppressFinalize(this);
    }
}
