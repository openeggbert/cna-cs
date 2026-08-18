using CNA.Media;

namespace CNA;

/// <summary>
/// Matches real XNA's <c>FrameworkDispatcher</c>: the per-frame pump for the framework's own
/// asynchronous subsystems. In real XNA a game that never calls this (or never runs a
/// <see cref="Game"/> loop, which calls it internally) sees media events never fire.
///
/// This finally gives <see cref="MediaPlayer.Update"/> its real XNA home. Before Phase 8 WP7,
/// <see cref="Game.Update"/> called <c>MediaPlayer.Update</c> directly and its doc comment called
/// that "the closest equivalent this project can offer" -- an explicitly documented stand-in for
/// exactly this type. <see cref="Game.Update"/> now calls <see cref="Update"/> instead, so a game
/// that drives the framework without a <see cref="Game"/> loop has the same entry point real XNA
/// gives it.
/// </summary>
public static class FrameworkDispatcher
{
    /// <summary>Pumps every framework subsystem that needs periodic servicing. Today that is
    /// media only (song-end detection and queue auto-advance); audio needs none here, since
    /// <c>SoundEffectInstance</c> state is queried from native on demand rather than tracked
    /// managed-side.</summary>
    public static void Update() => MediaPlayer.Update();
}
