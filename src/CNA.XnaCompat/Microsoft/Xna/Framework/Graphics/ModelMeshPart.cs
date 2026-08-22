namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>ModelMeshPart</c>. Extends <c>CNA.Graphics.ModelMeshPart</c> directly,
/// fully inherited-unchanged: <c>VertexBuffer</c>/<c>IndexBuffer</c>/<c>Effect</c>/<c>NumVertices</c>/
/// <c>PrimitiveCount</c>/<c>StartIndex</c>/<c>VertexOffset</c>/<c>Tag</c>/<c>SetVertexBuffer</c>/
/// <c>SetIndexBuffer</c>/<c>SetVertexOffset</c>/<c>SetNumVertices</c>/<c>SetStartIndex</c>/
/// <c>SetPrimitiveCount</c> all resolve correctly as-is. <c>VertexBuffer</c>/<c>IndexBuffer</c>'s
/// *declared* type stays <c>CNA.Graphics</c>-namespaced (a compat-typed instance still upcasts
/// through <c>SetVertexBuffer</c>/<c>SetIndexBuffer</c>'s inherited, base-typed parameters just
/// fine, so no override is needed to actually *use* a compat <see cref="VertexBuffer"/>/
/// <see cref="IndexBuffer"/> here); <c>Effect</c>'s declared type is <c>CNA.Graphics.Effect</c>
/// regardless of whether this <see cref="ModelMeshPart"/> is compat-typed, since this compat layer
/// has no separate compat <c>Effect</c> hierarchy at all (see <see cref="BasicEffect"/>'s own doc
/// comment). This type exists so <see cref="ModelMeshPartCollection"/> (and, through it,
/// <see cref="ModelMesh.MeshParts"/>) can hold a compat-namespaced element type -- the same
/// "an explicit type declaration was the only thing that needed fixing" reasoning
/// <c>BasicEffect.CurrentTechnique</c>'s own gap already established.
///
/// <c>ModelMesh.Effects</c>/<c>ModelEffectCollection</c> stay a documented, narrower gap,
/// not mirrored by this pass -- see <see cref="ModelMesh"/>'s own doc comment for why.
/// </summary>
public sealed class ModelMeshPart : CNA.Graphics.ModelMeshPart
{
    internal ModelMeshPart()
    {
    }

    internal ModelMeshPart(VertexBuffer? vertexBuffer, IndexBuffer? indexBuffer, int numVertices, int primitiveCount, int startIndex, int vertexOffset)
        : base(vertexBuffer?.FrameworkBuffer, indexBuffer?.FrameworkBuffer, numVertices, primitiveCount, startIndex, vertexOffset)
    {
    }

    public new Effect? Effect
    {
        get => Effect.FromFramework(base.Effect);
        set => base.Effect = value?.Inner;
    }

    public new IndexBuffer? IndexBuffer =>
        global::Microsoft.Xna.Framework.Graphics.IndexBuffer.FromFramework(base.IndexBuffer);

    public new VertexBuffer? VertexBuffer =>
        global::Microsoft.Xna.Framework.Graphics.VertexBuffer.FromFramework(base.VertexBuffer);

    public new int NumVertices => base.NumVertices;

    public new int PrimitiveCount => base.PrimitiveCount;

    public new int StartIndex => base.StartIndex;

    public new int VertexOffset => base.VertexOffset;

    public new object? Tag
    {
        get => base.Tag;
        set => base.Tag = value;
    }
}
