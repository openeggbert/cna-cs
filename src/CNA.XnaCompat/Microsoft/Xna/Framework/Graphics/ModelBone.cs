namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>ModelBone</c>. Extends <c>CNA.Graphics.ModelBone</c> directly --
/// <c>Name</c>/<see cref="Index"/>/<c>Transform</c> are inherited unchanged (no compat-type
/// crossing in any of them). <see cref="Children"/> needs its own independently-tracked storage:
/// the base class's own <c>Children</c> is built inside the base constructor from the base class's
/// own private list, with no seam for a subclass to substitute a compat-typed collection (the same
/// "container type is fixed at construction, no override point" wall <c>MediaLibrary</c>'s
/// picture-side collections hit this session) -- <see cref="AddChild"/> grows both the base's own
/// bookkeeping (<c>base.AddChild</c>, so anything reading through a base-typed reference still sees
/// correct, if base-typed, data) and this class's own parallel, compat-typed list; it's a genuinely
/// new overload rather than `new`-hiding, since its compat-typed parameter doesn't match the base
/// method's signature at all. <see cref="Parent"/> stays a downcast of the base property rather
/// than separate storage -- safe because every reachable compat <see cref="ModelBone"/> only ever
/// has <see cref="AddChild"/> called on it with a compat-typed child (this method's own parameter
/// type enforces that), so whatever <c>base.AddChild</c> stores in the base <c>Parent</c> property
/// is provably compat-typed too.
/// </summary>
public sealed class ModelBone : CNA.Graphics.ModelBone
{
    private readonly List<ModelBone> _children = [];

    public ModelBone(int index, string name)
        : base(index, name)
    {
        Children = new ModelBoneCollection(_children);
    }

    public new ModelBoneCollection Children { get; }

    public new ModelBone? Parent => (ModelBone?)base.Parent;

    public void AddChild(ModelBone child)
    {
        ArgumentNullException.ThrowIfNull(child);

        base.AddChild(child);
        _children.Add(child);
    }
}
