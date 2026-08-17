namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>ModelMesh</c>. Extends <c>CNA.Graphics.ModelMesh</c> directly.
/// <see cref="BoundingSphere"/>/<c>Tag</c>/<c>Draw</c> and this type's own
/// <c>Name</c> are all inherited unchanged -- <c>BoundingSphere</c> resolves correctly through
/// that struct's own implicit conversion operators (same as every other inherited-unchanged scalar
/// member across this compat layer), and <c>Draw</c> has no compat-typed parameter at all.
///
/// <c>MeshParts</c>/<c>Effects</c> are also inherited unchanged, staying base-typed
/// (<c>CNA.Graphics.ModelMeshPartCollection</c>/<c>ModelEffectCollection</c>) -- a real, narrow,
/// documented compat gap, the same shape and same justification as <c>BasicEffect</c>'s own
/// <c>CurrentTechnique</c>/<c>Passes</c>/<c>DirectionalLight0-2</c> gap: <c>ModelMeshPart</c>'s own
/// <c>VertexBuffer</c>/<c>IndexBuffer</c>/<c>Effect</c> properties can already legitimately *hold*
/// compat-typed instances (compat <c>VertexBuffer</c>/<c>IndexBuffer</c>/<c>BasicEffect</c> all
/// subclass their base counterparts directly), so ordinary, `var`-typed/`foreach` consumption of
/// mesh parts works correctly either way -- only an explicit
/// <c>Microsoft.Xna.Framework.Graphics.ModelMeshPart</c>/<c>ModelMeshPartCollection</c>/
/// <c>ModelEffectCollection</c> type declaration would fail to compile. Mirroring those three types
/// too is a real, separate follow-up, not attempted in this pass.
/// </summary>
public sealed class ModelMesh : CNA.Graphics.ModelMesh
{
    public ModelMesh(GraphicsDevice graphicsDevice, IReadOnlyList<CNA.Graphics.ModelMeshPart> parts)
        : base(graphicsDevice, parts)
    {
    }

    public ModelMesh(GraphicsDevice graphicsDevice, string name, IReadOnlyList<CNA.Graphics.ModelMeshPart> parts)
        : base(graphicsDevice, name, parts)
    {
    }

    /// <summary>Downcasts <c>base.ParentBone</c> rather than keeping separate storage -- safe
    /// because the only ways this is ever set (this setter, and <c>Model</c>'s own constructor
    /// assigning <c>meshParentBones</c>) both require a compat-typed <see cref="ModelBone"/>, so
    /// whatever ends up stored is provably compat-typed for every reachable compat instance.</summary>
    public new ModelBone? ParentBone
    {
        get => (ModelBone?)base.ParentBone;
        set => base.ParentBone = value;
    }
}
