namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>RenderTargetBinding</c>: one render-target subresource in a
/// multiple-render-target array, as passed to
/// <see cref="GraphicsDevice.SetRenderTargets(RenderTargetBinding[])"/>.
///
/// A struct with implicit conversions from the two render-target types, matching XNA -- which is
/// what makes <c>SetRenderTargets(target1, target2)</c> compile without a cast.
///
/// Was missing entirely until the WP16 re-audit, along with the MRT route it exists for
/// (<c>cna_graphics_device_set_render_targets</c>, <c>render_target.h:238</c>). Only the
/// single-target overloads had been bound.
/// </summary>
public readonly struct RenderTargetBinding : IEquatable<RenderTargetBinding>
{
    /// <summary>
    /// Binds a 2D target.
    ///
    /// <paramref name="renderTarget"/> is typed as <see cref="Texture"/> rather than
    /// <see cref="RenderTarget2D"/>, matching the engine's own <c>Texture*</c>. Two reasons: native
    /// validates the handle, dimensions and sample count before binding anyway, so a tighter
    /// managed type buys nothing it does not already enforce; and CNA.XnaCompat's own
    /// <c>RenderTarget2D</c> derives from *its* <c>Texture2D</c>, not from this namespace's
    /// <see cref="RenderTarget2D"/>, so a tighter type here would make the compat binding
    /// unconstructible.
    ///
    /// <paramref name="arraySlice"/> must be zero, and the reason matters more than the rule.
    /// This used to cite the binding struct's version -- "version one supports only zero" -- which
    /// says the limit will lift with a version two. It will not. Canonical <c>SetRenderTargets</c>
    /// refuses a nonzero slice for a <c>RenderTarget2D</c> outright, so this is XNA's own behaviour
    /// rather than a stage this ABI is passing through; upstream corrected the header's wording
    /// (CBIND-070) after this project quoted it back as a limitation.
    ///
    /// The distinction is not academic: a caller told "version one" waits for version two, and a
    /// caller told "the API refuses it" writes different code.
    /// </summary>
    public RenderTargetBinding(Texture renderTarget, int arraySlice = 0)
    {
        ArgumentNullException.ThrowIfNull(renderTarget);
        ArgumentOutOfRangeException.ThrowIfNegative(arraySlice);

        if (arraySlice != 0)
        {
            throw new NotSupportedException(
                $"A RenderTarget2D binding cannot select array slice {arraySlice}. XNA's own " +
                "SetRenderTargets refuses a nonzero slice for a 2D target, and the C API answers " +
                "NOT_SUPPORTED to match -- this is not a limit of the current ABI version that a " +
                "later one will lift. Bind a cube face through the CubeMapFace constructor instead.");
        }

        RenderTarget = renderTarget;
        ArraySlice = arraySlice;
        CubeMapFace = CubeMapFace.PositiveX;
    }

    /// <summary>
    /// Binds one face of a cube target. See the 2D constructor for why
    /// <paramref name="renderTarget"/> is a <see cref="Texture"/>.
    ///
    /// The array slice is fixed at zero here rather than offered: for a cube target the field means
    /// nothing, because the face selects the subresource. Native still requires it to be zero
    /// rather than ignoring it, so that a caller who set it believing otherwise is told -- which is
    /// why this constructor does not take one at all.
    /// </summary>
    public RenderTargetBinding(Texture renderTarget, CubeMapFace cubeMapFace)
    {
        ArgumentNullException.ThrowIfNull(renderTarget);

        RenderTarget = renderTarget;
        ArraySlice = 0;
        CubeMapFace = cubeMapFace;
    }

    /// <summary>The bound target. <see langword="null"/> only for a default-constructed binding,
    /// which is not usable -- XNA's own default constructor produces the same.</summary>
    public Texture? RenderTarget { get; }

    public int ArraySlice { get; }

    /// <summary>Meaningful only for a cube target; <see cref="CubeMapFace.PositiveX"/> otherwise,
    /// which is what the ABI requires a 2D binding to carry.</summary>
    public CubeMapFace CubeMapFace { get; }

    public static implicit operator RenderTargetBinding(RenderTarget2D renderTarget) => new(renderTarget, 0);

    public readonly bool Equals(RenderTargetBinding other) =>
        ReferenceEquals(RenderTarget, other.RenderTarget)
        && ArraySlice == other.ArraySlice
        && CubeMapFace == other.CubeMapFace;

    public override readonly bool Equals(object? obj) => obj is RenderTargetBinding other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(RenderTarget, ArraySlice, CubeMapFace);

    public static bool operator ==(RenderTargetBinding a, RenderTargetBinding b) => a.Equals(b);

    public static bool operator !=(RenderTargetBinding a, RenderTargetBinding b) => !a.Equals(b);
}
