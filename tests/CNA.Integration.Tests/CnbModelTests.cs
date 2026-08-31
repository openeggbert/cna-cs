using CNA;
using CNA.Content.Cnb;
using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// D2's third vertical slice: a compiled CNB model decoded into a managed graph.
///
/// <b>What these tests are built to catch.</b> "Decode returned success" and "the handle is not
/// zero" are worth nothing here -- a decoder that produced the right *counts* and wired the wrong
/// *relationships* would satisfy both, and a game would find out when a mesh drew the wrong
/// geometry or a bone animated the wrong child. So the fixture is deliberately asymmetric: three
/// bones in a chain whose transforms are all different, two parts with different vertex bytes, and
/// a mesh that names them **out of model order and one of them twice**. Every one of those choices
/// exists to make a plausible wrong implementation fail:
///
/// <list type="bullet">
/// <item>a mesh part list read as "the parts in model order" gets the order wrong;</item>
/// <item>one read as a set gets the length wrong;</item>
/// <item>a parent link built by position rather than by index gets the chain wrong;</item>
/// <item>a transform read column-major instead of row-major gets the translation wrong;</item>
/// <item>index bytes read as vertex bytes get the payload wrong.</item>
/// </list>
///
/// The fixture is written by CNA's own model encoder, so a fixture and a reader cannot be wrong in
/// the same way and agree with each other.
/// </summary>
[Collection(NativeGameCollection.Name)]
public class CnbModelTests(ITestOutputHelper output)
{
    /// <summary>Three bones in a chain, two parts, one mesh naming part 1 then part 0 then part 1
    /// again, a two-slot material and a two-joint skeleton with no root prefix.</summary>
    private static string WriteFixture()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-model-{Guid.NewGuid():N}.cnb");

        using var builder = new CnbTestModelBuilder();
        builder.SetFlags(appliesGltfLightingPolicy: true, hasBoneHierarchy: true);

        builder.AddBone("root", -1, Matrix.CreateTranslation(1f, 2f, 3f));
        builder.AddBone("spine", 0, Matrix.CreateTranslation(4f, 5f, 6f));
        builder.AddBone("head", 1, Matrix.CreateTranslation(7f, 8f, 9f));

        // Distinct bytes per part, so a reader that returned the first part's buffer for both, or
        // handed back the index bytes as vertex bytes, produces a visible mismatch rather than a
        // plausible-looking array.
        // Three vertices each: CNA's decoder checks that every index addresses a vertex the part
        // actually has, and refuses the file otherwise -- which is worth knowing and was found by
        // authoring a two-vertex part with an index of 2.
        byte[] firstVertices = [0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B];
        byte[] firstIndices = [0, 0, 1, 0, 2, 0];
        byte[] secondVertices = [0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B];
        byte[] secondIndices = [2, 0, 1, 0, 0, 0];

        int first = builder.AddPart("hull", firstVertices, 4, firstIndices, 2);
        int second = builder.AddPart(
            "turret", secondVertices, 4, secondIndices, 2, CnbEffectKind.External, "effects/turret");

        builder.SetMaterial(first, new Vector4(0.25f, 0.5f, 0.75f, 1f), 0.125f, 0.875f, CnbAlphaMode.Mask, 0.4f, true);
        builder.SetMaterialTexture(first, CnbMaterialTextureSlot.BaseColor, "textures/hull_albedo");
        builder.SetMaterialTexture(first, CnbMaterialTextureSlot.Normal, "textures/hull_normal");

        // Out of model order, and part 1 twice. Both are legal and both break a reader that treats
        // a mesh's part list as a view of the model's parts.
        builder.AddMesh("body", 1, second, first, second);

        builder.SetSkeleton(
            [-1, 0],
            [Matrix.CreateTranslation(10f, 0f, 0f), Matrix.CreateTranslation(0f, 20f, 0f)],
            [Matrix.CreateTranslation(-10f, 0f, 0f), Matrix.CreateTranslation(0f, -20f, 0f)],
            []);

        builder.WriteToFile(path, "models/fixture");
        return path;
    }

    [NativeFact]
    public void DecodedModel_CarriesItsIdentityCountsAndFlags()
    {
        string path = WriteFixture();
        try
        {
            using var document = CnbDocument.Open(path);
            Assert.Equal(CnbModel.ModelAssetTypeId, document.AssetTypeId);

            using CnbModel model = CnbModel.Decode(document);

            Assert.Equal(3, model.Bones.Count);
            Assert.Equal(2, model.Parts.Count);
            Assert.Single(model.Meshes);

            // Both flags, both directions. A reader that returned a constant true, or read the two
            // bytes in the other order, passes a one-flag test.
            Assert.True(model.AppliesGltfLightingPolicy);
            Assert.True(model.HasBoneHierarchy);

            output.WriteLine(
                $"decoded {model.Bones.Count} bone(s), {model.Parts.Count} part(s), {model.Meshes.Count} mesh(es)");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The bone chain, by name, by index and by object identity.
    ///
    /// Object identity is the part that matters: <c>Parent</c> and <c>Children</c> are links this
    /// binding builds, not fields the file contains, so they are exactly where a wrong
    /// implementation lives. Asserting only <c>ParentIndex</c> would test the file and not the code.
    /// </summary>
    [NativeFact]
    public void DecodedModel_LinksTheBoneHierarchyBothWays()
    {
        string path = WriteFixture();
        try
        {
            using CnbModel model = CnbModel.DecodeFile(path);

            Assert.Equal(["root", "spine", "head"], model.Bones.Select(bone => bone.Name));
            Assert.Equal([-1, 0, 1], model.Bones.Select(bone => bone.ParentIndex));

            CnbModelBone root = model.Bones[0];
            CnbModelBone spine = model.Bones[1];
            CnbModelBone head = model.Bones[2];

            Assert.Null(root.Parent);
            Assert.Same(root, spine.Parent);
            Assert.Same(spine, head.Parent);

            Assert.Same(root, Assert.Single(model.RootBones));
            Assert.Same(spine, Assert.Single(root.Children));
            Assert.Same(head, Assert.Single(spine.Children));
            Assert.Empty(head.Children);

            // Row-major, and each bone's own. A reader that transposed would put 1,2,3 in M14/M24/M34
            // and a reader that returned one bone's transform for all three would agree on the first.
            Assert.Equal(new Vector3(1f, 2f, 3f), root.Transform.Translation);
            Assert.Equal(new Vector3(4f, 5f, 6f), spine.Transform.Translation);
            Assert.Equal(new Vector3(7f, 8f, 9f), head.Transform.Translation);

            output.WriteLine($"chain: {root.Name} -> {spine.Name} -> {head.Name}");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The mesh's parts, in the file's draw order, including the repeat.
    ///
    /// This is the single most valuable assertion in the file. The mesh names part 1, part 0, part 1
    /// -- so the sequence distinguishes draw order from model order, and the length distinguishes a
    /// list from a set. Both are mistakes a reader can make while producing a graph that looks
    /// entirely reasonable.
    /// </summary>
    [NativeFact]
    public void DecodedModel_KeepsMeshPartsInDrawOrderIncludingRepeats()
    {
        string path = WriteFixture();
        try
        {
            using CnbModel model = CnbModel.DecodeFile(path);

            CnbModelMesh mesh = Assert.Single(model.Meshes);
            Assert.Equal("body", mesh.Name);
            Assert.Equal(3, mesh.Parts.Count);

            Assert.Same(model.Parts[1], mesh.Parts[0]);
            Assert.Same(model.Parts[0], mesh.Parts[1]);
            Assert.Same(model.Parts[1], mesh.Parts[2]);

            // The mesh hangs from bone 1, not bone 0 and not "the root".
            Assert.Equal(1, mesh.ParentBoneIndex);
            Assert.Same(model.Bones[1], mesh.ParentBone);

            output.WriteLine(
                $"mesh '{mesh.Name}' draws {string.Join(", ", mesh.Parts.Select(part => part.Name))}");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Each part's geometry bytes, exactly.
    ///
    /// Byte-for-byte rather than by length: a reader returning correctly sized zeros satisfies every
    /// size assertion, and that is the failure this repository has already seen once, in the CNB
    /// texture slice.
    /// </summary>
    [NativeFact]
    public void DecodedModel_CarriesEachPartsOwnVertexAndIndexBytes()
    {
        string path = WriteFixture();
        try
        {
            using CnbModel model = CnbModel.DecodeFile(path);

            CnbModelPart hull = model.Parts[0];
            CnbModelPart turret = model.Parts[1];

            Assert.Equal("hull", hull.Name);
            Assert.Equal<byte[]>(
                [0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B], hull.VertexBytes);
            Assert.Equal<byte[]>([0, 0, 1, 0, 2, 0], hull.IndexBytes);
            Assert.Equal(4, hull.VertexStride);
            Assert.Equal(3, hull.VertexCount);
            Assert.Equal(3, hull.IndexCount);
            Assert.Equal(2, hull.IndexElementSize);
            Assert.Equal(1, hull.PrimitiveCount);

            Assert.Equal("turret", turret.Name);
            Assert.Equal<byte[]>(
                [0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B], turret.VertexBytes);
            Assert.Equal<byte[]>([2, 0, 1, 0, 0, 0], turret.IndexBytes);
            Assert.Equal(3, turret.VertexCount);

            output.WriteLine(
                $"hull {hull.VertexBytes.Length}B verts / {hull.IndexBytes.Length}B indices, " +
                $"turret {turret.VertexBytes.Length}B / {turret.IndexBytes.Length}B");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The material, its two filled slots, and the six it does not have.
    ///
    /// The absence assertions are the point: a reader that surfaced all eight slots regardless would
    /// make every material claim eight textures, and a game iterating them would try to load six
    /// assets that do not exist.
    /// </summary>
    [NativeFact]
    public void DecodedModel_AssociatesMaterialsAndOnlyTheTexturesTheFileNames()
    {
        string path = WriteFixture();
        try
        {
            using CnbModel model = CnbModel.DecodeFile(path);

            CnbMaterial material = model.Parts[0].Material;
            Assert.Equal(new Vector4(0.25f, 0.5f, 0.75f, 1f), material.BaseColorFactor);
            Assert.Equal(0.125f, material.MetallicFactor);
            Assert.Equal(0.875f, material.RoughnessFactor);
            Assert.Equal(CnbAlphaMode.Mask, material.AlphaMode);
            Assert.Equal(0.4f, material.AlphaCutoff);
            Assert.True(material.DoubleSided);

            Assert.Equal(2, material.Textures.Count);
            Assert.Equal("textures/hull_albedo", material.Texture(CnbMaterialTextureSlot.BaseColor)?.AssetName);
            Assert.Equal("textures/hull_normal", material.Texture(CnbMaterialTextureSlot.Normal)?.AssetName);
            Assert.Null(material.Texture(CnbMaterialTextureSlot.Emissive));
            Assert.Null(material.Texture(CnbMaterialTextureSlot.MetallicRoughness));
            Assert.Null(material.Texture(CnbMaterialTextureSlot.Occlusion));
            Assert.Null(material.Texture(CnbMaterialTextureSlot.Second));
            Assert.Null(material.Texture(CnbMaterialTextureSlot.Specular));
            Assert.Null(material.Texture(CnbMaterialTextureSlot.SpecularColor));

            // The second part's material was never set, so it must not inherit the first's.
            Assert.Empty(model.Parts[1].Material.Textures);

            output.WriteLine(
                $"hull material: {string.Join(", ", material.Textures.Select(t => $"{t.Slot}={t.AssetName}"))}");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The effect association, and <b>whose</b> rule the empty name for a non-external part is.
    ///
    /// This test was first written to catch a reader that surfaced the effect name for every part,
    /// and it could not: removing the <c>EffectKind == External</c> guard from the reader left every
    /// assertion green. The reason turned out to be worth more than the guard. <b>CNA drops the
    /// name itself.</b> The fixture below hands a <see cref="CnbEffectKind.Basic"/> part the name
    /// <c>effects/should-not-appear</c>, CNA's encoder stores nothing, and the decoder answers an
    /// empty string -- so the managed guard cannot change an answer and is a cost optimisation (two
    /// native round trips saved per non-external part), not a correctness rule. Its doc comment now
    /// says so.
    ///
    /// What is asserted here instead is falsifiable, and is the thing actually worth watching: the
    /// empty name is <em>upstream's</em> answer. If a future CNA started carrying the field for
    /// every kind, this fails -- and that is exactly when this binding would have to decide whether
    /// to surface it.
    /// </summary>
    [NativeFact]
    public void DecodedModel_ReportsAnEffectNameOnlyForAnExternalEffect()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-effect-{Guid.NewGuid():N}.cnb");
        try
        {
            using (var builder = new CnbTestModelBuilder())
            {
                builder.AddBone("root", -1, Matrix.Identity);
                byte[] vertices = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
                byte[] indices = [0, 0, 1, 0, 2, 0];

                // A Basic part handed an effect name anyway. If CNA kept it, a reader without the
                // guard would report a dependency the model never asked for.
                builder.AddPart("basic", vertices, 4, indices, 2, CnbEffectKind.Basic, "effects/should-not-appear");
                builder.AddPart("external", vertices, 4, indices, 2, CnbEffectKind.External, "effects/turret");
                builder.AddMesh("m", 0, 0, 1);
                builder.WriteToFile(path);
            }

            using CnbModel model = CnbModel.DecodeFile(path);

            Assert.Equal(CnbEffectKind.Basic, model.Parts[0].EffectKind);
            Assert.Equal(string.Empty, model.Parts[0].ExternalEffect);

            Assert.Equal(CnbEffectKind.External, model.Parts[1].EffectKind);
            Assert.Equal("effects/turret", model.Parts[1].ExternalEffect);

            output.WriteLine(
                "a Basic part authored with 'effects/should-not-appear' decodes with no effect name: " +
                "CNA drops it, so the managed guard is a cost saving rather than the rule");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The skeleton, including the empty root prefix.
    ///
    /// An empty <see cref="CnbSkeleton.RootPrefix"/> is the documented way of saying the source
    /// carried none; a reader that filled it with identity matrices to keep the three arrays the
    /// same length would be inventing a transform, and every joint count assertion would still pass.
    /// </summary>
    [NativeFact]
    public void DecodedModel_CarriesTheSkeletonAndDistinguishesAnAbsentRootPrefix()
    {
        string path = WriteFixture();
        try
        {
            using CnbModel model = CnbModel.DecodeFile(path);

            CnbSkeleton skeleton = Assert.IsType<CnbSkeleton>(model.Skeleton);
            Assert.Equal(2, skeleton.JointCount);
            Assert.Equal([-1, 0], skeleton.Hierarchy);

            Assert.Equal(2, skeleton.BindPose.Count);
            Assert.Equal(new Vector3(10f, 0f, 0f), skeleton.BindPose[0].Translation);
            Assert.Equal(new Vector3(0f, 20f, 0f), skeleton.BindPose[1].Translation);

            // Not the same array as the bind pose, and not its negation by accident.
            Assert.Equal(new Vector3(-10f, 0f, 0f), skeleton.InverseBindPose[0].Translation);
            Assert.Equal(new Vector3(0f, -20f, 0f), skeleton.InverseBindPose[1].Translation);

            Assert.Empty(skeleton.RootPrefix);

            output.WriteLine($"skeleton: {skeleton.JointCount} joint(s), root prefix empty");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The decoded graph outlives the document it came from.
    ///
    /// The header says decoding copies rather than views, and <see cref="CnbModel"/> relies on that
    /// to be a snapshot with a single owner. If it were a view, this would read freed memory --
    /// which is exactly the class of bug that does not announce itself, so it is asserted rather
    /// than assumed.
    /// </summary>
    [NativeFact]
    public void DecodedModel_StaysReadableAfterItsDocumentIsDisposed()
    {
        string path = WriteFixture();
        try
        {
            CnbModel model;
            using (CnbDocument document = CnbDocument.Open(path))
            {
                model = CnbModel.Decode(document);
            }

            using (model)
            {
                Assert.Equal("head", model.Bones[2].Name);
                Assert.Equal<byte[]>(
                    [0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B],
                    model.Parts[1].VertexBytes);
                Assert.Same(model.Parts[1], model.Meshes[0].Parts[2]);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>A document that is not a model is refused, rather than decoded into an empty
    /// graph.</summary>
    [NativeFact]
    public void Decode_RefusesADocumentThatIsNotAModel()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-notamodel-{Guid.NewGuid():N}.cnb");
        try
        {
            using (var writer = new CnbTestWriter(0x54534554, 1))
            {
                writer.AddChunk(CnbTestWriter.ChunkId("ONE_"), [1, 2, 3, 4]);
                writer.WriteToFile(path);
            }

            using CnbDocument document = CnbDocument.Open(path);
            Assert.NotEqual(CnbModel.ModelAssetTypeId, document.AssetTypeId);

            CnaException failure = Assert.Throws<CnaException>(() => CnbModel.Decode(document));
            output.WriteLine($"non-model document refused: {failure.Message}");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The trap the header names: a texture <b>name</b> slot and an <b>importer</b> slot are
    /// different index spaces, and emissive and occlusion are swapped between them.
    ///
    /// This is the most valuable test in the file, because the mistake it catches is invisible. The
    /// first version of this binding passed the name slot straight into the per-slot routes, which
    /// compiles, never fails, and quietly returns the occlusion map's coordinate set when asked for
    /// the emissive map's. Every other assertion in this class passed while that was true.
    ///
    /// The fixture gives each of the seven importer slots a distinct coordinate set, addressed in
    /// the importer's own space, so the name-slot lookups have to land on the right ones. A binding
    /// that passed the name slot through reads 5 where 4 is asked for and 4 where 5 is -- and a
    /// binding that reported <see cref="CnbMaterialTextureSlot.Second"/>'s state from importer slot
    /// 1 gets the normal map's.
    /// </summary>
    [NativeFact]
    public void DecodedModel_CrossesNameSlotsIntoTheImportersOwnSlotSpace()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cna-cnb-slots-{Guid.NewGuid():N}.cnb");
        try
        {
            using (var builder = new CnbTestModelBuilder())
            {
                builder.AddBone("root", -1, Matrix.Identity);
                byte[] vertices = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
                byte[] indices = [0, 0, 1, 0, 2, 0];
                builder.AddPart("p", vertices, 4, indices, 2);

                // Every name slot filled, so nothing is skipped for being unused.
                builder.SetMaterialTexture(0, CnbMaterialTextureSlot.BaseColor, "t/base");
                builder.SetMaterialTexture(0, CnbMaterialTextureSlot.Second, "t/second");
                builder.SetMaterialTexture(0, CnbMaterialTextureSlot.Normal, "t/normal");
                builder.SetMaterialTexture(0, CnbMaterialTextureSlot.MetallicRoughness, "t/mr");
                builder.SetMaterialTexture(0, CnbMaterialTextureSlot.Emissive, "t/emissive");
                builder.SetMaterialTexture(0, CnbMaterialTextureSlot.Occlusion, "t/occlusion");
                builder.SetMaterialTexture(0, CnbMaterialTextureSlot.Specular, "t/specular");
                builder.SetMaterialTexture(0, CnbMaterialTextureSlot.SpecularColor, "t/specularcolor");

                // Importer order: base colour, normal, metallic-roughness, occlusion, emissive,
                // specular, specular colour. A distinct transform offset per slot, so a wrong index
                // is a wrong number -- CNA refuses a coordinate set above 1 (its vertex layouts
                // carry two at most), so the transform is what can carry seven distinct values.
                // The coordinate set still runs alongside as an independent signal: 3 and 4 have
                // different parity, which is enough to separate the swapped pair on its own.
                for (int importerSlot = 0; importerSlot < 7; importerSlot++)
                {
                    builder.SetImporterTransform(0, importerSlot, importerSlot + 1, 0f, 1f, 1f, 0f);
                    builder.SetImporterCoordinateSet(0, importerSlot, (byte)(importerSlot % 2));
                }

                builder.AddMesh("m", 0, 0);
                builder.WriteToFile(path);
            }

            using CnbModel model = CnbModel.DecodeFile(path);
            CnbMaterial material = model.Parts[0].Material;
            Assert.Equal(8, material.Textures.Count);

            CnbImporterSlotState StateOf(CnbMaterialTextureSlot slot) =>
                material.Texture(slot)!.Value.ImporterState!.Value;

            float OffsetOf(CnbMaterialTextureSlot slot) => StateOf(slot).Transform.Offset.X;

            Assert.Equal(1f, OffsetOf(CnbMaterialTextureSlot.BaseColor));
            Assert.Equal(2f, OffsetOf(CnbMaterialTextureSlot.Normal));
            Assert.Equal(3f, OffsetOf(CnbMaterialTextureSlot.MetallicRoughness));

            // The pair a pass-through binding gets wrong, and the whole point of this test:
            // occlusion is importer slot 3 and emissive is importer slot 4, while as *names* they
            // are 5 and 4. Asserting only one of them would not distinguish the two spaces.
            Assert.Equal(4f, OffsetOf(CnbMaterialTextureSlot.Occlusion));
            Assert.Equal(5f, OffsetOf(CnbMaterialTextureSlot.Emissive));

            Assert.Equal(6f, OffsetOf(CnbMaterialTextureSlot.Specular));
            Assert.Equal(7f, OffsetOf(CnbMaterialTextureSlot.SpecularColor));

            // Independently of the transform: importer 3 and 4 have different parity.
            Assert.Equal(1, StateOf(CnbMaterialTextureSlot.Occlusion).CoordinateSet);
            Assert.Equal(0, StateOf(CnbMaterialTextureSlot.Emissive).CoordinateSet);

            // DualTextureEffect's second layer is CNA's own slot; the importer has no entry for it.
            Assert.Null(material.Texture(CnbMaterialTextureSlot.Second)!.Value.ImporterState);
            Assert.Equal("t/second", material.Texture(CnbMaterialTextureSlot.Second)!.Value.AssetName);

            output.WriteLine(
                "name->importer: occlusion(name 5)->slot 3, emissive(name 4)->slot 4, second(name 1)->none");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
