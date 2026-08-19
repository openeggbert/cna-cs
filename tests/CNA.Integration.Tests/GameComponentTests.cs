using Xunit;
using Xunit.Abstractions;

namespace CNA.Integration.Tests;

/// <summary>
/// Game components, driven by native rather than by a direct call.
///
/// In the own-game collection because a component takes its game in the constructor, so the host
/// game has to be built here rather than borrowed from the shared fixture.
/// </summary>
[Collection(OwnGameCollection.Name)]
public class GameComponentTests(ITestOutputHelper output)
{
    /// <summary>
    /// A component added to Game.Components must actually tick.
    ///
    /// `CnaCApiRuntime.cpp`'s CGame overrides `Update` and `Draw` as
    /// `Invoke(callbacks_.update, ...)` with no `Game::Update(gameTime)` after it -- the only two
    /// overrides in that class that do not chain to the base. `Game::Update` is what walks
    /// `updateableComponents_` and updates each enabled one, so a component added through
    /// `cna_game_components_add` is created, initialized, and then never ticks. Measured: 6 frames,
    /// `initialized=True`, `updated 0 times`. Reported as the sibling of CBIND-063.
    ///
    /// The same omission also skips `FrameworkDispatcher::Update()`, the last statement of
    /// `Game::Update` -- which in XNA is what pumps DynamicSoundEffectInstance buffer refills and
    /// MediaPlayer song transitions. So that is silently dead for every C consumer too.
    ///
    /// <b>Fixed upstream in CBIND-068</b>, which chained both overrides to their base. This test
    /// was skipped rather than rewritten to assert the broken behaviour -- a test that encodes a
    /// defect as expected passes forever and stops being a question -- and un-skipping it is what
    /// verified the fix from this side.
    ///
    /// Not worked around here at the time, deliberately: a managed fallback driving components
    /// itself would have double-updated them the moment the fix landed.
    ///
    /// One consequence of the fix worth knowing: the base pass is skipped when a callback has
    /// already failed, because recording a callback failure calls Exit. So "the frame ran" and
    /// "components ticked" differ in exactly that case and no other.
    /// </summary>
    [NativeFact]
    public void GameComponents_AddedComponentReceivesUpdate()
    {
        using var game = new ComponentHost();

        for (int i = 0; i < 6 && game.Component.Updates == 0; i++)
        {
            game.RunOneFrame();
        }

        output.WriteLine(
            $"component updated {game.Component.Updates} time(s), initialized={game.Component.Initialized}");
        Assert.True(game.Component.Initialized, "The component was never initialized.");
        Assert.True(game.Component.Updates > 0, "The component was added but never updated.");
    }

    private sealed class CountingComponent(CNA.Game game) : CNA.GameComponent(game)
    {
        public int Updates { get; private set; }

        public bool Initialized { get; private set; }

        public override void Initialize() => Initialized = true;

        public override void Update(GameTime gameTime) => Updates++;
    }

    /// <summary>A component takes its game in the constructor, so the host has to build it after
    /// base construction rather than receive one -- which is XNA's own shape.</summary>
    private sealed class ComponentHost : CNA.Game
    {
        public ComponentHost()
        {
            Component = new CountingComponent(this);
            Components.Add(Component);
        }

        public CountingComponent Component { get; }

        private int _frames;

        protected override void Update(GameTime gameTime)
        {
            // Deliberately not exiting on the first frame: a component is updated by the game's own
            // update pass, so exiting immediately could cut it off before it ever ran.
            if (++_frames >= 3)
            {
                Exit();
            }

            base.Update(gameTime);
        }
    }
}
