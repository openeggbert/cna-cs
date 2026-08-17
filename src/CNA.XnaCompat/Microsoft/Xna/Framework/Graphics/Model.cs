namespace Microsoft.Xna.Framework.Graphics;

/// <summary>
/// XNA 4.0-compatible <c>Model</c>. Extends <c>CNA.Graphics.Model</c> directly. <see cref="Bones"/>/
/// <see cref="Meshes"/> are independently-tracked, compat-typed collections built directly from
/// this class's own constructor parameters (<see cref="ModelBoneCollection"/>/<see cref="ModelMeshCollection"/>
/// are independent reimplementations, not subclasses -- see their own doc comments), not downcasts
/// of the base class's own <c>Bones</c>/<c>Meshes</c> (which stay base-typed forever, built inside
/// the base constructor with no override seam). <see cref="Root"/> *is* a safe downcast though --
/// unlike <c>Bones</c>/<c>Meshes</c>'s container-type problem, <c>Root</c> is just a reference into
/// the same <c>bones</c> constructor parameter, which this class's own constructor already requires
/// to be compat-typed, so whatever the base constructor stores there is provably compat-typed too
/// (the same "single construction seam, provably compat-typed" reasoning
/// <c>SpriteFont.Texture</c>/<c>MediaLibrary.MediaSource</c> already established this session).
///
/// <c>Draw(Matrix,Matrix,Matrix)</c> is inherited unchanged -- its three scalar
/// <c>Matrix</c> parameters resolve correctly through that struct's own implicit conversion
/// operators, same as every other inherited-unchanged scalar-parameter member across this compat
/// layer. <c>CopyAbsoluteBoneTransformsTo</c>/<c>CopyBoneTransformsFrom</c>/<c>CopyBoneTransformsTo</c>
/// still needed real overloads (not `new`-hiding -- their parameter, once resolved in this
/// namespace, is a genuinely different type from the base method's, so nothing is actually hidden,
/// just overloaded) despite taking "just a <c>Matrix</c>" too, because their parameter is an
/// <em>array</em> (<c>Matrix[]</c>) -- C#'s implicit conversion operators only apply to single
/// values, never element-wise across an array, so a caller's real
/// <c>Microsoft.Xna.Framework.Matrix[]</c> buffer (the realistic, common shape real XNA game code
/// uses for this exact API) would not have bound to the base method's <c>CNA.Matrix[]</c>-typed
/// overload at all without one that does the element-wise conversion itself -- the same
/// "element-wise conversion, not a collection-level one" pattern <c>ContentManager</c>'s own
/// <c>SpriteFontData</c> conversion helpers already use in this compat layer.
///
/// <see cref="Meshes"/>'s own <c>MeshParts</c>/<c>Effects</c> stay base-typed -- see
/// <see cref="ModelMesh"/>'s own doc comment for that documented, narrow gap.
/// </summary>
public sealed class Model : CNA.Graphics.Model
{
    // Code-review findings: a per-call `new CNA.Matrix[destinationBoneTransforms.Length]` buffer
    // (1) allocated on every call -- these three methods are a standard per-frame skeletal-
    // animation idiom, so that's avoidable GC pressure the base class's own Draw() already avoids
    // via its own cached _sharedDrawBoneMatrices -- and (2), worse, sized to the *caller's* array
    // length rather than Bones.Count: base.Copy*() only ever writes indices [0, Bones.Count), so a
    // caller-supplied array longer than Bones.Count (a real, reasonable shape -- e.g. a reused
    // scratch buffer sized for the largest model in a batch) had its untouched, zeroed-out tail
    // copied back over whatever the caller's array already held there, silently clobbering data
    // real XNA (and the base method called directly) leaves alone. A single Bones.Count-sized,
    // lazily-grown shared buffer fixes both: it can never hold more than Bones.Count meaningful
    // entries to begin with, and it's only (re)allocated once per instance.
    private CNA.Matrix[] _sharedConversionBuffer = [];

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
        : base(graphicsDevice, bones, meshes, meshParentBones, rootBoneIndex)
    {
        Bones = new ModelBoneCollection(new List<ModelBone>(bones));
        Meshes = new ModelMeshCollection(new List<ModelMesh>(meshes));
    }

    public new ModelBoneCollection Bones { get; }

    public new ModelMeshCollection Meshes { get; }

    public new ModelBone? Root => (ModelBone?)base.Root;

    public void CopyAbsoluteBoneTransformsTo(Matrix[] destinationBoneTransforms)
    {
        ArgumentNullException.ThrowIfNull(destinationBoneTransforms);
        if (destinationBoneTransforms.Length < Bones.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationBoneTransforms));
        }

        CNA.Matrix[] buffer = GetSharedConversionBuffer();
        base.CopyAbsoluteBoneTransformsTo(buffer);
        for (int i = 0; i < Bones.Count; i++)
        {
            destinationBoneTransforms[i] = buffer[i];
        }
    }

    public void CopyBoneTransformsFrom(Matrix[] sourceBoneTransforms)
    {
        ArgumentNullException.ThrowIfNull(sourceBoneTransforms);
        if (sourceBoneTransforms.Length < Bones.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceBoneTransforms));
        }

        CNA.Matrix[] buffer = GetSharedConversionBuffer();
        for (int i = 0; i < Bones.Count; i++)
        {
            buffer[i] = sourceBoneTransforms[i];
        }

        base.CopyBoneTransformsFrom(buffer);
    }

    public void CopyBoneTransformsTo(Matrix[] destinationBoneTransforms)
    {
        ArgumentNullException.ThrowIfNull(destinationBoneTransforms);
        if (destinationBoneTransforms.Length < Bones.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationBoneTransforms));
        }

        CNA.Matrix[] buffer = GetSharedConversionBuffer();
        base.CopyBoneTransformsTo(buffer);
        for (int i = 0; i < Bones.Count; i++)
        {
            destinationBoneTransforms[i] = buffer[i];
        }
    }

    private CNA.Matrix[] GetSharedConversionBuffer()
    {
        if (_sharedConversionBuffer.Length < Bones.Count)
        {
            _sharedConversionBuffer = new CNA.Matrix[Bones.Count];
        }

        return _sharedConversionBuffer;
    }
}
