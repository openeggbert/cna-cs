namespace CNA.Graphics;

/// <summary>Matches real XNA's <c>CubeMapFace</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_CUBE_MAP_FACE_*</c> constants
/// (<c>render_target.h:38-50</c>).</summary>
public enum CubeMapFace
{
    PositiveX = 0,
    NegativeX = 1,
    PositiveY = 2,
    NegativeY = 3,
    PositiveZ = 4,
    NegativeZ = 5,
}
