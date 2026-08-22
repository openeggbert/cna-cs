namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible common base for all texture resources.</summary>
public abstract class Texture : GraphicsResource
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CNA.Graphics.Texture, Texture>
        FrameworkFacades = new();

    private readonly CNA.Graphics.Texture _frameworkTexture;

    internal Texture(GraphicsDevice graphicsDevice, CNA.Graphics.Texture frameworkTexture)
        : base(graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(frameworkTexture);
        _frameworkTexture = frameworkTexture;
        FrameworkFacades.Add(frameworkTexture, this);
    }

    internal CNA.Graphics.Texture FrameworkTexture => _frameworkTexture;

    internal static Texture? FromFramework(CNA.Graphics.Texture? frameworkTexture)
    {
        if (frameworkTexture is null)
        {
            return null;
        }

        return FrameworkFacades.TryGetValue(frameworkTexture, out Texture? facade)
            ? facade
            : null;
    }

    internal nint NativeHandleValue => _frameworkTexture.NativeHandleValue;

    internal nint DetachNativeHandle() => _frameworkTexture.DetachNativeHandle();

    public int LevelCount => _frameworkTexture.LevelCount;

    public SurfaceFormat Format => (SurfaceFormat)(int)_frameworkTexture.Format;

    internal void DisposeFrameworkTexture()
    {
        _frameworkTexture.Dispose();
    }
}
