namespace Microsoft.Xna.Framework.Graphics;

/// <summary>Represents a mesh that is part of a model.</summary>
public sealed class ModelMesh
{
    private ModelBone? _parentBone;
    private BoundingSphere _boundingSphere;

    internal ModelMesh(GraphicsDevice graphicsDevice, IReadOnlyList<ModelMeshPart> parts)
        : this(graphicsDevice, string.Empty, parts)
    {
    }

    internal ModelMesh(GraphicsDevice graphicsDevice, string name, IReadOnlyList<ModelMeshPart> parts)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(parts);

        Name = name;
        ModelMeshPart[] partArray = [.. parts];
        foreach (ModelMeshPart part in partArray)
        {
            part.SetParent(this);
        }

        MeshParts = new ModelMeshPartCollection(partArray);
        Effects = new ModelEffectCollection();
    }

    public string Name { get; }

    public ModelBone? ParentBone => _parentBone;

    public BoundingSphere BoundingSphere => _boundingSphere;

    public object? Tag { get; set; }

    public ModelMeshPartCollection MeshParts { get; }

    public ModelEffectCollection Effects { get; }

    internal void SetParentBone(ModelBone? value) => _parentBone = value;

    internal void SetBoundingSphere(BoundingSphere value) => _boundingSphere = value;

    public void Draw()
    {
        foreach (ModelMeshPart part in MeshParts)
        {
            Effect effect = part.Effect ?? throw new InvalidOperationException(
                $"Model mesh '{Name}' contains a part with no effect.");

            using CNA.Graphics.EffectTechnique technique = effect.Inner.CurrentTechnique;
            using CNA.Graphics.EffectPassCollection passes = technique.Passes;
            foreach (CNA.Graphics.EffectPass pass in passes)
            {
                using (pass)
                {
                    pass.Apply();
                    part.Draw();
                }
            }
        }
    }

    internal void DisposeOwnedResources()
    {
        foreach (ModelMeshPart part in MeshParts)
        {
            part.DisposeOwnedResources();
        }
    }
}
