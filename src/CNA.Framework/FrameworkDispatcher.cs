using CNA.Interop;
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
    /// <summary>
    /// Pumps every framework subsystem that needs periodic servicing.
    ///
    /// Forwards to <c>cna_framework_dispatcher_update</c>, the canonical pump, rather than calling
    /// <see cref="MediaPlayer.Update"/> directly as it used to. The native route is what the engine
    /// itself services -- media today, and whatever it services tomorrow -- so calling it means this
    /// does not have to be updated every time the engine grows a subsystem. A sweep of unbound
    /// header functions found it.
    ///
    /// <see cref="MediaPlayer.Update"/> stays public and still works; a game that only wants the
    /// media pump can call it.
    /// </summary>
    public static void Update()
    {
        CnaResult result = Native.cna_framework_dispatcher_update(CnaAmbientGame.Current);
        CnaException.ThrowIfFailed(result, nameof(Update));
    }
}
