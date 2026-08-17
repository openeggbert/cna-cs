using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// <see cref="NativeHandleValue"/> is resolved once, in <c>Game.CreateGraphicsDevice</c>, and
/// cached here for this object's whole life -- but the real, shipped openeggbert/cna C API
/// documents the device handle <c>cna_game_get_graphics_device</c> returns as valid only until the
/// lifecycle callback that fetched it returns (confirmed directly against the real test suite, not
/// just the header doc comment -- see <c>Game.GetNativeGraphicsDeviceHandle</c>'s own doc comment).
/// This class does not yet account for that: every method below still trusts the handle it was
/// constructed with, unchanged since before the real ABI existed. A known, deliberate gap left by
/// the native-ABI migration's Game-lifecycle step (2) for its own GraphicsDevice step (3) to close
/// -- see <c>NEXT.md</c>'s native-ABI-migration entry.
/// </summary>
public class GraphicsDevice
{
    /// <summary>
    /// The raw native device handle value. <c>protected internal</c> (not <c>internal</c>) so
    /// CNA.XnaCompat's <c>GraphicsDevice</c> subclass constructor can forward it to <c>base()</c>
    /// without CNA.XnaCompat ever naming <see cref="CnaHandle"/> -- see docs/architecture.md.
    /// </summary>
    protected internal nint NativeHandleValue { get; }

    protected internal GraphicsDevice(nint nativeHandleValue)
    {
        NativeHandleValue = nativeHandleValue;
    }

    public void Clear(Color color)
    {
        CnaResult result = Native.cna_graphics_device_clear(new CnaHandle(NativeHandleValue), color.ToNative());
        CnaException.ThrowIfFailed(result, nameof(Clear));
    }

    /// <summary>
    /// Sets the active render target, or restores the back buffer when <paramref name="renderTarget"/>
    /// is <c>null</c>. Takes <see cref="Texture2D"/> rather than the stricter
    /// <see cref="RenderTarget2D"/> -- a deliberate, documented looseness (real XNA's signature
    /// is <c>SetRenderTarget(RenderTarget2D)</c>) that lets CNA.XnaCompat's <c>RenderTarget2D</c>
    /// (which inherits from CNA.XnaCompat's own <c>Texture2D</c>, not this project's
    /// <see cref="RenderTarget2D"/> -- see that type's doc comment) upcast straight into this
    /// parameter with no override needed, matching every other XnaCompat <c>Draw</c>/<c>Clear</c>
    /// overload's "inherited unchanged, converts through implicit operators" pattern. Passing a
    /// texture that was never created as a render target is a caller error this method does not
    /// (and, without a real native ABI to validate against, currently cannot) catch.
    /// </summary>
    public void SetRenderTarget(Texture2D? renderTarget)
    {
        CnaHandle handle = renderTarget is null ? CnaHandle.Zero : new CnaHandle(renderTarget.NativeHandleValue);
        CnaResult result = Native.cna_graphics_device_set_render_target(new CnaHandle(NativeHandleValue), handle);
        CnaException.ThrowIfFailed(result, nameof(SetRenderTarget));
    }

    public void SetVertexBuffer(VertexBuffer? vertexBuffer)
    {
        CnaHandle handle = vertexBuffer is null ? CnaHandle.Zero : new CnaHandle(vertexBuffer.NativeHandleValue);
        CnaResult result = Native.cna_graphics_device_set_vertex_buffer(new CnaHandle(NativeHandleValue), handle);
        CnaException.ThrowIfFailed(result, nameof(SetVertexBuffer));
    }

    private IndexBuffer? _indices;

    public IndexBuffer? Indices
    {
        get => _indices;
        set
        {
            CnaHandle handle = value is null ? CnaHandle.Zero : new CnaHandle(value.NativeHandleValue);
            CnaResult result = Native.cna_graphics_device_set_indices(new CnaHandle(NativeHandleValue), handle);
            CnaException.ThrowIfFailed(result, nameof(Indices));
            _indices = value;
        }
    }

    public void DrawPrimitives(PrimitiveType primitiveType, int startVertex, int primitiveCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startVertex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(primitiveCount);

        CnaResult result = Native.cna_graphics_device_draw_primitives(
            new CnaHandle(NativeHandleValue), (int)primitiveType, startVertex, primitiveCount);
        CnaException.ThrowIfFailed(result, nameof(DrawPrimitives));
    }

    /// <summary><paramref name="minVertexIndex"/>/<paramref name="numVertices"/> match real XNA's
    /// full signature exactly, but are not forwarded to native code -- on modern GPUs they are
    /// driver hints with no required effect on the draw itself (real XNA/MonoGame accept and
    /// mostly ignore them too), so this project's minimal native surface omits them rather than
    /// plumb unused parameters through the ABI.</summary>
    public void DrawIndexedPrimitives(
        PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseVertex);
        ArgumentOutOfRangeException.ThrowIfNegative(minVertexIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(numVertices);
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(primitiveCount);

        CnaResult result = Native.cna_graphics_device_draw_indexed_primitives(
            new CnaHandle(NativeHandleValue), (int)primitiveType, baseVertex, startIndex, primitiveCount);
        CnaException.ThrowIfFailed(result, nameof(DrawIndexedPrimitives));
    }
}
