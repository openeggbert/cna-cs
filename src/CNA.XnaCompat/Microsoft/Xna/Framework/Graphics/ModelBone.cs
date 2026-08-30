namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Represents bone data for a model.</summary>
public sealed class ModelBone
{
    private readonly List<ModelBone> _children = [];
    private ModelBone? _parent;

    /// <summary><paramref name="name"/> may be null: XNA content permits an unnamed bone, and
    /// twenty models in the XNA sample collection have them. XNA's own <c>Name</c> returns whatever
    /// the file contained.</summary>
    internal ModelBone(int index, string? name)
    {
        Index = index;
        // XNA's Name is declared non-nullable and returns whatever the file contained, which for
        // an unnamed bone or mesh is null. Keeping the property's XNA signature exact matters more
        // than the annotation, so the null is stored deliberately rather than papered over with an
        // empty string that no file contains.
        Name = name!;
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
