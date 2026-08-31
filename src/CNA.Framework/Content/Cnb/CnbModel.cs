using System.Text;
using CNA.Graphics;
using CNA.Interop;

namespace CNA.Content.Cnb;

/// <summary>
/// Which effect a compiled CNB model part draws with.
///
/// CNA's own vocabulary, not XNA's. Three of these -- <see cref="Pbr"/>, <see cref="SkinnedPbr"/>
/// and <see cref="External"/> -- name effects XNA 4.0 has no equivalent for at all, which is why
/// this enum is here rather than projected onto anything in <c>Microsoft.Xna.Framework</c>.
/// </summary>
public enum CnbEffectKind
{
    Basic = 0,
    Skinned = 1,
    DualTexture = 2,
    Pbr = 3,
    SkinnedPbr = 4,

    /// <summary>The part draws with an effect named by <see cref="CnbModelPart.ExternalEffect"/>.
    /// The only kind for which that name means anything.</summary>
    External = 5,
}

/// <summary>
/// One of the eight named texture slots a CNB material can fill.
///
/// glTF's set rather than XNA's: <c>BasicEffect</c> has one texture and <c>DualTextureEffect</c>
/// two, while <see cref="MetallicRoughness"/>, <see cref="Occlusion"/> and the two specular slots
/// exist only in a PBR material.
/// </summary>
public enum CnbMaterialTextureSlot
{
    BaseColor = 0,
    Second = 1,
    Normal = 2,
    MetallicRoughness = 3,
    Emissive = 4,
    Occlusion = 5,
    Specular = 6,
    SpecularColor = 7,
}

/// <summary>How a material's alpha is interpreted -- glTF's <c>alphaMode</c>.</summary>
public enum CnbAlphaMode
{
    Opaque = 0,
    Mask = 1,
    Blend = 2,
}

/// <summary>
/// One importer slot's UV transform, as <c>KHR_texture_transform</c> stores it.
///
/// There is no "was one declared" flag, unlike <see cref="CnbSamplerState.IsDeclared"/>. That is the
/// ABI's shape, not an omission here: <c>cna_cnb_model_get_material_texture_transform</c> takes four
/// arguments and returns only the five floats, so an identity transform and an absent one are
/// indistinguishable through this route. Worth knowing before round-tripping a model.
/// </summary>
public readonly struct CnbTextureTransform
{
    internal CnbTextureTransform(CnaCnbTextureTransform native)
    {
        Offset = new Vector2(native.OffsetX, native.OffsetY);
        Scale = new Vector2(native.ScaleX, native.ScaleY);
        Rotation = native.Rotation;
    }

    public Vector2 Offset { get; }

    public Vector2 Scale { get; }

    /// <summary>Rotation in radians, counter-clockwise about the UV origin.</summary>
    public float Rotation { get; }
}

/// <summary>One texture slot's sampler state.</summary>
public readonly struct CnbSamplerState
{
    internal CnbSamplerState(CnaCnbSamplerState native)
    {
        Filter = (TextureFilter)native.Filter;
        AddressU = (TextureAddressMode)native.AddressU;
        AddressV = (TextureAddressMode)native.AddressV;
        IsDeclared = native.Declared != 0;
    }

    public TextureFilter Filter { get; }

    public TextureAddressMode AddressU { get; }

    public TextureAddressMode AddressV { get; }

    /// <summary>
    /// Whether the file declared a sampler for this slot.
    ///
    /// A zeroed sampler reads as "point, clamp, clamp", which is also a perfectly ordinary authored
    /// choice, so the two are not separable from the values alone. <see cref="CnbTextureTransform"/>
    /// carries no such flag -- not an omission here, but the shape of the route that reads it.
    /// </summary>
    public bool IsDeclared { get; }
}

/// <summary>
/// The per-slot sampling state CNB keeps in the <b>importer's</b> seven-element arrays.
///
/// Separate from <see cref="CnbMaterialTexture"/> because it is addressed by a different index in a
/// different order, and because one name slot has no entry at all -- see
/// <see cref="CnbMaterialTexture.ImporterState"/>.
/// </summary>
public readonly struct CnbImporterSlotState
{
    internal CnbImporterSlotState(int coordinateSet, CnbTextureTransform transform, CnbSamplerState sampler)
    {
        CoordinateSet = coordinateSet;
        Transform = transform;
        Sampler = sampler;
    }

    /// <summary>The <c>TEXCOORD_n</c> index this slot samples.</summary>
    public int CoordinateSet { get; }

    public CnbTextureTransform Transform { get; }

    public CnbSamplerState Sampler { get; }
}

/// <summary>
/// One filled texture slot of a <see cref="CnbMaterial"/>.
///
/// The asset name is logical -- what a <c>ContentManager</c> would be asked for -- not a path, and
/// this type deliberately does not resolve it. Materialising it needs a device and a content root,
/// which are the caller's, and a slot that names an asset the caller does not have is a fact about
/// the model rather than an error in it.
/// </summary>
public readonly struct CnbMaterialTexture
{
    internal CnbMaterialTexture(
        CnbMaterialTextureSlot slot,
        string assetName,
        CnbImporterSlotState? importerState)
    {
        Slot = slot;
        AssetName = assetName;
        ImporterState = importerState;
    }

    public CnbMaterialTextureSlot Slot { get; }

    /// <summary>The logical asset name; never empty, because an unused slot is not surfaced at
    /// all.</summary>
    public string AssetName { get; }

    /// <summary>
    /// The coordinate set, transform and sampler for this slot, or <see langword="null"/> for
    /// <see cref="CnbMaterialTextureSlot.Second"/>.
    ///
    /// <b>Null is the honest answer, not a gap.</b> CNB keeps per-slot state in the importer's own
    /// seven-element arrays, and <see cref="CnbMaterialTextureSlot.Second"/> --
    /// <c>DualTextureEffect</c>'s second layer -- is CNA's own effect slot with no glTF counterpart,
    /// so the importer has no entry for it. Reporting some other slot's sampler here would be worse
    /// than reporting none.
    /// </summary>
    public CnbImporterSlotState? ImporterState { get; }
}

/// <summary>
/// Crossing between CNB's two texture slot spaces.
///
/// <b>They are not the same eight slots, and the header calls the difference "a real trap".</b>
/// Texture <em>names</em> are addressed by <see cref="CnbMaterialTextureSlot"/> -- eight of them,
/// CNA's own effect slots, including <c>DualTextureEffect</c>'s second layer. Coordinate sets,
/// transforms and samplers live in the importer's seven-element arrays in glTF's order: base
/// colour, normal, metallic-roughness, occlusion, emissive, specular, specular colour.
///
/// So the two spaces differ in length <em>and</em> in order: emissive and occlusion are swapped
/// between them. Passing a name slot straight into a per-slot route silently returns the wrong
/// slot's sampler, which is exactly the mistake this map exists to make impossible -- and exactly
/// the mistake the first version of this file made.
/// </summary>
internal static class CnbMaterialTextureSlotMap
{
    /// <summary>The importer slot for a name slot, or <see langword="null"/> when the importer has
    /// none.</summary>
    public static ulong? ImporterSlot(CnbMaterialTextureSlot slot) => slot switch
    {
        CnbMaterialTextureSlot.BaseColor => 0,
        CnbMaterialTextureSlot.Second => null,
        CnbMaterialTextureSlot.Normal => 1,
        CnbMaterialTextureSlot.MetallicRoughness => 2,
        CnbMaterialTextureSlot.Occlusion => 3,
        CnbMaterialTextureSlot.Emissive => 4,
        CnbMaterialTextureSlot.Specular => 5,
        CnbMaterialTextureSlot.SpecularColor => 6,
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Not a CNB material texture slot."),
    };
}

/// <summary>
/// One part's material: the numeric state plus the texture slots the file actually filled.
///
/// A snapshot, like every other type here -- reading it after the <see cref="CnbModel"/> is disposed
/// answers what the file said rather than touching freed memory.
/// </summary>
public sealed class CnbMaterial
{
    internal CnbMaterial(CnaCnbMaterialInfo info, IReadOnlyList<CnbMaterialTexture> textures)
    {
        BaseColorFactor = new Vector4(
            info.BaseColorFactor.X, info.BaseColorFactor.Y, info.BaseColorFactor.Z, info.BaseColorFactor.W);
        EmissiveFactor = new Vector3(info.EmissiveFactor.X, info.EmissiveFactor.Y, info.EmissiveFactor.Z);
        SpecularColorFactor = new Vector3(
            info.SpecularColorFactor.X, info.SpecularColorFactor.Y, info.SpecularColorFactor.Z);
        MetallicFactor = info.MetallicFactor;
        RoughnessFactor = info.RoughnessFactor;
        IndexOfRefraction = info.Ior;
        SpecularFactor = info.SpecularFactor;
        NormalScale = info.NormalScale;
        OcclusionStrength = info.OcclusionStrength;
        AlphaCutoff = info.AlphaCutoff;
        AlphaMode = (CnbAlphaMode)info.AlphaMode;
        DoubleSided = info.DoubleSided != 0;
        Textures = textures;
    }

    public Vector4 BaseColorFactor { get; }

    public Vector3 EmissiveFactor { get; }

    public Vector3 SpecularColorFactor { get; }

    public float MetallicFactor { get; }

    public float RoughnessFactor { get; }

    public float IndexOfRefraction { get; }

    public float SpecularFactor { get; }

    public float NormalScale { get; }

    public float OcclusionStrength { get; }

    /// <summary>Meaningful only when <see cref="AlphaMode"/> is <see cref="CnbAlphaMode.Mask"/>.
    /// </summary>
    public float AlphaCutoff { get; }

    public CnbAlphaMode AlphaMode { get; }

    public bool DoubleSided { get; }

    /// <summary>Only the slots the file filled, in slot order. An unused slot is absent rather than
    /// present with an empty name, so a caller can iterate this and get exactly the textures the
    /// model asks for.</summary>
    public IReadOnlyList<CnbMaterialTexture> Textures { get; }

    /// <summary>The texture in one slot, or <see langword="null"/> when the slot is unused.</summary>
    public CnbMaterialTexture? Texture(CnbMaterialTextureSlot slot)
    {
        foreach (CnbMaterialTexture texture in Textures)
        {
            if (texture.Slot == slot)
            {
                return texture;
            }
        }

        return null;
    }
}

/// <summary>
/// One renderable part of a <see cref="CnbModel"/>: geometry plus the material and effect it draws
/// with.
///
/// The vertex and index bytes are the compiled buffers, copied out. They are bytes rather than a
/// typed array because CNB stores the stride and not a vertex declaration -- the layout is the
/// effect's business, and inventing a <c>VertexDeclaration</c> here would be this binding guessing
/// at something the file does not say.
/// </summary>
public sealed class CnbModelPart
{
    internal CnbModelPart(
        int index,
        string name,
        CnaCnbModelPartInfo info,
        string externalEffect,
        byte[] vertexBytes,
        byte[] indexBytes,
        CnbMaterial material)
    {
        Index = index;
        Name = name;
        VertexStride = (int)info.VertexStride;
        VertexCount = (int)info.VertexCount;
        IndexCount = (int)info.IndexCount;
        IndexElementSize = (int)info.IndexElementSize;
        PrimitiveTopology = (int)info.PrimitiveTopology;
        PrimitiveCount = (int)info.PrimitiveCount;
        EffectKind = (CnbEffectKind)info.EffectKind;
        VertexColorEnabled = info.VertexColorEnabled != 0;
        Unlit = info.Unlit != 0;
        ExternalEffect = externalEffect;
        VertexBytes = vertexBytes;
        IndexBytes = indexBytes;
        Material = material;
    }

    /// <summary>This part's index in <see cref="CnbModel.Parts"/>, which is what a mesh's part list
    /// refers to.</summary>
    public int Index { get; }

    /// <summary>The part's name, possibly empty -- CNB permits an unnamed part, and inventing a
    /// name here would let a caller comparing names match the wrong part.</summary>
    public string Name { get; }

    public int VertexStride { get; }

    public int VertexCount { get; }

    public int IndexCount { get; }

    /// <summary>Bytes per index: 2 or 4. Stored rather than derived from the vertex count, which is
    /// what the header says and what makes a truncated sidecar an error instead of a shorter
    /// mesh.</summary>
    public int IndexElementSize { get; }

    /// <summary>The topology as its numeric value; 4 is triangles, which is also glTF's default.
    /// Left numeric because that is exactly what CNB stores -- naming an enum here would claim a
    /// closed set the format does not promise.</summary>
    public int PrimitiveTopology { get; }

    public int PrimitiveCount { get; }

    public CnbEffectKind EffectKind { get; }

    public bool VertexColorEnabled { get; }

    /// <summary>Whether the material is <c>KHR_materials_unlit</c>.</summary>
    public bool Unlit { get; }

    /// <summary>The effect asset this part names, or empty. Non-empty only when
    /// <see cref="EffectKind"/> is <see cref="CnbEffectKind.External"/>: the header says the field
    /// is unused otherwise, so reporting it regardless would invent a dependency the file does not
    /// declare.</summary>
    public string ExternalEffect { get; }

    /// <summary>The compiled vertex buffer. <c>VertexStride * VertexCount</c> bytes.</summary>
    public byte[] VertexBytes { get; }

    /// <summary>The compiled index buffer. <c>IndexElementSize * IndexCount</c> bytes.</summary>
    public byte[] IndexBytes { get; }

    public CnbMaterial Material { get; }
}

/// <summary>One bone of the model's scene graph.</summary>
public sealed class CnbModelBone
{
    internal CnbModelBone(int index, string name, int parentIndex, Matrix transform)
    {
        Index = index;
        Name = name;
        ParentIndex = parentIndex;
        Transform = transform;
    }

    public int Index { get; }

    /// <summary>Possibly empty: CNB permits an unnamed bone, and this binding already learned once
    /// -- in the XNB model reader -- that substituting an empty name for a missing one lets a game
    /// comparing bone names match the wrong bone.</summary>
    public string Name { get; }

    /// <summary>The parent's index, or -1 for a root.</summary>
    public int ParentIndex { get; }

    /// <summary>The bone-local transform.</summary>
    public Matrix Transform { get; }

    /// <summary>The parent bone, or <see langword="null"/> for a root. Set once the whole graph is
    /// built, so a cycle in the file cannot make this recurse.</summary>
    public CnbModelBone? Parent { get; internal set; }

    /// <summary>The bones whose <see cref="ParentIndex"/> is this one, in index order.</summary>
    public IReadOnlyList<CnbModelBone> Children { get; internal set; } = [];
}

/// <summary>One mesh: a named group of parts, attached to a bone.</summary>
public sealed class CnbModelMesh
{
    internal CnbModelMesh(int index, string name, int parentBoneIndex, IReadOnlyList<CnbModelPart> parts)
    {
        Index = index;
        Name = name;
        ParentBoneIndex = parentBoneIndex;
        Parts = parts;
    }

    public int Index { get; }

    public string Name { get; }

    /// <summary>The bone this mesh hangs from, or -1 when it names none.</summary>
    public int ParentBoneIndex { get; }

    /// <summary>The bone this mesh hangs from, or <see langword="null"/>.</summary>
    public CnbModelBone? ParentBone { get; internal set; }

    /// <summary>This mesh's parts, <b>in draw order</b> -- which is the file's order, not the
    /// model's part order. A mesh may name the same part twice and may name them out of order, so
    /// this list is not a subset view of <see cref="CnbModel.Parts"/>.</summary>
    public IReadOnlyList<CnbModelPart> Parts { get; }
}

/// <summary>
/// A model's skinning skeleton: one parent index and up to three matrices per joint.
/// </summary>
public sealed class CnbSkeleton
{
    internal CnbSkeleton(
        IReadOnlyList<int> hierarchy,
        IReadOnlyList<Matrix> bindPose,
        IReadOnlyList<Matrix> inverseBindPose,
        IReadOnlyList<Matrix> rootPrefix)
    {
        Hierarchy = hierarchy;
        BindPose = bindPose;
        InverseBindPose = inverseBindPose;
        RootPrefix = rootPrefix;
    }

    /// <summary>Each joint's parent index, or -1 for a root.</summary>
    public IReadOnlyList<int> Hierarchy { get; }

    public IReadOnlyList<Matrix> BindPose { get; }

    public IReadOnlyList<Matrix> InverseBindPose { get; }

    /// <summary>The transform above the skeleton's root, or empty when the source carried none --
    /// which the header states is an ordinary outcome rather than a failure.</summary>
    public IReadOnlyList<Matrix> RootPrefix { get; }

    public int JointCount => Hierarchy.Count;
}

/// <summary>
/// A decoded CNA-native compiled model: CNB's own model graph, materialised.
///
/// <b>Not XNA, and deliberately not <c>Microsoft.Xna.Framework.Graphics.Model</c>.</b> The two are
/// different shapes and saying otherwise would be a lie in both directions. CNB carries PBR
/// materials, eight texture slots, glTF alpha modes, morph targets and a skinning skeleton, none of
/// which XNA's <c>Model</c> can hold; XNA's <c>Model</c> carries live <c>VertexBuffer</c>,
/// <c>IndexBuffer</c> and <c>Effect</c> objects bound to a device, which a decoded file does not
/// have. Projecting one onto the other would silently drop half of each.
///
/// <b>This is a decoded document, not a drawable.</b> Everything here is data the file contains --
/// the compiled vertex and index bytes are handed over as bytes, and nothing has been uploaded to a
/// device. Turning it into something drawable needs a graphics device, a content root for the
/// texture names and a vertex layout, all of which are the caller's to supply.
///
/// <b>Ownership.</b> The decoded model handle is owned: this object created it through
/// <c>cna_cnb_decode_model</c> and destroys it. Every accessor on this graph reads a managed
/// snapshot taken during <see cref="Decode"/>, so nothing here holds a view into native memory and
/// no child can outlive storage it depends on. That also makes the graph safe to keep after the
/// <see cref="CnbDocument"/> it came from is disposed -- decoding copies rather than viewing, which
/// the header states and this type relies on.
/// </summary>
public sealed class CnbModel : IDisposable
{
    private readonly NativeResourceHandle _handle;

    private CnbModel(nint handleValue)
    {
        _handle = new NativeResourceHandle(
            handleValue,
            h => Native.cna_cnb_model_destroy(new CnaHandle(h)).IsSuccess());
    }

    /// <summary>The asset type identity a document must carry for <see cref="Decode"/> to accept
    /// it -- <c>CNA_CNB_ASSET_TYPE_MODEL</c>.</summary>
    public const uint ModelAssetTypeId = 5;

    /// <summary>
    /// Decodes the model a document holds.
    ///
    /// The document is borrowed for the duration of the call and is not retained: the returned model
    /// stays valid after the document is disposed.
    /// </summary>
    /// <exception cref="CnaException">The document is not a model, or its chunks disagree.</exception>
    public static CnbModel Decode(CnbDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        CnaResult result = Native.cna_cnb_decode_model(document.NativeHandle, out CnaHandle model);
        CnaException.ThrowIfFailed(result, nameof(Decode));
        GC.KeepAlive(document);

        var decoded = new CnbModel(model.AsNint);
        try
        {
            decoded.Read();
            return decoded;
        }
        catch
        {
            decoded.Dispose();
            throw;
        }
    }

    /// <summary>Opens a <c>.cnb</c> file and decodes the model in it.</summary>
    public static CnbModel DecodeFile(string path)
    {
        using CnbDocument document = CnbDocument.Open(path);
        return Decode(document);
    }

    /// <summary>The scene graph's bones, in file order. Empty for a model with none.</summary>
    public IReadOnlyList<CnbModelBone> Bones { get; private set; } = [];

    /// <summary>The bones with no parent, in index order.</summary>
    public IReadOnlyList<CnbModelBone> RootBones { get; private set; } = [];

    /// <summary>Every renderable part, in file order. A mesh refers to these by index.</summary>
    public IReadOnlyList<CnbModelPart> Parts { get; private set; } = [];

    public IReadOnlyList<CnbModelMesh> Meshes { get; private set; } = [];

    /// <summary>The skinning skeleton, or <see langword="null"/> when the model has none.</summary>
    public CnbSkeleton? Skeleton { get; private set; }

    /// <summary>How many embedded animation clips the model carries. The clips themselves are not
    /// in this slice; the count is here because it is what tells a caller whether to look.</summary>
    public int AnimationCount { get; private set; }

    /// <summary>How many punctual lights the model carries.</summary>
    public int LightCount { get; private set; }

    /// <summary>
    /// Whether the materials were authored under glTF's lighting conventions.
    ///
    /// Carried rather than inferred from <see cref="LightCount"/>, because the two mean different
    /// things: a glTF import expects the importer's "no light was declared, so light it by default"
    /// fallback, while a hand-authored model expects XNA's, where <c>BasicEffect</c> starts unlit.
    /// </summary>
    public bool AppliesGltfLightingPolicy { get; private set; }

    /// <summary>
    /// Whether the source document carried a real scene-node hierarchy.
    ///
    /// Equivalent to more than one bone, but stated by the file rather than inferred, and the
    /// distinction selects real behaviour: attach each mesh to its named bone, or give every mesh
    /// its own child of the root.
    /// </summary>
    public bool HasBoneHierarchy { get; private set; }

    public void Dispose() => _handle.Dispose();

    private CnaHandle Handle => new(_handle.DangerousGetHandle());

    /// <summary>
    /// Walks the whole native graph once and keeps managed snapshots.
    ///
    /// Eager rather than lazy on purpose: a lazy accessor would have to hold the native model alive
    /// behind every child object, which is exactly the "two owners for one native resource" shape
    /// the ownership rules forbid. One pass, then the native handle matters only for
    /// <see cref="Dispose"/>.
    /// </summary>
    private void Read()
    {
        var info = CnaCnbModelInfo.Versioned();
        CnaException.ThrowIfFailed(Native.cna_cnb_model_get_info(Handle, ref info), nameof(CnbModel));
        GC.KeepAlive(this);

        AnimationCount = checked((int)info.AnimationCount);
        LightCount = checked((int)info.LightCount);
        AppliesGltfLightingPolicy = info.AppliesGltfLightingPolicy != 0;
        HasBoneHierarchy = info.HasBoneHierarchy != 0;

        Bones = ReadBones(checked((int)info.BoneCount));
        RootBones = LinkBones(Bones);
        Parts = ReadParts(checked((int)info.PartCount));
        Meshes = ReadMeshes(checked((int)info.MeshCount), Bones, Parts);
        Skeleton = info.HasSkeleton != 0 ? ReadSkeleton() : null;
    }

    private unsafe CnbModelBone[] ReadBones(int count)
    {
        var bones = new CnbModelBone[count];
        for (int index = 0; index < count; index++)
        {
            var bone = CnaCnbModelBone.Versioned();
            CnaException.ThrowIfFailed(
                Native.cna_cnb_model_get_bone(Handle, (ulong)index, ref bone), nameof(Bones));
            GC.KeepAlive(this);

            string name = NativeStringReader.ReadIndexed(
                Native.cna_cnb_model_get_bone_name_size,
                Native.cna_cnb_model_copy_bone_name,
                Handle,
                (ulong)index,
                nameof(Bones));

            bones[index] = new CnbModelBone(index, name, bone.Parent, Matrix.FromNative(bone.Transform));
        }

        return bones;
    }

    /// <summary>
    /// Fills in each bone's parent and children, and answers the roots.
    ///
    /// A parent index outside the bone array is refused rather than clamped or ignored: it means the
    /// file's scene graph does not describe itself, and a silently dropped edge would produce a
    /// model that looks whole and animates wrongly.
    /// </summary>
    private static CnbModelBone[] LinkBones(IReadOnlyList<CnbModelBone> bones)
    {
        var children = new List<CnbModelBone>[bones.Count];
        var roots = new List<CnbModelBone>();

        foreach (CnbModelBone bone in bones)
        {
            if (bone.ParentIndex < 0)
            {
                roots.Add(bone);
                continue;
            }

            if (bone.ParentIndex >= bones.Count)
            {
                throw new CnaException(
                    $"CNB bone {bone.Index} names parent {bone.ParentIndex}, but the model has {bones.Count} bone(s).");
            }

            bone.Parent = bones[bone.ParentIndex];
            (children[bone.ParentIndex] ??= []).Add(bone);
        }

        for (int index = 0; index < bones.Count; index++)
        {
            if (children[index] is { } list)
            {
                bones[index].Children = list;
            }
        }

        return [.. roots];
    }

    private unsafe CnbModelPart[] ReadParts(int count)
    {
        var parts = new CnbModelPart[count];
        for (int index = 0; index < count; index++)
        {
            var info = CnaCnbModelPartInfo.Versioned();
            CnaException.ThrowIfFailed(
                Native.cna_cnb_model_get_part(Handle, (ulong)index, ref info), nameof(Parts));
            GC.KeepAlive(this);

            string name = NativeStringReader.ReadIndexed(
                Native.cna_cnb_model_get_part_name_size,
                Native.cna_cnb_model_copy_part_name,
                Handle,
                (ulong)index,
                nameof(Parts));

            // Only for an external effect -- two native round trips saved per part, not a rule
            // being enforced. Measured: CNA's own encoder drops the name for every other kind, so a
            // reader without this test answers the empty string too. The header calls the field
            // unused for those kinds and upstream means it; `DecodedModel_ReportsAnEffectNameOnly-
            // ForAnExternalEffect` authors a Basic part with a name and pins that.
            string externalEffect = info.EffectKind == CnaCnbEffectKind.External
                ? NativeStringReader.ReadIndexed(
                    Native.cna_cnb_model_get_part_external_effect_size,
                    Native.cna_cnb_model_copy_part_external_effect,
                    Handle,
                    (ulong)index,
                    nameof(Parts))
                : string.Empty;

            byte[] vertexBytes = ReadBytes(
                (ulong)index, Native.cna_cnb_model_copy_part_vertex_bytes, nameof(CnbModelPart.VertexBytes));
            byte[] indexBytes = ReadBytes(
                (ulong)index, Native.cna_cnb_model_copy_part_index_bytes, nameof(CnbModelPart.IndexBytes));

            parts[index] = new CnbModelPart(
                index, name, info, externalEffect, vertexBytes, indexBytes, ReadMaterial((ulong)index));
        }

        return parts;
    }

    private unsafe delegate CnaResult ByteCopy(
        CnaHandle model, ulong index, byte* destination, ulong capacity, out ulong outByteCount);

    /// <summary>
    /// The size half of a capacity-probe route.
    ///
    /// These are not the size-then-copy pair the string families use: there is no separate
    /// <c>_size</c> route, and a zero-capacity call answers <b>BufferTooSmall</b> having written the
    /// required count. That is the documented contract -- "insufficient capacity performs no partial
    /// write" -- and treating it as a failure is what the first version of this code did, which made
    /// every part unreadable. Success is accepted too, because a genuinely empty buffer has nothing
    /// to be too small for.
    /// </summary>
    private static void ThrowUnlessProbed(CnaResult result, string context)
    {
        if (result != CnaResult.BufferTooSmall)
        {
            CnaException.ThrowIfFailed(result, context);
        }
    }

    private unsafe byte[] ReadBytes(ulong index, ByteCopy copy, string context)
    {
        ThrowUnlessProbed(copy(Handle, index, null, 0, out ulong byteCount), context);
        GC.KeepAlive(this);

        if (byteCount == 0)
        {
            return [];
        }

        var bytes = new byte[checked((int)byteCount)];
        fixed (byte* destination = bytes)
        {
            CnaException.ThrowIfFailed(
                copy(Handle, index, destination, byteCount, out ulong written), context);
            GC.KeepAlive(this);

            if (written != byteCount)
            {
                throw new CnaException(
                    $"CNB reported {byteCount} bytes for {context} and produced {written}.");
            }
        }

        return bytes;
    }

    private CnbMaterial ReadMaterial(ulong part)
    {
        var info = CnaCnbMaterialInfo.Versioned();
        CnaException.ThrowIfFailed(
            Native.cna_cnb_model_get_material(Handle, part, ref info), nameof(CnbMaterial));
        GC.KeepAlive(this);

        var textures = new List<CnbMaterialTexture>();
        for (uint slot = 0; slot <= (uint)CnbMaterialTextureSlot.SpecularColor; slot++)
        {
            var native = (CnaCnbMaterialTextureSlot)slot;
            CnaException.ThrowIfFailed(
                Native.cna_cnb_model_get_material_texture_size(Handle, part, native, out ulong byteCount),
                nameof(CnbMaterial));
            GC.KeepAlive(this);

            // Zero bytes means the slot is unused. Surfacing it as a texture with an empty name
            // would make every material report eight textures and force every caller to filter.
            if (byteCount == 0)
            {
                continue;
            }

            string assetName = ReadSlotString(part, native, byteCount);
            textures.Add(new CnbMaterialTexture(
                (CnbMaterialTextureSlot)slot,
                assetName,
                ReadImporterState(part, (CnbMaterialTextureSlot)slot)));
        }

        return new CnbMaterial(info, textures);
    }

    /// <summary>
    /// The per-slot state, crossed into the importer's index space -- or nothing, when the name slot
    /// has no importer counterpart.
    /// </summary>
    private CnbImporterSlotState? ReadImporterState(ulong part, CnbMaterialTextureSlot slot)
    {
        if (CnbMaterialTextureSlotMap.ImporterSlot(slot) is not { } importerSlot)
        {
            return null;
        }

        CnaException.ThrowIfFailed(
            Native.cna_cnb_model_get_material_texture_coordinate_set(
                Handle, part, importerSlot, out byte coordinateSet),
            nameof(CnbMaterial));
        CnaException.ThrowIfFailed(
            Native.cna_cnb_model_get_material_texture_transform(
                Handle, part, importerSlot, out CnaCnbTextureTransform transform),
            nameof(CnbMaterial));
        CnaException.ThrowIfFailed(
            Native.cna_cnb_model_get_material_sampler(Handle, part, importerSlot, out CnaCnbSamplerState sampler),
            nameof(CnbMaterial));
        GC.KeepAlive(this);

        return new CnbImporterSlotState(
            coordinateSet, new CnbTextureTransform(transform), new CnbSamplerState(sampler));
    }

    private unsafe string ReadSlotString(ulong part, CnaCnbMaterialTextureSlot slot, ulong byteCount)
    {
        var buffer = new byte[checked((int)byteCount)];
        fixed (byte* destination = buffer)
        {
            CnaException.ThrowIfFailed(
                Native.cna_cnb_model_copy_material_texture(
                    Handle, part, slot, destination, byteCount, out ulong written),
                nameof(CnbMaterial));
            GC.KeepAlive(this);
            return Encoding.UTF8.GetString(buffer, 0, checked((int)written));
        }
    }

    private unsafe CnbModelMesh[] ReadMeshes(
        int count, IReadOnlyList<CnbModelBone> bones, IReadOnlyList<CnbModelPart> parts)
    {
        var meshes = new CnbModelMesh[count];
        for (int index = 0; index < count; index++)
        {
            var info = CnaCnbMeshInfo.Versioned();
            CnaException.ThrowIfFailed(
                Native.cna_cnb_model_get_mesh(Handle, (ulong)index, ref info), nameof(Meshes));
            GC.KeepAlive(this);

            string name = NativeStringReader.ReadIndexed(
                Native.cna_cnb_model_get_mesh_name_size,
                Native.cna_cnb_model_copy_mesh_name,
                Handle,
                (ulong)index,
                nameof(Meshes));

            var partIndices = new uint[checked((int)info.PartIndexCount)];
            if (partIndices.Length > 0)
            {
                fixed (uint* destination = partIndices)
                {
                    CnaException.ThrowIfFailed(
                        Native.cna_cnb_model_copy_mesh_part_indices(
                            Handle, (ulong)index, destination, (ulong)partIndices.Length, out _),
                        nameof(Meshes));
                    GC.KeepAlive(this);
                }
            }

            var meshParts = new CnbModelPart[partIndices.Length];
            for (int slot = 0; slot < partIndices.Length; slot++)
            {
                uint partIndex = partIndices[slot];
                if (partIndex >= (uint)parts.Count)
                {
                    throw new CnaException(
                        $"CNB mesh {index} names part {partIndex}, but the model has {parts.Count} part(s).");
                }

                meshParts[slot] = parts[(int)partIndex];
            }

            var mesh = new CnbModelMesh(index, name, info.ParentBone, meshParts);
            if (info.ParentBone >= 0)
            {
                if (info.ParentBone >= bones.Count)
                {
                    throw new CnaException(
                        $"CNB mesh {index} names bone {info.ParentBone}, but the model has {bones.Count} bone(s).");
                }

                mesh.ParentBone = bones[info.ParentBone];
            }

            meshes[index] = mesh;
        }

        return meshes;
    }

    private unsafe CnbSkeleton ReadSkeleton()
    {
        var info = CnaCnbSkeletonInfo.Versioned();
        CnaException.ThrowIfFailed(Native.cna_cnb_model_get_skeleton(Handle, ref info), nameof(Skeleton));
        GC.KeepAlive(this);

        int joints = checked((int)info.JointCount);
        var hierarchy = new int[joints];
        if (joints > 0)
        {
            fixed (int* destination = hierarchy)
            {
                CnaException.ThrowIfFailed(
                    Native.cna_cnb_model_copy_skeleton_hierarchy(
                        Handle, destination, (ulong)joints, out _),
                    nameof(Skeleton));
                GC.KeepAlive(this);
            }
        }

        return new CnbSkeleton(
            hierarchy,
            ReadMatrices(CnaCnbSkeletonMatrixSet.BindPose),
            ReadMatrices(CnaCnbSkeletonMatrixSet.InverseBindPose),
            ReadMatrices(CnaCnbSkeletonMatrixSet.RootPrefix));
    }

    private unsafe Matrix[] ReadMatrices(CnaCnbSkeletonMatrixSet set)
    {
        ThrowUnlessProbed(
            Native.cna_cnb_model_copy_skeleton_matrices(Handle, set, null, 0, out ulong floatCount),
            nameof(Skeleton));
        GC.KeepAlive(this);

        if (floatCount == 0)
        {
            return [];
        }

        if (floatCount % 16 != 0)
        {
            throw new CnaException(
                $"CNB skeleton matrix set {set} reported {floatCount} floats, which is not a whole number of 4x4 matrices.");
        }

        var values = new float[checked((int)floatCount)];
        fixed (float* destination = values)
        {
            CnaException.ThrowIfFailed(
                Native.cna_cnb_model_copy_skeleton_matrices(Handle, set, destination, floatCount, out _),
                nameof(Skeleton));
            GC.KeepAlive(this);
        }

        var matrices = new Matrix[values.Length / 16];
        for (int index = 0; index < matrices.Length; index++)
        {
            int at = index * 16;
            matrices[index] = new Matrix(
                values[at + 0], values[at + 1], values[at + 2], values[at + 3],
                values[at + 4], values[at + 5], values[at + 6], values[at + 7],
                values[at + 8], values[at + 9], values[at + 10], values[at + 11],
                values[at + 12], values[at + 13], values[at + 14], values[at + 15]);
        }

        return matrices;
    }
}
