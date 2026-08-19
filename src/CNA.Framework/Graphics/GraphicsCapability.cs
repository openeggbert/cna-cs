namespace CNA.Graphics;

/// <summary>
/// One capability of the active renderer -- <c>graphics.h</c>'s <c>CNA_GRAPHICS_CAPABILITY_*</c>
/// identities.
///
/// Not an XNA type. XNA had no capability query at all: it had two fixed profiles, Reach and HiDef,
/// and every device that reported a profile supported everything in it. CNA ships renderers that
/// genuinely differ -- SDL_RENDERER is 2D-only and says so -- so "what profile is this" and "can
/// this device draw a triangle" are separate questions here.
///
/// Asking is worth it because the alternative is finding out from a draw call. A recognized but
/// unavailable capability is a <em>successful</em> query that answers false; operations needing it
/// then fail with <c>NOT_SUPPORTED</c> rather than silently substituting something else.
/// </summary>
public enum GraphicsCapability : uint
{
    /// <summary>3D draw submission. False on a 2D-only renderer, where
    /// <c>DrawUserPrimitives</c> and friends throw.</summary>
    ThreeD = 0,

    DepthStencilBuffer = 1,

    MultiSampleAntiAliasing = 2,

    /// <summary>More than one simultaneous render target.</summary>
    MultipleRenderTargets = 3,

    AnisotropicFiltering = 4,

    WireFrame = 5,

    OcclusionQuery = 6,

    /// <summary>Custom effects built from shader <em>source</em>. Separate from
    /// <see cref="CompiledEffects"/>, which is about pre-compiled bytecode.</summary>
    CustomEffects = 7,

    Texture3D = 8,

    MultiStreamVertexInput = 9,

    Instancing = 10,

    StencilBuffer = 11,

    AdditiveBlending = 12,

    /// <summary>Compiled Effect Framework bytecode -- what
    /// <see cref="Effect(GraphicsDevice, byte[])"/> and <c>Load&lt;Effect&gt;</c> need for a
    /// compiled asset. The header recommends asking this in advance rather than branching on a
    /// file name, since only the compiled shape depends on it.</summary>
    CompiledEffects = 13,
}
