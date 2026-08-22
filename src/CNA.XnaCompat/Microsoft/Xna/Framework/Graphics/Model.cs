namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible model facade. The public object model is independent of the CNA
/// implementation hierarchy; native resources remain owned by the compat mesh parts.</summary>
public sealed class Model
{
    private Matrix[] _sharedDrawBoneMatrices = [];
    private readonly IDisposable _ownedResources;

    internal Model(
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

        ModelBone[] boneArray = [.. bones];
        ModelMesh[] meshArray = [.. meshes];
        Bones = new ModelBoneCollection(boneArray);
        Meshes = new ModelMeshCollection(meshArray);

        if (boneArray.Length > 0)
        {
            if (rootBoneIndex < 0 || rootBoneIndex >= boneArray.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(rootBoneIndex));
            }

            Root = boneArray[rootBoneIndex];
        }

        if (meshParentBones.Count != 0)
        {
            if (meshParentBones.Count != meshArray.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(meshParentBones));
            }

            for (int i = 0; i < meshArray.Length; i++)
            {
                meshArray[i].SetParentBone(meshParentBones[i]);
            }
        }

        _ownedResources = new OwnedResourceLifetime(meshArray);
    }

    public ModelBone? Root { get; }

    public ModelBoneCollection Bones { get; }

    public object? Tag { get; set; }

    public ModelMeshCollection Meshes { get; }

    internal IDisposable OwnedResources => _ownedResources;

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

    public void Draw(Matrix world, Matrix view, Matrix projection)
    {
        if (_sharedDrawBoneMatrices.Length < Bones.Count)
        {
            _sharedDrawBoneMatrices = new Matrix[Bones.Count];
        }

        CopyAbsoluteBoneTransformsTo(_sharedDrawBoneMatrices);
        foreach (ModelMesh mesh in Meshes)
        {
            int boneIndex = mesh.ParentBone?.Index ?? Root?.Index ?? 0;
            foreach (Effect effect in mesh.Effects)
            {
                if (effect is not IEffectMatrices effectMatrices)
                {
                    throw new InvalidOperationException(
                        $"{effect.GetType().Name} does not implement {nameof(IEffectMatrices)}.");
                }

                effectMatrices.World = _sharedDrawBoneMatrices[boneIndex] * world;
                effectMatrices.View = view;
                effectMatrices.Projection = projection;
            }

            mesh.Draw();
        }
    }

    private sealed class OwnedResourceLifetime : IDisposable
    {
        private ModelMesh[]? _meshes;

        internal OwnedResourceLifetime(ModelMesh[] meshes)
        {
            _meshes = meshes;
        }

        public void Dispose()
        {
            ModelMesh[]? meshes = Interlocked.Exchange(ref _meshes, null);
            if (meshes is null)
            {
                return;
            }

            foreach (ModelMesh mesh in meshes)
            {
                mesh.DisposeOwnedResources();
            }
        }
    }
}
