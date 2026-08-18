using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_Texture2DCreateInfo</c> exactly
/// (<c>graphics.h:367-388</c>). This specific creation route -- <c>cna_texture2d_create</c> in
/// <c>graphics.h</c>, not any of the several routes in <c>texture.h</c>
/// (<c>create_standalone</c>/<c>create_from_rgba8</c>/<c>create_cpu_only_rgba8</c>/
/// <c>create_from_file[_with_device]</c>/<c>create_from_encoded_memory</c>, none of which allocate
/// an empty, dimensions-only, device-attached texture the way real XNA's own
/// <c>new Texture2D(device, width, height)</c> does) -- is the one that actually matches
/// <c>CNA.Graphics.Texture2D</c>'s own constructor. <see cref="Format"/> is always
/// <c>CNA_SURFACE_FORMAT_COLOR</c> (0) here, matching this project's own scope (no other surface
/// format is supported anywhere else in this codebase either). See <see cref="CnaGameFrameHooks"/>'s
/// own constructor doc comment for why this self-populates <see cref="StructSize"/>/
/// <see cref="StructVersion"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaTexture2DCreateInfo
{
    public uint StructSize;
    public uint StructVersion;
    public uint Width;
    public uint Height;
    public byte MipMap;
    public CnaReservedBytes3 Reserved;
    public uint Format;

    public unsafe CnaTexture2DCreateInfo()
    {
        StructSize = (uint)sizeof(CnaTexture2DCreateInfo);
        StructVersion = 1;
    }
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_Texture2DInfo</c> exactly
/// (<c>graphics.h:391-411</c>) -- unlike <c>texture.h</c>'s own, differently-shaped
/// <c>CNA_TextureInfo</c> (which reports only <c>level_count</c>/<c>format</c>, no dimensions at
/// all), this one is the real source of <see cref="Width"/>/<see cref="Height"/> for a
/// <c>graphics.h</c>-created texture -- resolves what was, before this migration, an open question
/// this session's own research forks could not answer from <c>texture.h</c> alone. See
/// <see cref="CnaGameFrameHooks"/>'s own constructor doc comment for the self-populating-constructor
/// rationale.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaTexture2DInfo
{
    public uint StructSize;
    public uint StructVersion;
    public uint Width;
    public uint Height;
    public uint LevelCount;
    public uint Format;

    public unsafe CnaTexture2DInfo()
    {
        StructSize = (uint)sizeof(CnaTexture2DInfo);
        StructVersion = 1;
    }
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_RenderTarget2DCreateInfo</c>
/// exactly (<c>render_target.h:59-83</c>) -- <c>RenderTarget2D</c> is its own real resource type
/// upstream, not a texture created with special usage flags the way this project originally guessed
/// (see <c>NEXT.md</c>'s native-ABI-migration entry, step 5). This project only ever wants CNA's own
/// defaults for everything beyond width/height/mip-mapping (depth/stencil none, no multisampling,
/// discard-contents), matching what <c>CNA.Graphics.RenderTarget2D</c>'s own constructor already
/// exposed before this migration (just <c>width</c>/<c>height</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaRenderTarget2DCreateInfo
{
    public uint StructSize;
    public uint StructVersion;
    public uint Width;
    public uint Height;
    public byte MipMap;
    public CnaReservedBytes3 Reserved;
    public uint ColorFormat;
    public uint DepthStencilFormat;
    public int MultiSampleCount;
    public uint Usage;
    public uint Reserved2;

    public unsafe CnaRenderTarget2DCreateInfo()
    {
        StructSize = (uint)sizeof(CnaRenderTarget2DCreateInfo);
        StructVersion = 1;
    }
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_RenderTargetInfo</c>
/// exactly (<c>render_target.h:107-135</c>). <see cref="RendererAvailable"/> is a real, degraded
/// -backend case this project previously had no concept of at all: creation can succeed even when
/// the active renderer has no real off-screen storage to back it, and binding such a render target
/// then fails with <c>CNA_RESULT_NOT_SUPPORTED</c> rather than the creation call itself
/// failing.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaRenderTargetInfo
{
    public uint StructSize;
    public uint StructVersion;
    public uint Kind;
    public uint Width;
    public uint Height;
    public uint LevelCount;
    public uint ColorFormat;
    public uint DepthStencilFormat;
    public int MultiSampleCount;
    public uint Usage;
    public byte ContentLost;
    public byte RendererAvailable;
    public ushort ReservedTail;

    public unsafe CnaRenderTargetInfo()
    {
        StructSize = (uint)sizeof(CnaRenderTargetInfo);
        StructVersion = 1;
    }
}

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_TextureInfo</c> exactly
/// (<c>texture.h:62-71</c>). Deliberately the *narrow* one: unlike <see cref="CnaTexture2DInfo"/>
/// it carries no dimensions at all, only the two properties every texture kind shares -- which is
/// exactly why it backs <c>CNA.Graphics.Texture</c>'s <c>LevelCount</c>/<c>Format</c> rather than
/// any one concrete texture type's. Its native accessor is documented as accepting a "Texture2D,
/// Texture3D, TextureCube or matching render-target handle" (<c>texture.h:126</c>), so the base
/// class can read it without knowing which subclass it is on.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaTextureInfo
{
    public uint StructSize;
    public uint StructVersion;
    public uint LevelCount;
    public uint Format;

    public unsafe CnaTextureInfo()
    {
        StructSize = (uint)sizeof(CnaTextureInfo);
        StructVersion = 1;
    }
}
