namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>TextureFilter</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_TEXTURE_FILTER_*</c> constants
/// (<c>graphics_state.h:143-161</c>).</summary>
public enum TextureFilter
{
    Linear = 0,
    Point = 1,
    Anisotropic = 2,
    LinearMipPoint = 3,
    PointMipLinear = 4,
    MinLinearMagPointMipLinear = 5,
    MinLinearMagPointMipPoint = 6,
    MinPointMagLinearMipLinear = 7,
    MinPointMagLinearMipPoint = 8,
}
