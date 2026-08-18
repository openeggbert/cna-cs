namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>RenderTargetUsage</c> values exactly -- also confirmed against
/// the real, shipped openeggbert/cna C API's own <c>CNA_RENDER_TARGET_USAGE_*</c> constants
/// (<c>render_target.h:29-35</c>).</summary>
public enum RenderTargetUsage
{
    DiscardContents = 0,
    PreserveContents = 1,
    PlatformContents = 2,
}
