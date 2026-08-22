namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>ModelMesh</c>. Extends <c>CNA.Graphics.ModelMesh</c> directly.
/// <see cref="BoundingSphere"/>/<c>Tag</c>/<c>Draw</c> and this type's own
/// <c>Name</c> are all inherited unchanged -- <c>BoundingSphere</c> resolves correctly through
/// that struct's own implicit conversion operators (same as every other inherited-unchanged scalar
/// member across this compat layer), and <c>Draw</c> has no compat-typed parameter at all.
///
/// <see cref="MeshParts"/> is now a real, independently-tracked, compat-typed collection (a
/// follow-up pass closed the gap this doc comment used to describe) -- built directly from this
/// constructor's own <c>parts</c> parameter, the same "single construction seam,
/// provably compat-typed" pattern <see cref="Model"/>'s own <c>Bones</c>/<c>Meshes</c> already use:
/// <c>ModelMeshPartCollection</c> is (like <c>ModelBoneCollection</c>/<c>ModelMeshCollection</c>)
/// an independent reimplementation, not a subclass, so there is no covariant-return downcast
/// available for it, and no growth after construction to worry about (real XNA has no
/// <c>AddPart</c>-style method either).
///
/// <c>Effects</c>/<c>ModelEffectCollection</c> stay a real, narrower, documented gap,
/// deliberately *not* mirrored even in this follow-up pass -- unlike <c>MeshParts</c>, there is no
/// safe way to make it compat-typed: <c>ModelMeshPart.Effect</c>'s setter (inherited unchanged --
/// see <see cref="ModelMeshPart"/>'s own doc comment for why it stays that way) mutates the owning
/// mesh's <c>Effects</c> collection directly, and that collection is constructed at *field-initializer*
/// time inside the base <c>CNA.Graphics.ModelMesh</c> (<c>public ModelEffectCollection Effects { get; } = new();</c>)
/// with no override seam at all -- there is no point after construction where a subclass could ever
/// substitute a compat-typed collection the way <see cref="Model"/>'s own constructor substitutes
/// compat-typed <c>Bones</c>/<c>Meshes</c>. Making this safe would need <c>ModelMeshPart.Effect</c>'s
/// setter overridden with its own parallel-tracking logic (the same shape of problem
/// <c>ModelBone.Children</c>/<c>AddChild</c> solved for bones) *and* this class maintaining a
/// second, independent <c>Effects</c> collection kept in sync with it -- a materially bigger design
/// task than the rest of this pass, structurally closer to why <c>MediaPlayer.Queue</c> stays a
/// documented non-mirror than to anything solved elsewhere in the <c>Model</c> feature. `var`-typed/
/// `foreach` consumption of <c>Effects</c> still works correctly either way (compat <c>BasicEffect</c>
/// instances added to it upcast fine), same "only an explicit type declaration fails" shape as
/// every other narrow gap in this compat layer.
///
/// The constructor's own <c>new List&lt;ModelMeshPart&gt;(parts)</c> copy is a second allocation on
/// top of the base constructor's own internal list (built from the same <c>parts</c> argument via
/// the <c>base(...)</c> call just above it) -- a code-review finding, deliberately left as is:
/// this is the exact same shape of cost <see cref="Model"/>'s own constructor already accepts for
/// <c>Bones</c>/<c>Meshes</c> (an unavoidable consequence of "independently-tracked, compat-typed
/// collection built from a single construction seam" needing its own storage separate from the
/// base class's own, unreviewed-as-an-issue there across three separate review passes), and unlike
/// <see cref="Model"/>'s own <c>CopyAbsoluteBoneTransformsTo</c>/etc. (a real, hot, per-frame-call
/// allocation this session did fix), this one happens once per mesh, at load time, not once per
/// frame.
/// </summary>
public sealed class ModelMesh : CNA.Graphics.ModelMesh
{
    internal ModelMesh(GraphicsDevice graphicsDevice, IReadOnlyList<ModelMeshPart> parts)
        : this(graphicsDevice, string.Empty, parts)
    {
    }

    internal ModelMesh(GraphicsDevice graphicsDevice, string name, IReadOnlyList<ModelMeshPart> parts)
        : base(graphicsDevice.Framework, name, parts)
    {
        MeshParts = new ModelMeshPartCollection(new List<ModelMeshPart>(parts));
        Effects = new ModelEffectCollection(base.Effects);
    }

    public new ModelMeshPartCollection MeshParts { get; }

    public new ModelEffectCollection Effects { get; }

    public new BoundingSphere BoundingSphere
    {
        get => base.BoundingSphere;
        set => base.BoundingSphere = value;
    }

    public new string Name => base.Name;

    public new object? Tag
    {
        get => base.Tag;
        set => base.Tag = value;
    }

    public new void Draw() => base.Draw();

    /// <summary>Downcasts <c>base.ParentBone</c> rather than keeping separate storage -- safe
    /// because the only ways this is ever set (this setter, and <c>Model</c>'s own constructor
    /// assigning <c>meshParentBones</c>) both require a compat-typed <see cref="ModelBone"/>, so
    /// whatever ends up stored is provably compat-typed for every reachable compat instance.</summary>
    public new ModelBone? ParentBone
    {
        get => (ModelBone?)base.ParentBone;
    }

    internal void SetParentBone(ModelBone? value) => base.ParentBone = value;
}
