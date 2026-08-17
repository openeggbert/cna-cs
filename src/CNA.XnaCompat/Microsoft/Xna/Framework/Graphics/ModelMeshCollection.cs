namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>ModelMeshCollection</c>. Independent reimplementation, not a
/// subclass of <c>CNA.Graphics.ModelMeshCollection</c> -- same reasoning as
/// <see cref="ModelBoneCollection"/>'s own doc comment. Shares its indexer/lookup implementation
/// with <see cref="ModelBoneCollection"/> via <see cref="NamedModelCollection{T}"/>.</summary>
public sealed class ModelMeshCollection : NamedModelCollection<ModelMesh>
{
    internal ModelMeshCollection(List<ModelMesh> meshes)
        : base(meshes, mesh => mesh.Name, "mesh")
    {
    }
}
