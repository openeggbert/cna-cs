using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_VisualizationData</c> exactly
/// (<c>media.h:66-78</c>) -- both buffers are fixed-size (256 elements,
/// <c>CNA_VISUALIZATION_DATA_SIZE</c>) and passed as one caller-provided struct filled in place by
/// <see cref="Native.cna_media_player_get_visualization_data"/>, not the three flat
/// pointer/pointer/count arguments this project originally guessed. See
/// <see cref="CnaGameFrameHooks"/>'s own constructor doc comment for the self-populating
/// -constructor rationale.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaVisualizationData
{
    public uint StructSize;
    public uint StructVersion;
    public CnaFloatBuffer256 Frequencies;
    public CnaFloatBuffer256 Samples;

    public unsafe CnaVisualizationData()
    {
        StructSize = (uint)sizeof(CnaVisualizationData);
        StructVersion = 1;
    }
}

/// <summary>A fixed-capacity inline array of 256 floats, matching
/// <c>CNA_VISUALIZATION_DATA_SIZE</c> -- see <see cref="CnaGlyphBuffer"/> for why this project uses
/// the C# 12 <c>InlineArray</c> feature for fixed-size inline buffers like this one.</summary>
[InlineArray(256)]
internal struct CnaFloatBuffer256
{
    private float _element0;
}
