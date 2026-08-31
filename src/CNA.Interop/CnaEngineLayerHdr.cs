namespace CNA.Interop;

/// <summary>
/// Mirrors the <c>CNA_TonemappingMode</c> identities exactly (<c>graphics_ext.h:81-90</c> plus
/// <c>engine_layer.h:8222</c>): the curve a tonemap pass applies.
///
/// <see cref="Uncharted2"/> is declared in a different header from the other four, and appended
/// rather than inserted: upstream records that the preceding values are stored in pipeline settings
/// and compared by ordinal, so renumbering them would reinterpret saved settings. It is also the one
/// curve that does not bake gamma into itself, so the pipeline's gamma step still applies after it.
/// </summary>
internal enum CnaTonemappingMode : uint
{
    None = 0,
    Reinhard = 1,
    Filmic = 2,
    Aces = 3,
    Uncharted2 = 4,
}

/// <summary>Mirrors <c>graphics_ext.h</c>'s <c>CNA_RenderQuality</c> exactly
/// (<c>graphics_ext.h:55-64</c>).</summary>
internal enum CnaRenderQuality : uint
{
    Low = 0,
    Medium = 1,
    High = 2,
    Ultra = 3,
}
