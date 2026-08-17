using Xunit;
using XnaModelBone = Microsoft.Xna.Framework.Graphics.ModelBone;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// <see cref="XnaModelBone"/> is the only new-this-pass compat graphics type reachable from this
/// test project: its constructor takes no <c>GraphicsDevice</c>. <c>Model</c>/<c>ModelMesh</c> both
/// need one, and (same limitation already documented on compat <c>VertexBuffer</c>/<c>IndexBuffer</c>/
/// <c>BasicEffect</c>) this project has no <c>InternalsVisibleTo</c> grant to reach compat
/// <c>GraphicsDevice</c>'s <c>protected internal</c> constructor -- so neither is exercisable here.
/// <see cref="Microsoft.Xna.Framework.Graphics.ModelBoneCollection"/>'s own <c>internal</c>
/// constructor is unreachable too, but its full public surface is exercised indirectly through
/// <see cref="XnaModelBone.Children"/>, which real code always reaches it through anyway.
/// </summary>
public class ModelBoneCompatTests
{
    [Fact]
    public void Constructor_SetsIndexAndName()
    {
        var bone = new XnaModelBone(3, "Head");

        Assert.Equal(3, bone.Index);
        Assert.Equal("Head", bone.Name);
    }

    [Fact]
    public void Constructor_ChildrenStartsEmpty()
    {
        var bone = new XnaModelBone(0, "Root");

        Assert.Equal(0, bone.Children.Count);
    }

    [Fact]
    public void Constructor_ParentStartsNull()
    {
        var bone = new XnaModelBone(0, "Root");

        Assert.Null(bone.Parent);
    }

    [Fact]
    public void AddChild_AddsToChildrenAndSetsParent()
    {
        var parent = new XnaModelBone(0, "Root");
        var child = new XnaModelBone(1, "Head");

        parent.AddChild(child);

        Assert.Equal(1, parent.Children.Count);
        Assert.Same(child, parent.Children[0]);
        Assert.Same(parent, child.Parent);
    }

    [Fact]
    public void AddChild_NullChild_ThrowsArgumentNullException()
    {
        var bone = new XnaModelBone(0, "Root");

        Assert.Throws<ArgumentNullException>(() => bone.AddChild(null!));
    }

    [Fact]
    public void Children_NameIndexer_FindsChildByName()
    {
        var parent = new XnaModelBone(0, "Root");
        var child = new XnaModelBone(1, "Head");
        parent.AddChild(child);

        Assert.Same(child, parent.Children["Head"]);
    }

    [Fact]
    public void Children_NameIndexer_UnknownName_ThrowsKeyNotFoundException()
    {
        var parent = new XnaModelBone(0, "Root");

        Assert.Throws<KeyNotFoundException>(() => parent.Children["NoSuchBone"]);
    }

    [Fact]
    public void Children_TryGetValue_FindsAndMissesCorrectly()
    {
        var parent = new XnaModelBone(0, "Root");
        var child = new XnaModelBone(1, "Head");
        parent.AddChild(child);

        Assert.True(parent.Children.TryGetValue("Head", out XnaModelBone? found));
        Assert.Same(child, found);
        Assert.False(parent.Children.TryGetValue("NoSuchBone", out XnaModelBone? missing));
        Assert.Null(missing);
    }

    [Fact]
    public void Children_Contains_ReflectsMembership()
    {
        var parent = new XnaModelBone(0, "Root");
        var child = new XnaModelBone(1, "Head");
        var stranger = new XnaModelBone(2, "Unrelated");
        parent.AddChild(child);

        Assert.True(parent.Children.Contains(child));
        Assert.False(parent.Children.Contains(stranger));
    }

    [Fact]
    public void Children_Enumerable_YieldsEveryChildInOrder()
    {
        var parent = new XnaModelBone(0, "Root");
        var first = new XnaModelBone(1, "Left");
        var second = new XnaModelBone(2, "Right");
        parent.AddChild(first);
        parent.AddChild(second);

        Assert.Equal([first, second], parent.Children);
    }

    [Fact]
    public void AddChild_MultipleChildren_AllGetSameParent()
    {
        var parent = new XnaModelBone(0, "Root");
        var first = new XnaModelBone(1, "Left");
        var second = new XnaModelBone(2, "Right");

        parent.AddChild(first);
        parent.AddChild(second);

        Assert.Same(parent, first.Parent);
        Assert.Same(parent, second.Parent);
    }
}
