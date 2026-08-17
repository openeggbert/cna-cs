namespace CNA.Interop;

/// <summary>
/// Holds the one currently-running game's native handle so the static, parameterless
/// <c>Keyboard</c>/<c>Mouse</c>/<c>GamePad</c>/<c>MediaPlayer</c> classes have somewhere to read a
/// <c>CNA_Handle game</c> from -- every real route in <c>audio.h</c>/<c>media_player.h</c>/
/// <c>input.h</c> requires one as its first parameter, with no parameterless alternative anywhere,
/// but those classes are deliberately static to match real XNA's own static API shape (a real
/// compatibility requirement, not an implementation detail -- see the "one genuinely blocking
/// architectural decision" in <c>NEXT.md</c>'s native-ABI-migration entry). <see cref="Game"/> sets
/// <see cref="Current"/> once its native handle exists and clears it on disposal; a plain static
/// field (not thread-local, not a stack) is correct here because this project, like real XNA
/// itself, assumes exactly one game runs per process -- the same assumption the static
/// input/media classes already make.
/// </summary>
internal static class CnaAmbientGame
{
    public static CnaHandle Current { get; set; } = CnaHandle.Zero;
}
