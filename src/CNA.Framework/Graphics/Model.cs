namespace CNA.Graphics;

/// <summary>
/// A 3D model composed of bones and meshes. Real XNA populates this exclusively via
/// <c>Content.Load&lt;Model&gt;()</c> (the content pipeline's binary format) -- this project has no
/// model-file loader yet (parsing a real model format is a separate, much larger problem than
/// anything else built so far this session), so these constructors are the only way to obtain a
/// <see cref="Model"/> here. They match the real openeggbert/cna C++ engine's own <c>Model</c>
/// constructors exactly (both marked <c>CNAEXT</c> there for the same reason: real XNA's own
/// <c>Model</c> constructor is content-pipeline-only). Deliberately does not reproduce the C++
/// header's parameterless <c>Model() = default;</c> -- an unpopulated model has no realistic use
/// in this project (there is no loader that would ever construct one and fill it in afterward),
/// so adding it would be speculative API surface, not a real gap.
/// </summary>
public class Model
{
    private Matrix[] _sharedDrawBoneMatrices = [];

    public Model(GraphicsDevice graphicsDevice, IReadOnlyList<ModelBone> bones, IReadOnlyList<ModelMesh> meshes)
        : this(graphicsDevice, bones, meshes, [], 0)
    {
    }

    public Model(
        GraphicsDevice graphicsDevice,
        IReadOnlyList<ModelBone> bones,
        IReadOnlyList<ModelMesh> meshes,
        IReadOnlyList<ModelBone> meshParentBones,
        int rootBoneIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(bones);
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentNullException.ThrowIfNull(meshParentBones);

        var boneList = new List<ModelBone>(bones);
        var meshList = new List<ModelMesh>(meshes);
        Bones = new ModelBoneCollection(boneList);
        Meshes = new ModelMeshCollection(meshList);

        // Matches the 3-argument constructor's own leniency: an empty bones list leaves Root null
        // regardless of rootBoneIndex, rather than throwing on the default value 0.
        if (boneList.Count > 0)
        {
            if (rootBoneIndex < 0 || rootBoneIndex >= boneList.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(rootBoneIndex));
            }

            Root = boneList[rootBoneIndex];
        }

        if (meshParentBones.Count == 0)
        {
            return;
        }

        if (meshParentBones.Count != meshList.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(meshParentBones));
        }

        for (int i = 0; i < meshParentBones.Count; i++)
        {
            meshList[i].ParentBone = meshParentBones[i];
        }
    }

    public ModelBoneCollection Bones { get; }

    public ModelMeshCollection Meshes { get; }

    public ModelBone? Root { get; }

    public object? Tag { get; set; }

    public void CopyAbsoluteBoneTransformsTo(Matrix[] destinationBoneTransforms)
    {
        ArgumentNullException.ThrowIfNull(destinationBoneTransforms);
        if (destinationBoneTransforms.Length < Bones.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationBoneTransforms));
        }

        for (int i = 0; i < Bones.Count; i++)
        {
            ModelBone bone = Bones[i];
            destinationBoneTransforms[i] = bone.Parent is null
                ? bone.Transform
                : bone.Transform * destinationBoneTransforms[bone.Parent.Index];
        }
    }

    public void CopyBoneTransformsFrom(Matrix[] sourceBoneTransforms)
    {
        ArgumentNullException.ThrowIfNull(sourceBoneTransforms);
        if (sourceBoneTransforms.Length < Bones.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceBoneTransforms));
        }

        for (int i = 0; i < Bones.Count; i++)
        {
            Bones[i].Transform = sourceBoneTransforms[i];
        }
    }

    public void CopyBoneTransformsTo(Matrix[] destinationBoneTransforms)
    {
        ArgumentNullException.ThrowIfNull(destinationBoneTransforms);
        if (destinationBoneTransforms.Length < Bones.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationBoneTransforms));
        }

        for (int i = 0; i < Bones.Count; i++)
        {
            destinationBoneTransforms[i] = Bones[i].Transform;
        }
    }

    /// <summary>
    /// Reproduces the real openeggbert/cna C++ engine's own <c>Model::Draw</c> algorithm exactly:
    /// compute every bone's absolute (world-relative) transform once into a reused buffer, then for
    /// each mesh, push <c>boneTransform * world</c>/<paramref name="view"/>/<paramref name="projection"/>
    /// into every effect the mesh's parts use (via <see cref="IEffectMatrices"/>, since this project
    /// has no generic effect-parameter system -- see <see cref="ModelMeshPart.Effect"/>'s own doc
    /// comment) before drawing the mesh. Throws if a mesh uses an effect that doesn't implement
    /// <see cref="IEffectMatrices"/>, matching the real engine's own behavior there.
    /// </summary>
    public void Draw(Matrix world, Matrix view, Matrix projection)
    {
        if (_sharedDrawBoneMatrices.Length < Bones.Count)
        {
            _sharedDrawBoneMatrices = new Matrix[Bones.Count];
        }

        CopyAbsoluteBoneTransformsTo(_sharedDrawBoneMatrices);

        foreach (ModelMesh mesh in Meshes)
        {
            foreach (Effect effect in mesh.Effects)
            {
                if (effect is not IEffectMatrices effectMatrices)
                {
                    throw new InvalidOperationException(
                        $"{effect.GetType().Name} does not implement {nameof(IEffectMatrices)}.");
                }

                int boneIndex = mesh.ParentBone?.Index ?? 0;
                effectMatrices.World = _sharedDrawBoneMatrices[boneIndex] * world;
                effectMatrices.View = view;
                effectMatrices.Projection = projection;
            }

            mesh.Draw();
        }
    }
}
