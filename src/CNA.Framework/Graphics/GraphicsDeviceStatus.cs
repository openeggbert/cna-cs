namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>GraphicsDeviceStatus</c> values exactly -- also confirmed
/// against the real, shipped openeggbert/cna C API's own <c>CNA_GRAPHICS_DEVICE_STATUS_*</c>
/// constants (<c>graphics_device.h:34-41</c>).</summary>
public enum GraphicsDeviceStatus
{
    Normal = 0,
    Lost = 1,
    NotReset = 2,
}
