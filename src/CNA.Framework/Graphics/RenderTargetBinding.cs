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
    /// <paramref name="arraySlice"/> must be zero: the ABI's binding struct is version one, which
    /// "supports only zero", and accepting another value would bind the wrong subresource rather
    /// than fail.
    /// </summary>
    public RenderTargetBinding(Texture renderTarget, int arraySlice = 0)
    {
        ArgumentNullException.ThrowIfNull(renderTarget);
        ArgumentOutOfRangeException.ThrowIfNegative(arraySlice);

        if (arraySlice != 0)
        {
            throw new NotSupportedException(
                "Only array slice 0 is supported -- CNA_RenderTargetBinding version one supports no other.");
        }

        RenderTarget = renderTarget;
        ArraySlice = arraySlice;
        CubeMapFace = CubeMapFace.PositiveX;
    }

    /// <summary>Binds one face of a cube target. See the 2D constructor for why
    /// <paramref name="renderTarget"/> is a <see cref="Texture"/>.</summary>
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
