namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>GraphicsDevice</c>. A pure subclass -- <c>Clear(Color)</c> is inherited
/// unchanged from <see cref="CNA.Graphics.GraphicsDevice"/> and resolves correctly
/// against this namespace's <see cref="Color"/> argument through that struct's implicit
/// conversion operator, so no override is needed here. See docs/architecture.md.
/// </summary>
public class GraphicsDevice : CNA.Graphics.GraphicsDevice
{
    protected internal GraphicsDevice(nint nativeHandleValue)
        : base(nativeHandleValue)
    {
    }

    /// <summary>
    /// <c>SetVertexBuffer</c> is inherited unchanged (its <c>VertexBuffer</c> argument upcasts,
    /// same as every other compat method taking a native-backed resource type). <c>Indices</c>
    /// needs a `new` override since its declared type differs from the base property's -- but
    /// deliberately holds *no field of its own*: an earlier draft gave this property its own
    /// private backing field (mirroring <c>SetData</c>-style patterns elsewhere), which desyncs
    /// from the base class's own field whenever the object is accessed through a base-typed
    /// reference (e.g. <c>GraphicsDeviceManager.Game</c> is declared with the base <c>CNA.Game</c>
    /// type, so <c>manager.Game.GraphicsDevice.Indices</c> would silently read/write a different
    /// field than <c>this.GraphicsDevice.Indices</c> inside a <c>Game</c> subclass) -- caught by
    /// a code-review pass, not by testing (the desync needs two different static-type access
    /// paths to the same object to manifest, which no test here exercises). Fixed by making this
    /// a pure downcast pass-through to the base property's own single field instead, the same
    /// "no independent state, just a typed read/write-through" pattern
    /// <see cref="Microsoft.Xna.Framework.Audio.SoundEffectInstance.State"/> already uses.
    /// </summary>
    public new IndexBuffer? Indices
    {
        get => (IndexBuffer?)base.Indices;
        set => base.Indices = value;
    }

    public void DrawPrimitives(PrimitiveType primitiveType, int startVertex, int primitiveCount) =>
        base.DrawPrimitives((CNA.Graphics.PrimitiveType)(int)primitiveType, startVertex, primitiveCount);

    public void DrawIndexedPrimitives(
        PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount) =>
        base.DrawIndexedPrimitives(
            (CNA.Graphics.PrimitiveType)(int)primitiveType, baseVertex, minVertexIndex, numVertices, startIndex, primitiveCount);
}
