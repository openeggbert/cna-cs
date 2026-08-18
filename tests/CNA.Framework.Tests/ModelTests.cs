using CNA.Graphics;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="Model"/>/<see cref="ModelBone"/>/the bone-hierarchy collections are pure managed
/// state and math -- no native dependency, real and testable today (see their own doc comments,
/// all confirmed against the real openeggbert/cna C++ engine's implementation). <see cref="ModelMesh.Draw"/>
/// itself needs a real native device to actually draw, but <see cref="Model.Draw"/>'s own
/// bone-transform/effect-matrix-assignment logic runs *before* any native call, so it's tested
/// here by letting the expected-to-fail native call throw and then checking the state it already
/// set -- see <see cref="Draw_SetsEffectMatricesBeforeDrawingMesh"/>.
/// </summary>
public class ModelTests
{
    private static GraphicsDevice CreateDummyDevice() => new(nativeGameHandleValue: 0);

    private sealed class RecordingEffect(GraphicsDevice graphicsDevice) : Effect(graphicsDevice), IEffectMatrices
    {
        public Matrix World { get; set; } = Matrix.Identity;
        public Matrix View { get; set; } = Matrix.Identity;
        public Matrix Projection { get; set; } = Matrix.Identity;

        protected override void OnApply()
        {
        }
    }

    private sealed class NonMatrixEffect(GraphicsDevice graphicsDevice) : Effect(graphicsDevice)
    {
        protected override void OnApply()
        {
        }
    }

    [Fact]
    public void ModelBone_AddChild_SetsParentAndChildren()
    {
        var parent = new ModelBone(0, "parent");
        var child = new ModelBone(1, "child");

        parent.AddChild(child);

        Assert.Same(parent, child.Parent);
        Assert.Single(parent.Children);
        Assert.Same(child, parent.Children[0]);
    }

    [Fact]
    public void Constructor_EmptyBones_RootIsNull()
    {
        var model = new Model(CreateDummyDevice(), [], []);

        Assert.Null(model.Root);
        Assert.Equal(0, model.Bones.Count);
    }

    [Fact]
    public void Constructor_DefaultsRootToFirstBone()
    {
        var bone0 = new ModelBone(0, "root");
        var bone1 = new ModelBone(1, "other");

        var model = new Model(CreateDummyDevice(), [bone0, bone1], []);

        Assert.Same(bone0, model.Root);
    }

    [Fact]
    public void Constructor_WithRootBoneIndex_SelectsCorrectBone()
    {
        var bone0 = new ModelBone(0, "a");
        var bone1 = new ModelBone(1, "b");

        var model = new Model(CreateDummyDevice(), [bone0, bone1], [], [], rootBoneIndex: 1);

        Assert.Same(bone1, model.Root);
    }

    [Fact]
    public void Constructor_RootBoneIndexOutOfRange_Throws()
    {
        var bone0 = new ModelBone(0, "a");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Model(CreateDummyDevice(), [bone0], [], [], rootBoneIndex: 5));
    }

    [Fact]
    public void Constructor_MeshParentBonesWrongLength_Throws()
    {
        var bone0 = new ModelBone(0, "root");
        var mesh0 = new ModelMesh(CreateDummyDevice(), []);
        var mesh1 = new ModelMesh(CreateDummyDevice(), []);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Model(CreateDummyDevice(), [bone0], [mesh0, mesh1], [bone0]));
    }

    [Fact]
    public void Constructor_MeshParentBones_SetsEachMeshParentBone()
    {
        var bone0 = new ModelBone(0, "root");
        var bone1 = new ModelBone(1, "other");
        var mesh0 = new ModelMesh(CreateDummyDevice(), []);
        var mesh1 = new ModelMesh(CreateDummyDevice(), []);

        _ = new Model(CreateDummyDevice(), [bone0, bone1], [mesh0, mesh1], [bone1, bone0]);

        Assert.Same(bone1, mesh0.ParentBone);
        Assert.Same(bone0, mesh1.ParentBone);
    }

    [Fact]
    public void CopyAbsoluteBoneTransformsTo_SingleRootBone_ReturnsItsOwnTransform()
    {
        var root = new ModelBone(0, "root") { Transform = Matrix.CreateTranslation(1f, 2f, 3f) };
        var model = new Model(CreateDummyDevice(), [root], []);
        var destination = new Matrix[1];

        model.CopyAbsoluteBoneTransformsTo(destination);

        Assert.Equal(new Vector3(1f, 2f, 3f), destination[0].Translation);
    }

    [Fact]
    public void CopyAbsoluteBoneTransformsTo_ThreeLevelChain_ComposesTranslations()
    {
        var root = new ModelBone(0, "root") { Transform = Matrix.CreateTranslation(1f, 0f, 0f) };
        var child = new ModelBone(1, "child") { Transform = Matrix.CreateTranslation(0f, 2f, 0f) };
        var grandchild = new ModelBone(2, "grandchild") { Transform = Matrix.CreateTranslation(0f, 0f, 3f) };
        root.AddChild(child);
        child.AddChild(grandchild);
        var model = new Model(CreateDummyDevice(), [root, child, grandchild], []);
        var destination = new Matrix[3];

        model.CopyAbsoluteBoneTransformsTo(destination);

        Assert.Equal(new Vector3(1f, 0f, 0f), destination[0].Translation);
        Assert.Equal(new Vector3(1f, 2f, 0f), destination[1].Translation);
        Assert.Equal(new Vector3(1f, 2f, 3f), destination[2].Translation);
    }

    [Fact]
    public void CopyAbsoluteBoneTransformsTo_DestinationTooSmall_Throws()
    {
        var root = new ModelBone(0, "root");
        var model = new Model(CreateDummyDevice(), [root], []);

        Assert.Throws<ArgumentOutOfRangeException>(() => model.CopyAbsoluteBoneTransformsTo([]));
    }

    [Fact]
    public void CopyAbsoluteBoneTransformsTo_BoneIndexDoesNotMatchListPosition_Throws()
    {
        var bone0 = new ModelBone(0, "a");
        // bone1's Index (5) deliberately does not match its position (1) in the Bones list below.
        var bone1 = new ModelBone(5, "b");
        var model = new Model(CreateDummyDevice(), [bone0, bone1], []);

        Assert.Throws<InvalidOperationException>(() => model.CopyAbsoluteBoneTransformsTo(new Matrix[2]));
    }

    [Fact]
    public void CopyAbsoluteBoneTransformsTo_ParentAppearsAfterChildInList_Throws()
    {
        var parent = new ModelBone(1, "parent");
        var child = new ModelBone(0, "child");
        parent.AddChild(child);
        // child is at position 0 but its parent is at position 1 -- the parent's absolute
        // transform has not been computed yet when child's turn comes.
        var model = new Model(CreateDummyDevice(), [child, parent], []);

        Assert.Throws<InvalidOperationException>(() => model.CopyAbsoluteBoneTransformsTo(new Matrix[2]));
    }

    [Fact]
    public void Draw_MeshHasEffectButModelHasNoBones_ThrowsInsteadOfCrashing()
    {
        var device = CreateDummyDevice();
        var effect = new RecordingEffect(device);
        var part = new ModelMeshPart(null, null, numVertices: 3, primitiveCount: 1, startIndex: 0, vertexOffset: 0);
        var mesh = new ModelMesh(device, [part]);
        part.Effect = effect;
        var model = new Model(device, [], [mesh]);

        Assert.Throws<InvalidOperationException>(() => model.Draw(Matrix.Identity, Matrix.Identity, Matrix.Identity));
    }

    [Fact]
    public void CopyBoneTransformsFrom_ThenCopyBoneTransformsTo_RoundTrips()
    {
        var bone0 = new ModelBone(0, "a");
        var bone1 = new ModelBone(1, "b");
        var model = new Model(CreateDummyDevice(), [bone0, bone1], []);
        Matrix[] source = [Matrix.CreateTranslation(1f, 0f, 0f), Matrix.CreateTranslation(0f, 5f, 0f)];

        model.CopyBoneTransformsFrom(source);
        var roundTripped = new Matrix[2];
        model.CopyBoneTransformsTo(roundTripped);

        Assert.Equal(source[0], roundTripped[0]);
        Assert.Equal(source[1], roundTripped[1]);
    }

    [Fact]
    public void ModelBoneCollection_IndexerByName_FindsBone()
    {
        var bone0 = new ModelBone(0, "root");
        var model = new Model(CreateDummyDevice(), [bone0], []);

        Assert.Same(bone0, model.Bones["root"]);
    }

    [Fact]
    public void ModelBoneCollection_IndexerByName_NotFound_Throws()
    {
        var model = new Model(CreateDummyDevice(), [new ModelBone(0, "root")], []);

        Assert.Throws<KeyNotFoundException>(() => model.Bones["missing"]);
    }

    [Fact]
    public void ModelBoneCollection_TryGetValue_NotFound_ReturnsFalse()
    {
        var model = new Model(CreateDummyDevice(), [new ModelBone(0, "root")], []);

        Assert.False(model.Bones.TryGetValue("missing", out ModelBone? bone));
        Assert.Null(bone);
    }

    [Fact]
    public void ModelMeshCollection_IndexerByName_FindsMesh()
    {
        var mesh = new ModelMesh(CreateDummyDevice(), "torso", []);
        var model = new Model(CreateDummyDevice(), [], [mesh]);

        Assert.Same(mesh, model.Meshes["torso"]);
    }

    [Fact]
    public void Draw_EffectWithoutIEffectMatrices_Throws()
    {
        var device = CreateDummyDevice();
        var effect = new NonMatrixEffect(device);
        var part = new ModelMeshPart(null, null, numVertices: 3, primitiveCount: 1, startIndex: 0, vertexOffset: 0);
        var mesh = new ModelMesh(device, [part]);
        // Effect must be assigned *after* the part belongs to a mesh (i.e. after ModelMeshPart's
        // Parent is set by ModelMesh's constructor) -- setting it earlier is a no-op for mesh
        // registration purposes, matching the real engine's own behavior (see
        // ModelMeshPartTests.cs's own Effect_Setter tests for the direct coverage of this).
        part.Effect = effect;
        var model = new Model(device, [new ModelBone(0, "root")], [mesh]);

        Assert.Throws<InvalidOperationException>(() => model.Draw(Matrix.Identity, Matrix.Identity, Matrix.Identity));
    }

    [Fact]
    public void Draw_SetsEffectMatricesBeforeDrawingMesh()
    {
        var device = CreateDummyDevice();
        var effect = new RecordingEffect(device);
        var part = new ModelMeshPart(null, null, numVertices: 3, primitiveCount: 1, startIndex: 0, vertexOffset: 0);
        var bone = new ModelBone(0, "root") { Transform = Matrix.CreateTranslation(1f, 2f, 3f) };
        var mesh = new ModelMesh(device, [part]);
        part.Effect = effect;
        var model = new Model(device, [bone], [mesh]);

        Matrix world = Matrix.CreateScale(2f);
        Matrix view = Matrix.CreateTranslation(5f, 0f, 0f);
        Matrix projection = Matrix.CreateTranslation(0f, 5f, 0f);

        // mesh.Draw() calls into native code past this point (no cna-native present in this
        // environment), but Model.Draw() sets every effect's matrices *before* calling mesh.Draw()
        // -- letting the expected native failure happen lets this test check that state anyway.
        Record.Exception(() => model.Draw(world, view, projection));

        Assert.Equal(bone.Transform * world, effect.World);
        Assert.Equal(view, effect.View);
        Assert.Equal(projection, effect.Projection);
    }

    [Fact]
    public void Draw_MeshWithoutExplicitParentBone_FallsBackToRootNotBoneZero()
    {
        var device = CreateDummyDevice();
        var effect = new RecordingEffect(device);
        var part = new ModelMeshPart(null, null, numVertices: 3, primitiveCount: 1, startIndex: 0, vertexOffset: 0);
        var bone0 = new ModelBone(0, "not-root") { Transform = Matrix.CreateTranslation(100f, 0f, 0f) };
        var bone1 = new ModelBone(1, "root") { Transform = Matrix.CreateTranslation(1f, 2f, 3f) };
        var mesh = new ModelMesh(device, [part]);
        part.Effect = effect;
        // rootBoneIndex: 1 with no meshParentBones -- mesh.ParentBone stays null, so Draw() must
        // fall back to Root (bone1), not position 0 (bone0).
        var model = new Model(device, [bone0, bone1], [mesh], [], rootBoneIndex: 1);

        Record.Exception(() => model.Draw(Matrix.Identity, Matrix.Identity, Matrix.Identity));

        Assert.Equal(bone1.Transform, effect.World);
    }
}
