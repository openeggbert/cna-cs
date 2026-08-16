using CNA.Graphics;
using Xunit;

namespace CNA.Tests;

/// <summary>
/// <see cref="ModelMeshPart.Effect"/>'s setter auto-maintains its parent <see cref="ModelMesh"/>'s
/// <see cref="ModelMesh.Effects"/> collection -- pure managed logic, no native dependency,
/// reproduced from the real openeggbert/cna C++ engine's own <c>ModelMeshPart::setEffectProperty</c>
/// (see that property's own doc comment). Fully testable without a real <c>cna-native</c>.
/// </summary>
public class ModelMeshPartTests
{
    private static GraphicsDevice CreateDummyDevice() => new(nativeHandleValue: 0);

    private sealed class NoOpEffect(GraphicsDevice graphicsDevice) : Effect(graphicsDevice)
    {
        protected override void OnApply()
        {
        }
    }

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var part = new ModelMeshPart(null, null, numVertices: 4, primitiveCount: 2, startIndex: 1, vertexOffset: 3);

        Assert.Equal(4, part.NumVertices);
        Assert.Equal(2, part.PrimitiveCount);
        Assert.Equal(1, part.StartIndex);
        Assert.Equal(3, part.VertexOffset);
        Assert.Null(part.Effect);
        Assert.Null(part.VertexBuffer);
        Assert.Null(part.IndexBuffer);
    }

    [Fact]
    public void Setters_UpdateCorrespondingProperties()
    {
        var part = new ModelMeshPart();

        part.SetNumVertices(10);
        part.SetPrimitiveCount(5);
        part.SetStartIndex(2);
        part.SetVertexOffset(1);

        Assert.Equal(10, part.NumVertices);
        Assert.Equal(5, part.PrimitiveCount);
        Assert.Equal(2, part.StartIndex);
        Assert.Equal(1, part.VertexOffset);
    }

    [Fact]
    public void Effect_Setter_AddsToMeshEffects()
    {
        var device = CreateDummyDevice();
        var effect = new NoOpEffect(device);
        var part = new ModelMeshPart();
        var mesh = new ModelMesh(device, [part]);

        part.Effect = effect;

        Assert.Same(effect, part.Effect);
        Assert.Equal(1, mesh.Effects.Count);
        Assert.True(mesh.Effects.Contains(effect));
    }

    [Fact]
    public void Effect_Setter_SameEffectOnMultipleParts_EffectListedOnce()
    {
        var device = CreateDummyDevice();
        var effect = new NoOpEffect(device);
        var partA = new ModelMeshPart();
        var partB = new ModelMeshPart();
        var mesh = new ModelMesh(device, [partA, partB]);

        partA.Effect = effect;
        partB.Effect = effect;

        Assert.Equal(1, mesh.Effects.Count);
    }

    [Fact]
    public void Effect_Setter_RemovingLastUser_RemovesFromMeshEffects()
    {
        var device = CreateDummyDevice();
        var effect = new NoOpEffect(device);
        var part = new ModelMeshPart();
        var mesh = new ModelMesh(device, [part]);
        part.Effect = effect;

        part.Effect = null;

        Assert.Equal(0, mesh.Effects.Count);
        Assert.False(mesh.Effects.Contains(effect));
    }

    [Fact]
    public void Effect_Setter_OtherPartStillUsingEffect_EffectStaysInMeshEffects()
    {
        var device = CreateDummyDevice();
        var sharedEffect = new NoOpEffect(device);
        var otherEffect = new NoOpEffect(device);
        var partA = new ModelMeshPart();
        var partB = new ModelMeshPart();
        var mesh = new ModelMesh(device, [partA, partB]);
        partA.Effect = sharedEffect;
        partB.Effect = sharedEffect;

        partA.Effect = otherEffect;

        Assert.True(mesh.Effects.Contains(sharedEffect));
        Assert.True(mesh.Effects.Contains(otherEffect));
        Assert.Equal(2, mesh.Effects.Count);
    }

    [Fact]
    public void Effect_Setter_BeforePartHasParent_DoesNotRegisterOnMesh()
    {
        var device = CreateDummyDevice();
        var effect = new NoOpEffect(device);
        var part = new ModelMeshPart { Effect = effect };

        var mesh = new ModelMesh(device, [part]);

        Assert.Same(effect, part.Effect);
        Assert.Equal(0, mesh.Effects.Count);
    }

    [Fact]
    public void Effect_Setter_SettingSameEffectAgain_IsNoOp()
    {
        var device = CreateDummyDevice();
        var effect = new NoOpEffect(device);
        var part = new ModelMeshPart();
        var mesh = new ModelMesh(device, [part]);
        part.Effect = effect;

        part.Effect = effect;

        Assert.Equal(1, mesh.Effects.Count);
    }
}
