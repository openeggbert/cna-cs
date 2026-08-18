using CNA.Input.Touch;
using Xunit;

namespace CNA.Framework.Tests;

/// <summary>
/// <see cref="TouchPanel"/> itself needs a real <c>cna-native</c> and so cannot be exercised here,
/// but the snapshot types it hands back are pure managed logic and are fully testable -- the same
/// split this project already applies to <c>KeyboardState</c>/<c>MouseState</c>. What is pinned
/// below is the behaviour most easily got wrong: id-based lookup (indices are not stable across
/// frames, so <see cref="TouchCollection.FindById"/> is the only correct way to track a touch),
/// the throwing mutators that make the XNA-shaped <c>ICollection&lt;T&gt;</c> read-only, and the
/// default-constructed collection, whose backing array is null.
/// </summary>
public class TouchCollectionTests
{
    private static TouchCollection ThreeTouches() => new(
    [
        new TouchLocation(7, TouchLocationState.Pressed, new Vector2(1f, 2f)),
        new TouchLocation(9, TouchLocationState.Moved, new Vector2(3f, 4f)),
        new TouchLocation(4, TouchLocationState.Released, new Vector2(5f, 6f)),
    ]);

    [Fact]
    public void FindById_LocatesRegardlessOfIndex()
    {
        Assert.True(ThreeTouches().FindById(9, out TouchLocation found));

        Assert.Equal(9, found.Id);
        Assert.Equal(TouchLocationState.Moved, found.State);
        Assert.Equal(new Vector2(3f, 4f), found.Position);
    }

    [Fact]
    public void FindById_UnknownId_ReturnsFalse()
    {
        Assert.False(ThreeTouches().FindById(123, out TouchLocation found));
        Assert.Equal(default, found);
    }

    [Fact]
    public void Indexer_AndCount_ReflectTheSnapshot()
    {
        TouchCollection touches = ThreeTouches();

        Assert.Equal(3, touches.Count);
        Assert.Equal(7, touches[0].Id);
        Assert.Equal(4, touches[2].Id);
    }

    [Fact]
    public void IsReadOnly_IsTrue() => Assert.True(ThreeTouches().IsReadOnly);

    [Theory]
    [InlineData("Add")]
    [InlineData("Clear")]
    [InlineData("Remove")]
    public void MutatingMembers_Throw(string member)
    {
        ICollection<TouchLocation> touches = ThreeTouches();
        var item = new TouchLocation(1, TouchLocationState.Pressed, Vector2.Zero);

        Action act = member switch
        {
            "Add" => () => touches.Add(item),
            "Clear" => touches.Clear,
            _ => () => touches.Remove(item),
        };

        Assert.Throws<NotSupportedException>(act);
    }

    /// <summary>A <c>default</c> collection has a null backing array; every read must cope rather
    /// than throwing <see cref="NullReferenceException"/>.</summary>
    [Fact]
    public void Default_IsEmptyAndSafeToRead()
    {
        TouchCollection touches = default;

        Assert.Equal(0, touches.Count);
        Assert.False(touches.FindById(1, out _));
        Assert.Empty(touches);
        Assert.Equal(-1, touches.IndexOf(new TouchLocation(1, TouchLocationState.Pressed, Vector2.Zero)));
    }

    [Fact]
    public void Enumeration_YieldsEveryTouchInOrder() =>
        Assert.Equal([7, 9, 4], ThreeTouches().Select(t => t.Id));

    [Fact]
    public void TryGetPreviousLocation_WithoutAPrevious_ReturnsFalse()
    {
        var touch = new TouchLocation(1, TouchLocationState.Pressed, Vector2.Zero);

        Assert.False(touch.TryGetPreviousLocation(out TouchLocation previous));
        Assert.Equal(TouchLocationState.Invalid, previous.State);
    }

    [Fact]
    public void TryGetPreviousLocation_WithAPrevious_ReturnsIt()
    {
        var touch = new TouchLocation(
            1, TouchLocationState.Moved, new Vector2(10f, 10f), TouchLocationState.Pressed, new Vector2(5f, 5f));

        Assert.True(touch.TryGetPreviousLocation(out TouchLocation previous));
        Assert.Equal(TouchLocationState.Pressed, previous.State);
        Assert.Equal(new Vector2(5f, 5f), previous.Position);
        Assert.Equal(1, previous.Id);
    }

    /// <summary>Equality ignores the previous sample, matching real XNA -- two reads of the same
    /// touch in one frame compare equal even if one carries history and the other does not.</summary>
    [Fact]
    public void Equality_IgnoresPreviousLocation()
    {
        var withoutHistory = new TouchLocation(1, TouchLocationState.Moved, new Vector2(10f, 10f));
        var withHistory = new TouchLocation(
            1, TouchLocationState.Moved, new Vector2(10f, 10f), TouchLocationState.Pressed, new Vector2(5f, 5f));

        Assert.Equal(withoutHistory, withHistory);
        Assert.True(withoutHistory == withHistory);
    }
}
