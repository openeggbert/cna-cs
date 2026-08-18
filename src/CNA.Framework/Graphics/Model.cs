namespace CNA.Graphics;

/// <summary>
/// A 3D model composed of bones and meshes. Real XNA populates this exclusively via
/// <c>Content.Load&lt;Model&gt;()</c> (the content pipeline's binary format) -- this project now has
/// a real, from-scratch <c>.xnb</c> reader for this (<c>CNA.Content.Xnb</c>, driven by
/// <c>ContentManager.Load&lt;Model&gt;()</c>; see that type's own doc comment), but these
/// constructors remain the only way to *hand-build* a <see cref="Model"/> directly -- both are
/// still real, useful API surface (the C++ engine's own equivalents are marked <c>CNAEXT</c> for
/// the same reason: real XNA's own <c>Model</c> constructor is content-pipeline-only, but there is
/// real value in constructing one directly for tests/tools/procedural content, matching the C++
/// engine's own choice to expose them). Deliberately does not reproduce the C++ header's
/// parameterless <c>Model() = default;</c> -- an unpopulated model has no realistic use here
/// either way, so adding it would be speculative API surface, not a real gap.
/// </summary>
public class Model : IDisposable
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

    /// <summary>
    /// Writes each bone's transform relative to the model's root into <paramref name="destinationBoneTransforms"/>,
    /// composing each bone's own <see cref="ModelBone.Transform"/> with its already-computed parent
    /// entry. This relies on the same invariant the real openeggbert/cna C++ engine's own
    /// <c>Model::CopyAbsoluteBoneTransformsTo</c> relies on (and does not validate either): each
    /// bone's <see cref="ModelBone.Index"/> must equal its position in <see cref="Bones"/>, and a
    /// parent bone must appear at an earlier position than its children -- both guaranteed by a
    /// real content-pipeline's output, but this project has no content pipeline (see <see cref="Model"/>'s
    /// own doc comment), so hand-built bone lists are checked explicitly here instead of silently
    /// producing a wrong (default/zero) matrix the way the unchecked C++ algorithm would for a
    /// malformed list.
    /// </summary>
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
            if (bone.Index != i)
            {
                throw new InvalidOperationException(
                    $"Bone '{bone.Name}' has Index {bone.Index}, but is at position {i} in Bones. " +
                    "Each bone's Index must match its position in Bones.");
            }

            if (bone.Parent is null)
            {
                destinationBoneTransforms[i] = bone.Transform;
                continue;
            }

            if (bone.Parent.Index < 0 || bone.Parent.Index >= i)
            {
                throw new InvalidOperationException(
                    $"Bone '{bone.Name}' (index {i}) has parent '{bone.Parent.Name}' (index {bone.Parent.Index}), " +
                    "which must appear at an earlier position in Bones.");
            }

            destinationBoneTransforms[i] = bone.Transform * destinationBoneTransforms[bone.Parent.Index];
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

                // Falls back to Root, not a hardcoded 0, when a mesh has no explicit ParentBone --
                // a deliberate improvement over the real C++ engine's own literal ": 0" fallback
                // (confirmed in its Model.cpp): that only coincides with the actual root bone when
                // rootBoneIndex is 0, which the 3-argument/default-rootBoneIndex constructors always
                // produce, but a caller using a non-zero rootBoneIndex together with an empty
                // meshParentBones list (a fully valid, publicly reachable combination -- see the
                // 5-argument constructor) would otherwise silently draw every parentless mesh
                // relative to the wrong bone. Root itself falls back to 0 for a model with bones,
                // matching real XNA's own "first bone is root by default" convention.
                int boneIndex = mesh.ParentBone?.Index ?? Root?.Index ?? 0;
                if (boneIndex < 0 || boneIndex >= _sharedDrawBoneMatrices.Length)
                {
                    throw new InvalidOperationException(
                        $"Mesh '{mesh.Name}' references a parent bone index ({boneIndex}) that is out " +
                        $"of range for this model's {Bones.Count} bone(s).");
                }

                effectMatrices.World = _sharedDrawBoneMatrices[boneIndex] * world;
                effectMatrices.View = view;
                effectMatrices.Projection = projection;
            }

            mesh.Draw();
        }
    }

    /// <summary>Disposes every mesh, and through them every part's builder-created buffers and
    /// effect. Real XNA has no <c>Model.Dispose</c>; this exists because those are native handles
    /// with no device to reclaim them -- see <see cref="ModelMeshPart.Dispose"/>.</summary>
    public void Dispose()
    {
        foreach (ModelMesh mesh in Meshes)
        {
            mesh.Dispose();
        }

        GC.SuppressFinalize(this);
    }

}
