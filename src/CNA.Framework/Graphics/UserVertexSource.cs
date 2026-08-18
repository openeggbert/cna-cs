namespace CNA.Graphics;

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_UserVertexSource</c>
/// exactly (<c>graphics_device.h:897-908</c>). Public (unlike the internal
/// <c>CNA.Interop.CnaUserVertexSource</c> it mirrors) so CNA.XnaCompat's
/// <c>GraphicsDevice.DrawUserPrimitives&lt;T&gt;</c> override can identify a compat vertex type
/// and pass the identity into <see cref="GraphicsDevice.DrawUserPrimitivesRaw"/> without needing an
/// <c>InternalsVisibleTo</c> grant into CNA.Interop (compat vertex structs are separate types from
/// this project's own -- structs can't be subclassed to share one -- so the type-to-source mapping
/// itself has to live on each side of the CNA/XnaCompat boundary independently).</summary>
public enum UserVertexSource
{
    RawStream = 0,
    PositionColor = 1,
    PositionColorTexture = 2,
    PositionTexture = 3,
    PositionNormalTexture = 4,
}
