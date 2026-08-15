namespace Microsoft.Xna.Framework.Content;

/// <summary>
/// XNA 4.0-compatible <c>ContentManager</c>. Overrides <c>Load&lt;T&gt;</c> to recognize compat
/// content types (currently <see cref="Graphics.Texture2D"/>), reusing the base class's protected
/// native-load helper so this project never references CNA.Interop directly. See
/// ../../../../../docs/architecture.md and ../../../../../docs/xna-compatibility.md.
/// </summary>
public class ContentManager : CNA.Framework.Content.ContentManager
{
    protected internal ContentManager(nint nativeHandleValue)
        : base(nativeHandleValue)
    {
    }

    public override T Load<T>(string assetName)
    {
        if (typeof(T) == typeof(Graphics.Texture2D))
        {
            return (T)(object)new Graphics.Texture2D(LoadNativeTexture2DHandle(assetName));
        }

        throw new NotSupportedException($"Unsupported content type {typeof(T)}.");
    }
}
