using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;
using Xunit;

namespace CNA.XnaCompat.Tests;

public class ModelFacadeContractTests
{
    [Theory]
    [InlineData(typeof(Model))]
    [InlineData(typeof(ModelBone))]
    [InlineData(typeof(ModelMesh))]
    [InlineData(typeof(ModelMeshPart))]
    public void ModelObjects_HaveObjectBaseAndNoPublicConstructors(Type type)
    {
        Assert.Equal(typeof(object), type.BaseType);
        Assert.Empty(type.GetConstructors());
    }

    [Theory]
    [InlineData(typeof(ModelBoneCollection), typeof(ReadOnlyCollection<ModelBone>))]
    [InlineData(typeof(ModelMeshCollection), typeof(ReadOnlyCollection<ModelMesh>))]
    [InlineData(typeof(ModelMeshPartCollection), typeof(ReadOnlyCollection<ModelMeshPart>))]
    [InlineData(typeof(ModelEffectCollection), typeof(ReadOnlyCollection<Effect>))]
    public void ModelCollections_HaveExactReadOnlyCollectionBase(Type type, Type expectedBase)
    {
        Assert.Equal(expectedBase, type.BaseType);
        Assert.NotNull(type.GetNestedType("Enumerator"));
    }
}
