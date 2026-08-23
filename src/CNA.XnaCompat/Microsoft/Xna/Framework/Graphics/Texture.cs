namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible common base for all texture resources.</summary>
public abstract class Texture : GraphicsResource
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CNA.Graphics.Texture, Texture>
        FrameworkFacades = new();

    private readonly CNA.Graphics.Texture _frameworkTexture;
    private readonly int _levelCount;
    private readonly SurfaceFormat _format;

    internal Texture(GraphicsDevice graphicsDevice, CNA.Graphics.Texture frameworkTexture)
        : base(graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(frameworkTexture);
        _frameworkTexture = frameworkTexture;
        _levelCount = frameworkTexture.LevelCount;
        _format = (SurfaceFormat)(int)frameworkTexture.Format;
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

    public int LevelCount => _levelCount;

    public SurfaceFormat Format => _format;

    internal void DisposeFrameworkTexture()
    {
        // A throwing derived constructor still leaves a finalizable GraphicsResource instance.
        // Its backend field has not necessarily been assigned yet.
        _frameworkTexture?.Dispose();
    }
}
