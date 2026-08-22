namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Represents bone data for a model.</summary>
public sealed class ModelBone
{
    private readonly List<ModelBone> _children = [];
    private ModelBone? _parent;

    internal ModelBone(int index, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Index = index;
        Name = name;
        Transform = Matrix.Identity;
        Children = new ModelBoneCollection(_children);
    }

    public string Name { get; }

    public int Index { get; }

    public Matrix Transform { get; set; }

    public ModelBone? Parent => _parent;

    public ModelBoneCollection Children { get; }

    internal void AddChild(ModelBone child)
    {
        ArgumentNullException.ThrowIfNull(child);

        child._parent = this;
        _children.Add(child);
    }
}
