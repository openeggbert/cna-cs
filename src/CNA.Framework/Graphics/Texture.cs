using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>Texture</c> abstract base: the ancestor of every texture kind
/// (<see cref="Texture2D"/>, and -- once WP3b lands them -- <c>Texture3D</c>/<c>TextureCube</c>),
/// carrying the two properties they all share plus ownership of the native handle itself.
///
/// Introduced by Phase 8 WP3 (see <c>plan.md</c>). Two things moved *up* into this class from
/// <see cref="Texture2D"/>, where they used to live:
///
/// <list type="bullet">
/// <item>The <c>NativeResourceHandle</c> field and the <see cref="ReleaseNative"/> hook, so every
/// texture kind shares one disposal path rather than each re-implementing it.</item>
/// <item>Disposal itself, which now runs through <see cref="GraphicsResource.Dispose(bool)"/> --
/// so <see cref="GraphicsResource.IsDisposed"/> and the <see cref="GraphicsResource.Disposing"/>
/// event are real for textures instead of being inherited-but-inert.</item>
/// </list>
///
/// <see cref="LevelCount"/>/<see cref="Format"/> are backed by <c>cna_texture_get_info</c>, which
/// the C API documents as accepting "Texture2D, Texture3D, TextureCube or matching render-target
/// handle" (<c>texture.h:126</c>) -- one accessor genuinely valid for every subclass, which is why
/// they belong here and not on each concrete type. They are read through on every access rather
/// than cached at construction, matching how <see cref="Texture2D.Width"/>/<see cref="Texture2D.Height"/>
/// already behave.
/// </summary>
public abstract class Texture : GraphicsResource
{
    private readonly NativeResourceHandle _handle;

    protected Texture(GraphicsDevice graphicsDevice, nint nativeHandleValue)
        : this(graphicsDevice, nativeHandleValue, ownsHandle: true)
    {
    }

    /// <summary>Wraps a handle whose lifetime someone else controls when
    /// <paramref name="ownsHandle"/> is <see langword="false"/> -- see
    /// <see cref="NativeResourceHandle"/>'s own doc comment. Used by
    /// <c>VideoPlayer.GetTexture</c>, whose frame texture the *player* owns and replaces.</summary>
    private protected Texture(GraphicsDevice graphicsDevice, nint nativeHandleValue, bool ownsHandle)
        : base(graphicsDevice)
    {
        _handle = new NativeResourceHandle(nativeHandleValue, ReleaseNative, ownsHandle);
    }

    internal nint NativeHandleValue => _handle.DangerousGetHandle();

    /// <summary>How this specific texture kind releases its native handle. Every kind has its own
    /// destroy function (<c>cna_texture2d_destroy</c>, <c>cna_texture3d_destroy</c>,
    /// <c>cna_texturecube_destroy</c>, <c>cna_render_target_destroy</c>) -- there is no shared
    /// <c>cna_texture_destroy</c>, so this stays abstract rather than defaulting to the 2D
    /// one.</summary>
    protected abstract void ReleaseNative(nint handleValue);

    /// <summary>Number of mipmap levels. See this class's own doc comment for why this reads
    /// through to native on every access.</summary>
    public virtual int LevelCount => (int)GetTextureInfo().LevelCount;

    public virtual SurfaceFormat Format => (SurfaceFormat)GetTextureInfo().Format;

    private CnaTextureInfo GetTextureInfo()
    {
        var info = new CnaTextureInfo();
        CnaResult result = Native.cna_texture_get_info(new CnaHandle(NativeHandleValue), ref info);
        CnaException.ThrowIfFailed(result, "cna_texture_get_info");
        return info;
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposed)
        {
            return;
        }

        base.Dispose(disposing);
        _handle.Dispose();
    }
}
