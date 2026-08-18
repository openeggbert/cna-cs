namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>TextureAddressMode</c> values exactly -- also confirmed against
/// the real, shipped openeggbert/cna C API's own <c>CNA_TEXTURE_ADDRESS_*</c> constants
/// (<c>graphics_state.h:134-140</c>).</summary>
public enum TextureAddressMode
{
    Wrap = 0,
    Clamp = 1,
    Mirror = 2,
}
