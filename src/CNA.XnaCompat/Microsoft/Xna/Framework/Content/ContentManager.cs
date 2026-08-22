namespace Microsoft.Xna.Framework.Content;

/// <summary>
/// XNA 4.0-compatible content-manager facade. Its public type system is independent of
/// <c>CNA.Content</c>; native loading is delegated through an internal backend instead.
/// </summary>
public class ContentManager : IDisposable
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
        CNA.Content.ContentManager,
        ContentManager> BackendFacades = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, object> _loadedAssets = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IDisposable> _disposableAssets = [];
    private CNA.Content.ContentManager? _backend;
    private readonly bool _ownsBackend;
    private string _rootDirectory;
    private bool _disposed;

    public ContentManager(IServiceProvider serviceProvider)
        : this(serviceProvider, string.Empty)
    {
    }

    public ContentManager(IServiceProvider serviceProvider, string rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(rootDirectory);

        _serviceProvider = serviceProvider;
        _rootDirectory = rootDirectory;
        _ownsBackend = true;
    }

    /// <summary>Wraps the borrowed manager owned by a <see cref="Game"/>. The facade never
    /// destroys this backend; the game remains its sole native owner.</summary>
    internal ContentManager(CNA.Content.ContentManager backend, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _backend = backend;
        _serviceProvider = serviceProvider;
        _rootDirectory = backend.RootDirectory;
        _ownsBackend = false;
        BackendFacades.Add(backend, this);
    }

    public string RootDirectory
    {
        get => _backend is null ? _rootDirectory : _backend.RootDirectory;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            ThrowIfDisposed();

            _rootDirectory = value;
            if (_backend is not null)
            {
                _backend.RootDirectory = value;
            }
        }
    }

    public IServiceProvider ServiceProvider => _serviceProvider;

    public virtual T Load<T>(string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            throw new ArgumentNullException(nameof(assetName));
        }

        ThrowIfDisposed();

        string key = assetName.Replace('\\', '/');
        if (_loadedAssets.TryGetValue(key, out object? cached) && cached is T typed)
        {
            return typed;
        }

        T result = ReadAsset<T>(assetName, RecordDisposableObject);
        _loadedAssets[key] = result!;
        return result;
    }

    public virtual void Unload()
    {
        ThrowIfDisposed();

        foreach (IDisposable disposable in _disposableAssets)
        {
            disposable.Dispose();
        }

        _disposableAssets.Clear();
        _loadedAssets.Clear();
        _backend?.Unload();
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

        if (disposing)
        {
            // Unload before setting the disposed flag because Unload deliberately validates it.
            Unload();
        }

        _disposed = true;

        if (_ownsBackend)
        {
            _backend?.Dispose();
        }

        _backend = null;
    }

    protected virtual Stream OpenStream(string assetName)
    {
        ArgumentNullException.ThrowIfNull(assetName);

        try
        {
            return TitleContainer.OpenStream(Path.Combine(RootDirectory, assetName) + ".xnb");
        }
        catch (Exception exception) when (
            exception is FileNotFoundException or DirectoryNotFoundException or IOException or UnauthorizedAccessException)
        {
            throw new ContentLoadException($"Could not open content asset '{assetName}'.", exception);
        }
    }

    protected T ReadAsset<T>(string assetName, Action<IDisposable>? recordDisposableObject)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            throw new ArgumentNullException(nameof(assetName));
        }

        ThrowIfDisposed();

        try
        {
            T result = LoadCore<T>(assetName);
            if (result is IDisposable disposable)
            {
                recordDisposableObject?.Invoke(disposable);
            }

            return result;
        }
        catch (ContentLoadException)
        {
            throw;
        }
        catch (CNA.Content.ContentLoadException exception)
        {
            throw new ContentLoadException(exception.Message, exception);
        }
    }

    private T LoadCore<T>(string assetName)
    {
        // XNA content is table-driven. The native CNA loaders remain the best route for the
        // native-backed built-ins below, but a user-defined T has no CNA type identity to dispatch
        // on. Read those XNB files here through the real public ContentReader/ContentTypeReader
        // contract instead of requiring a non-XNA LoadForeign<T>() escape hatch.
        if (!IsNativeBackedBuiltIn(typeof(T)))
        {
            using Stream stream = OpenStream(assetName);
            return ManagedXnbContentLoader.Load<T>(this, stream, assetName, RecordDisposableObject);
        }

        CNA.Content.ContentManager backend = GetBackend();

        if (typeof(T) == typeof(Graphics.Texture2D))
        {
            return (T)(object)new Graphics.Texture2D(
                RequireGraphicsDevice<T>(backend, assetName), backend.LoadNativeTexture2DHandle(assetName));
        }

        if (typeof(T) == typeof(Graphics.SpriteFont))
        {
            CNA.Content.ContentManager.SpriteFontData data = backend.LoadSpriteFontData(assetName);
            return (T)(object)new Graphics.SpriteFont(
                new Graphics.Texture2D(RequireGraphicsDevice<T>(backend, assetName), data.TextureHandle),
                Convert(data.GlyphBounds),
                Convert(data.Cropping),
                data.Characters,
                data.LineSpacing,
                data.Spacing,
                Convert(data.Kerning),
                data.DefaultCharacter);
        }

        if (typeof(T) == typeof(Graphics.TextureCube))
        {
            return (T)(object)new Graphics.TextureCube(
                RequireGraphicsDevice<T>(backend, assetName), backend.LoadNativeTextureCubeHandle(assetName));
        }

        if (typeof(T) == typeof(Audio.SoundEffect))
        {
            return (T)(object)new Audio.SoundEffect(backend.LoadNativeSoundEffectHandle(assetName));
        }

        if (typeof(T) == typeof(Graphics.Model))
        {
            return (T)(object)LoadCompatModel(backend, assetName);
        }

        if (typeof(T) == typeof(Graphics.Effect))
        {
            Graphics.GraphicsDevice device = RequireGraphicsDevice<T>(backend, assetName);
            return (T)(object)Graphics.Effect.Adopt(
                device, new CNA.Graphics.Effect(device.Framework, backend.LoadNativeEffectHandle(assetName)));
        }

        throw new ContentLoadException($"Unsupported built-in content type {typeof(T)}.");
    }

    private static bool IsNativeBackedBuiltIn(Type type) =>
        type == typeof(Graphics.Texture2D) ||
        type == typeof(Graphics.SpriteFont) ||
        type == typeof(Graphics.TextureCube) ||
        type == typeof(Audio.SoundEffect) ||
        type == typeof(Graphics.Model) ||
        type == typeof(Graphics.Effect);

    private CNA.Content.ContentManager GetBackend()
    {
        if (_backend is not null)
        {
            return _backend;
        }

        object? service = _serviceProvider.GetService(typeof(Graphics.IGraphicsDeviceService));
        if (service is not Graphics.IGraphicsDeviceService graphicsDeviceService ||
            graphicsDeviceService.GraphicsDevice is null)
        {
            throw new ContentLoadException(
                "No Microsoft.Xna.Framework.Graphics.IGraphicsDeviceService is available for content loading.");
        }

        _backend = CNA.Content.ContentManager.CreateOwned(graphicsDeviceService.GraphicsDevice.Framework, _rootDirectory);
        BackendFacades.Add(_backend, this);
        return _backend;
    }

    internal static ContentManager FromBackend(CNA.Content.ContentManager backend) =>
        BackendFacades.TryGetValue(backend, out ContentManager? facade)
            ? facade
            : throw new NotSupportedException(
                "The CNA content backend was not created through Microsoft.Xna.Framework.Content.ContentManager.");

    private static Graphics.GraphicsDevice RequireGraphicsDevice<T>(
        CNA.Content.ContentManager backend,
        string assetName) =>
        Graphics.GraphicsDevice.FromFramework(backend.GraphicsDevice) ?? throw new ContentLoadException(
            $"Cannot load {typeof(T).Name} '{assetName}': no compatible GraphicsDevice is available.");

    private Graphics.Model LoadCompatModel(CNA.Content.ContentManager backend, string assetName)
    {
        Graphics.GraphicsDevice graphicsDevice = RequireGraphicsDevice<Graphics.Model>(backend, assetName);

        if (File.Exists(Path.Combine(RootDirectory, assetName + ".xnb")))
        {
            return Graphics.XnbCompatModelBuilder.Build(graphicsDevice, backend.LoadXnbModelData(assetName));
        }

        if (File.Exists(Path.Combine(RootDirectory, assetName + ".cnj")))
        {
            return Graphics.CnjCompatModelBuilder.Build(graphicsDevice, backend.LoadCnjModelData(assetName));
        }

        throw new ContentLoadException(
            $"Content file '{assetName}' was not found (tried '{assetName}.xnb' and '{assetName}.cnj').");
    }

    private void RecordDisposableObject(IDisposable disposable)
    {
        if (!_disposableAssets.Contains(disposable))
        {
            _disposableAssets.Add(disposable);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static Rectangle[] Convert(IReadOnlyList<CNA.Rectangle> rectangles)
    {
        var result = new Rectangle[rectangles.Count];
        for (int i = 0; i < rectangles.Count; i++)
        {
            result[i] = rectangles[i];
        }

        return result;
    }

    private static Vector3[] Convert(IReadOnlyList<CNA.Vector3> vectors)
    {
        var result = new Vector3[vectors.Count];
        for (int i = 0; i < vectors.Count; i++)
        {
            result[i] = vectors[i];
        }

        return result;
    }
}
