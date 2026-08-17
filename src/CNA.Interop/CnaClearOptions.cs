namespace CNA.Interop;

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_ClearOptions</c> exactly
/// (<c>typedef uint32_t CNA_ClearOptions;</c> plus its <c>CNA_CLEAR_OPTION_*</c> value macros,
/// <c>graphics_device.h:17-30</c>) -- a bit set selecting which buffers
/// <see cref="Native.cna_graphics_device_clear_options"/> touches. Represented here as a C#
/// <c>[Flags] enum</c> (matching <see cref="CnaResult"/>'s own precedent for a fixed-width wire
/// identity) even though the real ABI convention itself avoids a C <c>enum</c> field on the wire --
/// that constraint is about the C struct/parameter representation, not about how this project
/// models the same values in C#.
/// </summary>
[Flags]
internal enum CnaClearOptions : uint
{
    Target = 1,
    DepthBuffer = 2,
    Stencil = 4,
}
