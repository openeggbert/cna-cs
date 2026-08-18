namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>ClearOptions</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_CLEAR_OPTION_*</c> constants
/// (<c>graphics_device.h:17-30</c>), which <see cref="GraphicsDevice.Clear(ClearOptions,Color,float,int)"/>
/// maps this to directly.</summary>
[Flags]
public enum ClearOptions
{
    Target = 1,
    DepthBuffer = 2,
    Stencil = 4,
}
