namespace CNA.Interop;

/// <summary>
/// Mirrors <c>cnb.h</c>'s <c>CNA_CnbEffectKind</c> exactly (<c>cnb.h:2453-2468</c>): which effect a
/// compiled model part draws with.
///
/// <see cref="External"/> is the only one that gives the part's <c>external_effect</c> name a
/// meaning; for every other kind the header says the field is unused, so a reader that reported it
/// regardless would be inventing a dependency the file does not declare.
/// </summary>
internal enum CnaCnbEffectKind : uint
{
    Basic = 0,
    Skinned = 1,
    DualTexture = 2,
    Pbr = 3,
    SkinnedPbr = 4,
    External = 5,
}

/// <summary>
/// Mirrors <c>cnb.h</c>'s <c>CNA_CnbMaterialTextureSlot</c> exactly (<c>cnb.h:2491-2510</c>): the
/// eight named texture slots a CNB material can fill.
///
/// The set is glTF's, not XNA's -- XNA's <c>BasicEffect</c> has one texture and
/// <c>DualTextureEffect</c> has two, while metallic-roughness, occlusion and the two specular slots
/// exist only in a PBR material. That is why this enum lives in CNA's own vocabulary and does not
/// try to project onto anything in <c>Microsoft.Xna.Framework</c>.
/// </summary>
internal enum CnaCnbMaterialTextureSlot : uint
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

/// <summary>
/// Mirrors <c>cnb.h</c>'s <c>CNA_CnbSkeletonMatrixSet</c> exactly (<c>cnb.h:2537-2546</c>): which of
/// a skeleton's three per-joint matrix arrays to copy.
///
/// <see cref="RootPrefix"/> is the one that can legitimately be empty: the header says it reports
/// zero floats when the source carried none, which is a fact about the content rather than a
/// failure, and <c>CNA_CnbSkeletonInfo.has_root_prefix</c> says so in advance.
/// </summary>
internal enum CnaCnbSkeletonMatrixSet : uint
{
    BindPose = 0,
    InverseBindPose = 1,
    RootPrefix = 2,
}

/// <summary>
/// Mirrors <c>cnb.h</c>'s <c>CNA_CnbAudioFormat</c> exactly (<c>cnb.h:3941-3953</c>): how a compiled
/// sound's samples are stored.
///
/// Every identity is declared, including the ones this binding cannot yet hand to CNA's audio
/// engine. A decoder that only knew the playable subset could not report what a file it refused
/// actually holds, which is the difference between "this asset needs ADPCM" and "this asset failed".
/// </summary>
internal enum CnaCnbAudioFormat : uint
{
    Unknown = 0,
    Pcm16 = 1,
    Pcm8 = 2,
    PcmFloat32 = 3,
    Adpcm = 4,
    Vorbis = 5,
}
