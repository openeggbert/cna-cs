using Xunit;
using XnaModelBone = Microsoft.Xna.Framework.Graphics.ModelBone;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// Exercises the internal model-builder seam while separately asserting that XNA consumers cannot
/// see that construction surface.
/// </summary>
public class ModelBoneCompatTests
{
    [Fact]
    public void ConstructionHooks_AreNotPublic()
    {
        Assert.Empty(typeof(XnaModelBone).GetConstructors());
        Assert.Null(typeof(XnaModelBone).GetMethod(
            "AddChild",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance));
    }

    [Fact]
    public void Constructor_SetsIndexAndName()
    {
        XnaModelBone bone = CreateBone(3, "Head");

        Assert.Equal(3, bone.Index);
        Assert.Equal("Head", bone.Name);
    }

    [Fact]
    public void Constructor_ChildrenStartsEmpty()
    {
        XnaModelBone bone = CreateBone(0, "Root");

        Assert.Empty(bone.Children);
    }

    [Fact]
    public void Constructor_ParentStartsNull()
    {
        XnaModelBone bone = CreateBone(0, "Root");

        Assert.Null(bone.Parent);
    }

    [Fact]
    public void AddChild_AddsToChildrenAndSetsParent()
    {
        XnaModelBone parent = CreateBone(0, "Root");
        XnaModelBone child = CreateBone(1, "Head");

        AddChild(parent, child);

        Assert.Single(parent.Children);
        Assert.Same(child, parent.Children[0]);
        Assert.Same(parent, child.Parent);
    }

    [Fact]
    public void AddChild_NullChild_ThrowsArgumentNullException()
    {
        XnaModelBone bone = CreateBone(0, "Root");

        Assert.Throws<ArgumentNullException>(() => AddChild(bone, null!));
    }

    [Fact]
    public void Children_NameIndexer_FindsChildByName()
    {
        XnaModelBone parent = CreateBone(0, "Root");
        XnaModelBone child = CreateBone(1, "Head");
        AddChild(parent, child);

        Assert.Same(child, parent.Children["Head"]);
    }

    [Fact]
    public void Children_NameIndexer_UnknownName_ThrowsKeyNotFoundException()
    {
        XnaModelBone parent = CreateBone(0, "Root");

        Assert.Throws<KeyNotFoundException>(() => parent.Children["NoSuchBone"]);
    }

    [Fact]
    public void Children_TryGetValue_FindsAndMissesCorrectly()
    {
        XnaModelBone parent = CreateBone(0, "Root");
        XnaModelBone child = CreateBone(1, "Head");
        AddChild(parent, child);

        Assert.True(parent.Children.TryGetValue("Head", out XnaModelBone? found));
        Assert.Same(child, found);
        Assert.False(parent.Children.TryGetValue("NoSuchBone", out XnaModelBone? missing));
        Assert.Null(missing);
    }

    [Fact]
    public void Children_Contains_ReflectsMembership()
    {
        XnaModelBone parent = CreateBone(0, "Root");
        XnaModelBone child = CreateBone(1, "Head");
        XnaModelBone stranger = CreateBone(2, "Unrelated");
        AddChild(parent, child);

        Assert.Contains(child, parent.Children);
        Assert.DoesNotContain(stranger, parent.Children);
    }

    [Fact]
    public void Children_Enumerable_YieldsEveryChildInOrder()
    {
        XnaModelBone parent = CreateBone(0, "Root");
        XnaModelBone first = CreateBone(1, "Left");
        XnaModelBone second = CreateBone(2, "Right");
        AddChild(parent, first);
        AddChild(parent, second);

        Assert.Equal([first, second], parent.Children);
    }

    [Fact]
    public void AddChild_MultipleChildren_AllGetSameParent()
    {
        XnaModelBone parent = CreateBone(0, "Root");
        XnaModelBone first = CreateBone(1, "Left");
        XnaModelBone second = CreateBone(2, "Right");

        AddChild(parent, first);
        AddChild(parent, second);

        Assert.Same(parent, first.Parent);
        Assert.Same(parent, second.Parent);
    }

    private static XnaModelBone CreateBone(int index, string name) =>
        (XnaModelBone)Activator.CreateInstance(
            typeof(XnaModelBone),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [index, name],
            culture: null)!;

    private static void AddChild(XnaModelBone parent, XnaModelBone child)
    {
        MethodInfo method = typeof(XnaModelBone).GetMethod(
            "AddChild",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            method.Invoke(parent, [child]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }
}
