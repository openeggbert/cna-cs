using CNA.Framework.Graphics;
using CNA.Interop;

namespace CNA.Framework.Content;

/// <summary>
/// The native C ABI cannot expose C# generics directly, so <see cref="Load{T}"/> dispatches by
/// runtime type -- see ../../cnabinding/analysis_binding.md §26. CNA.XnaCompat's
/// <c>ContentManager</c> overrides this same method to additionally recognize its own compat
/// content types, reusing <see cref="LoadNativeTexture2DHandle"/> so it never has to touch
/// CNA.Interop directly (see docs/architecture.md).
/// </summary>
public class ContentManager
{
    private readonly nint _nativeHandleValue;
    private string _rootDirectory = string.Empty;

    /// <summary>
    /// <c>protected internal</c> so CNA.XnaCompat's <c>ContentManager</c> subclass constructor
    /// can forward to it without naming <see cref="CnaHandle"/> -- see docs/architecture.md.
    /// </summary>
    protected internal ContentManager(nint nativeHandleValue)
    {
        _nativeHandleValue = nativeHandleValue;
    }

    public string RootDirectory
    {
        get => _rootDirectory;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = Native.cna_content_set_root_directory(new CnaHandle(_nativeHandleValue), value);
            CnaException.ThrowIfFailed(result, nameof(RootDirectory));
            _rootDirectory = value;
        }
    }

    public virtual T Load<T>(string assetName)
    {
        if (typeof(T) == typeof(Texture2D))
        {
            return (T)(object)new Texture2D(LoadNativeTexture2DHandle(assetName));
        }

        throw new NotSupportedException($"Unsupported content type {typeof(T)}.");
    }

    protected nint LoadNativeTexture2DHandle(string assetName)
    {
        CnaResult result = Native.cna_content_load_texture2d(new CnaHandle(_nativeHandleValue), assetName, out CnaHandle texture);
        CnaException.ThrowIfFailed(result, nameof(Load));
        return texture.Value;
    }
}
