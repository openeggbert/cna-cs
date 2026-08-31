using System.Text;
using CNA.Interop;

namespace CNA.Graphics.Experimental;

/// <summary>
/// One frame's inputs to a post-process pass or chain.
///
/// A plain managed value rather than a handle: it describes a frame, owns nothing, and is passed by
/// reference only to avoid copying four matrices. The native structure it becomes is versioned and
/// caller-initialised, and this type is what keeps that detail out of a caller's way.
///
/// <b>Textures are borrowed for the duration of the call.</b> Nothing here retains
/// <see cref="Source"/> or <see cref="Destination"/>, so the caller must keep them alive across
/// <see cref="PostProcessChain.Apply"/> -- which is the ordinary situation, since the caller is
/// drawing with them.
///
/// <b>No pipeline settings.</b> CNA's context can carry a <c>RenderPipelineSettings</c> pointer;
/// upstream records that its C form is still a subset of the canonical type, so passing that subset
/// would silently apply engine defaults for every field it omits. A null settings pointer means
/// "the pass uses its own defaults", which is exactly what this type promises and nothing more.
/// </summary>
public struct PostProcessFrame
{
    /// <summary>The colour input, or <see langword="null"/> for none.</summary>
    public Texture2D? Source { get; set; }

    /// <summary>The destination render target, or <see langword="null"/> for the back buffer.</summary>
    public RenderTarget2D? Destination { get; set; }

    /// <summary>Destination width in pixels.</summary>
    public int Width { get; set; }

    /// <summary>Destination height in pixels.</summary>
    public int Height { get; set; }

    /// <summary>Seconds elapsed since the previous frame.</summary>
    public float ElapsedSeconds { get; set; }

    /// <summary>The camera's near plane distance.</summary>
    public float NearPlane { get; set; }

    /// <summary>The camera's far plane distance.</summary>
    public float FarPlane { get; set; }

    /// <summary>The camera's projection matrix, for a pass that reconstructs positions.</summary>
    public Matrix Projection { get; set; }

    /// <summary>Its inverse.</summary>
    public Matrix InverseProjection { get; set; }

    /// <summary>The inverse of the camera's view matrix.</summary>
    public Matrix InverseView { get; set; }

    /// <summary>The previous frame's view-projection, for a pass that reprojects. Ignored unless
    /// <see cref="HasPreviousFrame"/> is set, so a first frame is stated rather than implied by a
    /// zeroed matrix.</summary>
    public Matrix PreviousViewProjection { get; set; }

    /// <summary>Whether <see cref="PreviousViewProjection"/> describes a real previous frame.</summary>
    public bool HasPreviousFrame { get; set; }

    /// <summary>
    /// Builds the native context, starting from CNA's own defaults.
    ///
    /// <c>context_init</c> is called first and its result overwritten field by field, rather than a
    /// zeroed structure being filled in: the header is explicit that zero is not the default, and a
    /// later engine-layer revision adding a field would otherwise silently mean something.
    /// </summary>
    internal readonly CnaPostProcessContext ToNative()
    {
        var context = CnaPostProcessContext.Versioned();
        CnaResult result = Native.cna_post_process_context_init(ref context);
        CnaException.ThrowIfFailed(result, nameof(PostProcessFrame));

        context.Source = new CnaHandle(Source?.NativeHandleValue ?? 0);
        context.Destination = new CnaHandle(Destination?.NativeHandleValue ?? 0);
        context.Width = Width;
        context.Height = Height;
        context.ElapsedSeconds = ElapsedSeconds;
        context.NearPlane = NearPlane;
        context.FarPlane = FarPlane;
        context.Projection = Projection.ToNative();
        context.InverseProjection = InverseProjection.ToNative();
        context.InverseView = InverseView.ToNative();
        context.PreviousViewProjection = PreviousViewProjection.ToNative();
        context.HasPreviousFrame = HasPreviousFrame ? (byte)1 : (byte)0;
        return context;
    }
}

/// <summary>
/// One engine-layer post-process pass.
///
/// <b>Owned until it is handed over.</b> A pass this object created is destroyed by
/// <see cref="Dispose"/> -- unless it has been given to a chain through
/// <see cref="PostProcessChain.AddOwned"/>, after which this object owns nothing and disposing it
/// does nothing. That transfer is one-way and irreversible, which is why it is a separate method on
/// the chain rather than a flag.
///
/// <b>Not supported is not broken.</b> <see cref="IsSupportedOn"/> answering <see langword="false"/>
/// means the pass degrades -- typically to a copy -- rather than failing, so a chain may still run
/// it. Ask to know which behaviour you will get, not whether calling is safe.
/// </summary>
public sealed class PostProcessPass : IDisposable
{
    private readonly NativeResourceHandle _handle;
    private bool _surrendered;

    private PostProcessPass(nint handleValue) =>
        _handle = new NativeResourceHandle(
            handleValue,
            h => Native.cna_post_process_pass_destroy(new CnaHandle(h)).IsSuccess());

    /// <summary>Creates a pass that copies its source to its destination unchanged -- the identity
    /// of this family, and the one pass whose correct output can be asserted texel for texel without
    /// reimplementing a shader in the test.</summary>
    public static PostProcessPass CreateBlit(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        CnaResult result = Native.cna_blit_pass_create(
            graphicsDevice.ResolveNativeDeviceHandle(), out CnaHandle pass);
        CnaException.ThrowIfFailed(result, nameof(CreateBlit));
        return new PostProcessPass(pass.AsNint);
    }

    /// <summary>
    /// Creates a bloom pass: extract what is brighter than a threshold, blur it, add it back.
    ///
    /// Configure it through <see cref="Bloom"/>. The threshold, intensity and iteration count live
    /// on the native pass, not here, so a pass added to a chain and reconfigured afterwards takes
    /// the new settings -- which is the shape a game wants when quality is a menu option.
    /// </summary>
    public static PostProcessPass CreateBloom(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        CnaResult result = Native.cna_bloom_pass_create(
            graphicsDevice.ResolveNativeDeviceHandle(), out CnaHandle pass);
        CnaException.ThrowIfFailed(result, nameof(CreateBloom));
        return new PostProcessPass(pass.AsNint);
    }

    /// <summary>Creates a tonemap pass, which maps an HDR image into a displayable range.
    /// Configure it through <see cref="Tonemap"/>.</summary>
    public static PostProcessPass CreateTonemap(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        CnaResult result = Native.cna_tonemap_pass_create(
            graphicsDevice.ResolveNativeDeviceHandle(), out CnaHandle pass);
        CnaException.ThrowIfFailed(result, nameof(CreateTonemap));
        return new PostProcessPass(pass.AsNint);
    }

    /// <summary>
    /// This pass's bloom settings.
    ///
    /// A typed view rather than properties on <see cref="PostProcessPass"/> itself, because a
    /// threshold means nothing on a blit and a type that offered one on every pass would be saying
    /// otherwise. Asking a pass of another kind is refused by CNA with
    /// <c>InvalidArgument</c> -- native does the type check, and this does not second-guess it.
    /// </summary>
    public BloomSettings Bloom => _bloom ??= new BloomSettings(this);

    /// <summary>This pass's tonemap settings, on the same terms as <see cref="Bloom"/>.</summary>
    public TonemapSettings Tonemap => _tonemap ??= new TonemapSettings(this);

    private BloomSettings? _bloom;
    private TonemapSettings? _tonemap;

    /// <summary>The handle, for the settings views in this file.</summary>
    internal CnaHandle NativeHandle => Handle;

    /// <summary>The pass's own name, as CNA reports it.</summary>
    public unsafe string Name
    {
        get
        {
            CnaResult sizeResult = Native.cna_post_process_pass_copy_name(Handle, null, 0, out ulong required);
            if (sizeResult.IsFailure() && sizeResult != CnaResult.BufferTooSmall)
            {
                CnaException.ThrowIfFailed(sizeResult, nameof(Name));
            }

            if (required == 0)
            {
                GC.KeepAlive(this);
                return string.Empty;
            }

            var bytes = new byte[checked((int)required)];
            fixed (byte* destination = bytes)
            {
                CnaResult result = Native.cna_post_process_pass_copy_name(
                    Handle, destination, (ulong)bytes.Length, out _);
                CnaException.ThrowIfFailed(result, nameof(Name));
            }

            GC.KeepAlive(this);
            return Encoding.UTF8.GetString(bytes);
        }
    }

    /// <summary>Whether the pass can do its real work on this device, as opposed to degrading.</summary>
    public bool IsSupportedOn(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        CnaResult result = Native.cna_post_process_pass_is_supported(
            Handle, graphicsDevice.ResolveNativeDeviceHandle(), out byte supported);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(IsSupportedOn));
        return supported != 0;
    }

    /// <summary>Runs this pass alone over one frame's inputs.</summary>
    public void Apply(in PostProcessFrame frame)
    {
        CnaPostProcessContext context = frame.ToNative();
        CnaResult result = Native.cna_post_process_pass_apply(Handle, in context);
        GC.KeepAlive(this);
        GC.KeepAlive(frame.Source);
        GC.KeepAlive(frame.Destination);
        CnaException.ThrowIfFailed(result, nameof(Apply));
    }

    /// <summary>Releases the pass, and the effect it owns if it owns one. Does nothing once the pass
    /// has been handed to a chain, and is safe to call repeatedly.</summary>
    public void Dispose() => _handle.Dispose();

    /// <summary>Whether this object still owns its native pass. <see langword="false"/> once
    /// <see cref="PostProcessChain.AddOwned"/> has taken it, after which every other member throws
    /// <see cref="ObjectDisposedException"/>.</summary>
    public bool OwnsNativePass => !_surrendered;

    internal CnaHandle Handle
    {
        get
        {
            // Surrendering is not disposal, and neither is it a state a caller can recover from, so
            // it produces the same refusal: every operation after AddOwned would otherwise reach a
            // handle CNA has already consumed. SafeHandle.Detach leaves the value readable -- it
            // only stops the release -- so without this check the wrapper stays usable and wrong.
            ObjectDisposedException.ThrowIf(_surrendered, this);
            return new CnaHandle(_handle.DangerousGetHandle());
        }
    }

    /// <summary>
    /// Gives up ownership without releasing anything, for <see cref="PostProcessChain.AddOwned"/>.
    ///
    /// Called <em>before</em> the result is checked, deliberately: the route documents the pass
    /// handle as invalid on return whether or not the call succeeded, so a wrapper that detached
    /// only on success would destroy a handle CNA had already consumed.
    /// </summary>
    internal void GiveUpOwnership()
    {
        _handle.Detach();
        _surrendered = true;
    }
}

/// <summary>
/// An ordered chain of post-process passes, from CNA's engine layer.
///
/// <b>Experimental, and not XNA.</b> XNA has no post-process concept at all; a game writes its own
/// ping-pong between render targets. This is CNA's, and it builds on the same
/// <see cref="RenderTargetPool"/> the first engine-layer slice bound -- <see cref="BorrowTargetPool"/>
/// hands back the very pool the chain ping-pongs through, which is why the two slices belong
/// together rather than beside each other.
///
/// <b>Three different ownerships meet here, and each is tested rather than documented:</b>
///
/// <list type="bullet">
/// <item><see cref="Add"/> appends a pass the caller keeps owning. Destroying the chain does not
/// release it, so the caller still must.</item>
/// <item><see cref="AddOwned"/> hands the pass over. The native handle is consumed whether or not
/// the call succeeds, so the managed wrapper gives up ownership first and is inert afterwards.</item>
/// <item><see cref="BorrowTargetPool"/> is a <em>counted</em> borrow: destroying the chain is
/// refused while a borrow is outstanding, the same contract <see cref="PooledRenderTarget"/> has
/// against its pool.</item>
/// </list>
///
/// <b>Availability is asked, not assumed.</b> Every engine-layer route resolves in every CNA build
/// and a build without the layer answers <c>NOT_SUPPORTED</c> at call time, so
/// <see cref="GraphicsDevice.IsCnaEngineLayerAvailable"/> is the question -- symbol resolution
/// answers a different one.
/// </summary>
public sealed class PostProcessChain : IDisposable
{
    private readonly NativeResourceHandle _handle;
    private readonly GraphicsDevice _graphicsDevice;

    /// <summary>Creates an empty chain. Throws on a build with no engine layer rather than returning
    /// a chain whose every operation would fail.</summary>
    public PostProcessChain(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        _graphicsDevice = graphicsDevice;
        CnaResult result = Native.cna_post_process_chain_create(
            graphicsDevice.ResolveNativeDeviceHandle(), out CnaHandle chain);
        CnaException.ThrowIfFailed(result, nameof(PostProcessChain));

        _handle = new NativeResourceHandle(
            chain.AsNint,
            h => Native.cna_post_process_chain_destroy(new CnaHandle(h)).IsSuccess());
    }

    /// <summary>How many passes the chain holds.</summary>
    public int PassCount
    {
        get
        {
            CnaResult result = Native.cna_post_process_chain_get_pass_count(Handle, out int count);
            GC.KeepAlive(this);
            CnaException.ThrowIfFailed(result, nameof(PassCount));
            return count;
        }
    }

    /// <summary>Appends a pass the caller keeps owning. The caller must outlive the chain's use of
    /// it and must dispose it.</summary>
    public void Add(PostProcessPass pass)
    {
        ArgumentNullException.ThrowIfNull(pass);

        CnaResult result = Native.cna_post_process_chain_add_pass(Handle, pass.Handle);
        GC.KeepAlive(this);
        GC.KeepAlive(pass);
        CnaException.ThrowIfFailed(result, nameof(Add));
    }

    /// <summary>
    /// Appends a pass and takes ownership of it. <paramref name="pass"/> owns nothing afterwards and
    /// disposing it does nothing.
    ///
    /// Ownership is surrendered before the result is checked, because CNA consumes the handle
    /// whether or not the call succeeds.
    /// </summary>
    public void AddOwned(PostProcessPass pass)
    {
        ArgumentNullException.ThrowIfNull(pass);

        CnaHandle handle = pass.Handle;
        pass.GiveUpOwnership();

        CnaResult result = Native.cna_post_process_chain_add_owned_pass(Handle, handle);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(AddOwned));
    }

    /// <summary>Removes every pass, releasing the ones the chain owns and leaving the others alone.</summary>
    public void Clear()
    {
        CnaResult result = Native.cna_post_process_chain_clear(Handle);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(Clear));
    }

    /// <summary>Runs every pass in order, ping-ponging between pooled intermediate targets, and
    /// leaves the result in <see cref="PostProcessFrame.Destination"/>.</summary>
    public void Apply(in PostProcessFrame frame)
    {
        CnaPostProcessContext context = frame.ToNative();
        CnaResult result = Native.cna_post_process_chain_apply(Handle, in context);
        GC.KeepAlive(this);
        GC.KeepAlive(frame.Source);
        GC.KeepAlive(frame.Destination);
        CnaException.ThrowIfFailed(result, nameof(Apply));
    }

    /// <summary>Releases the chain's pooled intermediate targets.</summary>
    public void ResetTargets()
    {
        CnaResult result = Native.cna_post_process_chain_reset_targets(Handle);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(ResetTargets));
    }

    /// <summary>
    /// Borrows the pool the chain ping-pongs through, so a caller can see or bound what the chain
    /// is holding.
    ///
    /// The returned pool is a borrow, not the caller's to keep: destroying the chain is refused
    /// while it is outstanding. Disposing the borrow is what releases it, and the same
    /// <c>pool_destroy</c> route that would destroy an owned pool decrements a borrow instead --
    /// which is CNA's model, not a coincidence this wrapper relies on.
    /// </summary>
    public RenderTargetPool BorrowTargetPool()
    {
        CnaResult result = Native.cna_post_process_chain_get_target_pool(Handle, out CnaHandle pool);
        GC.KeepAlive(this);
        CnaException.ThrowIfFailed(result, nameof(BorrowTargetPool));

        return RenderTargetPool.CreateBorrowed(_graphicsDevice, pool.AsNint);
    }

    /// <summary>Releases the chain and every pass it owns. Refused by native while a target-pool
    /// borrow is outstanding.</summary>
    public void Dispose() => _handle.Dispose();

    private CnaHandle Handle => new(_handle.DangerousGetHandle());
}
