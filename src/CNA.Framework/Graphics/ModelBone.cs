namespace CNA.Graphics;

/// <summary>
/// Bone data for a <see cref="Model"/>: a transform relative to a parent bone, plus the parent
/// itself and any children. Real XNA's own <c>ModelBone</c> has no public constructor and no
/// public <see cref="AddChild"/> at all -- both are content-pipeline-only (<c>internal</c>),
/// because real XNA games only ever get an already-populated bone hierarchy back from
/// <c>Content.Load&lt;Model&gt;</c>. This project has no content pipeline / model-file loader yet
/// (parsing a real model format is a separate, much larger problem than anything else built so far
/// this session -- see <see cref="Model"/>'s own doc comment), so the real openeggbert/cna C++
/// engine deliberately marks this constructor and <see cref="AddChild"/> <c>CNAEXT</c> (a
/// documented, intentional deviation from real XNA) to give hand-written code the only
/// construction path that actually exists here -- reproduced verbatim, not invented.
/// </summary>
public class ModelBone
{
    private readonly List<ModelBone> _children = [];

    public ModelBone(int index, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Index = index;
        Name = name;
        Children = new ModelBoneCollection(_children);
    }

    public string Name { get; }

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
