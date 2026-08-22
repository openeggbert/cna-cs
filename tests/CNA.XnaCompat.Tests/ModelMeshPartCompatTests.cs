using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Xunit;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// ModelMeshPart instances are created by the content/model runtime in XNA. Its constructors and
/// SetXxx implementation hooks are therefore deliberately not part of the public contract.
/// These checks protect the facade from exposing the CNA implementation surface again.
/// </summary>
public class ModelMeshPartCompatTests
{
    [Fact]
    public void Constructors_AreNotPublic()
    {
        Assert.Empty(typeof(ModelMeshPart).GetConstructors());
    }

    [Theory]
    [InlineData(nameof(ModelMeshPart.VertexBuffer), typeof(VertexBuffer), false)]
    [InlineData(nameof(ModelMeshPart.IndexBuffer), typeof(IndexBuffer), false)]
    [InlineData(nameof(ModelMeshPart.Effect), typeof(Effect), true)]
    [InlineData(nameof(ModelMeshPart.Tag), typeof(object), true)]
    public void ResourceProperties_UseCompatTypesAndExpectedSetterVisibility(
        string name,
        Type expectedType,
        bool hasPublicSetter)
    {
        var property = typeof(ModelMeshPart).GetProperty(
            name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.NotNull(property);
        Assert.Equal(expectedType, property.PropertyType);
        Assert.Equal(hasPublicSetter, property.SetMethod?.IsPublic == true);
    }
}
