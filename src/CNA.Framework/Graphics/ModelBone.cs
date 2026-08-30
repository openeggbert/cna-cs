namespace CNA.Graphics;

/// <summary>
/// Bone data for a <see cref="Model"/>: a transform relative to a parent bone, plus the parent
/// itself and any children. Real XNA's own <c>ModelBone</c> has no public constructor and no
/// public <see cref="AddChild"/> at all -- both are content-pipeline-only (<c>internal</c>),
/// because real XNA games only ever get an already-populated bone hierarchy back from
/// <c>Content.Load&lt;Model&gt;</c>. The real openeggbert/cna C++ engine deliberately marks this
/// constructor and <see cref="AddChild"/> <c>CNAEXT</c> (a documented, intentional deviation from
/// real XNA) so hand-written code has a construction path too -- reproduced verbatim, not invented.
/// <c>CNA.Content.Xnb.XnbModelBuilder</c> (this project's own real <c>.xnb</c> loader, driving
/// <c>ContentManager.Load&lt;Model&gt;()</c>) uses this exact same public constructor/<c>AddChild</c>
/// surface to build a loaded model's bone hierarchy -- there is no separate, more-privileged
/// construction path reserved for it.
/// </summary>
public class ModelBone
{
    private readonly List<ModelBone> _children = [];

    /// <summary>
    /// <paramref name="name"/> may be null, because XNA content permits an unnamed bone.
    ///
    /// This rejected null until the content *loading* survey ran: twenty models in the XNA sample
    /// collection have unnamed bones or meshes, they shipped working on XNA, and this refused all of
    /// them. Storing an empty string instead would be worse -- it invents a name the file does not
    /// contain, and a game comparing bone names would then match the wrong bone.
    /// </summary>
    public ModelBone(int index, string? name)
    {
        Index = index;
        Name = name;
        Children = new ModelBoneCollection(_children);
    }

    public string? Name { get; }

    public int Index { get; }

    public Matrix Transform { get; set; } = Matrix.Identity;

    public ModelBone? Parent { get; private set; }

    public ModelBoneCollection Children { get; }

    public void AddChild(ModelBone child)
    {
        ArgumentNullException.ThrowIfNull(child);

        child.Parent = this;
        _children.Add(child);
    }
}
