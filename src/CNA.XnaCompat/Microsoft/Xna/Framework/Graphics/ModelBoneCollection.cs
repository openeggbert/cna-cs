namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>ModelBoneCollection</c>. Independent reimplementation, not a
/// subclass of <c>CNA.Graphics.ModelBoneCollection</c> -- same reasoning as <c>SongCollection</c>'s
/// own doc comment in the media compat layer (extending directly would inherit an indexer typed to
/// <c>CNA.Graphics.ModelBone</c>, not this namespace's own). Shares its indexer/lookup
/// implementation with <see cref="ModelMeshCollection"/> via <see cref="NamedModelCollection{T}"/>
/// -- see that type's own doc comment.</summary>
public sealed class ModelBoneCollection : NamedModelCollection<ModelBone>
{
    internal ModelBoneCollection(List<ModelBone> bones)
        : base(bones, bone => bone.Name, "bone")
    {
    }
}
