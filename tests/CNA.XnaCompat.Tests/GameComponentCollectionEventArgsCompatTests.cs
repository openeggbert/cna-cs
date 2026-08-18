using Xunit;
using XnaEventArgs = Microsoft.Xna.Framework.GameComponentCollectionEventArgs;

namespace CNA.XnaCompat.Tests;

/// <summary>
/// A member's *name* is part of the compat contract exactly as much as its type is, and nothing
/// else in this repository catches a wrong one: the compat assembly compiles happily against any
/// name it chose itself. Only a ported game notices, at which point the error is in the game.
///
/// This type carried <c>Component</c> until a member-level diff against the engine's own headers
/// flagged it. Real XNA calls it <c>GameComponent</c>, so <c>e.GameComponent</c> is what a handler
/// written against XNA reads.
///
/// The pin is the property access below. It is a compile-time assertion first and a runtime one
/// second -- renaming the property back breaks the build of this test, which is the point.
/// </summary>
public class GameComponentCollectionEventArgsCompatTests
{
    private sealed class StubComponent : CNA.IGameComponent
    {
        public void Initialize()
        {
        }
    }

    [Fact]
    public void GameComponent_IsNamedAsRealXnaNamesIt()
    {
        var component = new StubComponent();

        var args = new XnaEventArgs(component);

        Assert.Same(component, args.GameComponent);
    }

    /// <summary>Public, as in real XNA. An <c>internal</c> constructor compiles here and leaves a
    /// ported game unable to raise the event itself.</summary>
    [Fact]
    public void Constructor_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new XnaEventArgs(null!));
    }
}
