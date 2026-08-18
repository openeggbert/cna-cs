using CNA.Interop;

namespace CNA;

/// <summary>
/// The base directory the title's content is resolved against -- real XNA's
/// <c>Microsoft.Xna.Framework.TitleLocation</c>, which FNA also ships. A
/// <see cref="Content.ContentManager"/> with a relative <c>RootDirectory</c> resolves against this,
/// so a game that needs to open a file next to its own assets asks here rather than assuming the
/// process working directory.
///
/// This type was missing entirely, and the member-level diff could not have found it: that diff
/// walks the C++ headers and compares members of types that exist on *both* sides, so a type
/// absent from C# is skipped rather than reported. A whole-type sweep is what surfaced it. See
/// <c>plan.md</c>'s "Coverage: how it is measured" for the two blind spots this makes three of.
///
/// Static, matching XNA and matching the C++ class (whose constructor is <c>= delete</c>).
/// </summary>
public static class TitleLocation
{
    /// <summary>
    /// The title's base content path.
    ///
    /// <c>cna_title_location_get_path_size</c>/<c>_copy_path</c> both take a game handle, but the
    /// header is explicit that it is "taken for thread affinity only" -- the accessor behind it is
    /// static and resolves the executable's directory on first use. So this reads the ambient game
    /// the same way <c>GraphicsAdapter.Adapters</c> and <c>MediaSource</c> do, and stays a static
    /// property as in XNA rather than growing a <c>Game</c> parameter the ABI does not actually
    /// need.
    ///
    /// Not cached. The C++ side exposes a <c>CNAEXT setPathProperty</c> for tests and custom
    /// launchers, so the value is not guaranteed constant for the process lifetime, and caching it
    /// here would make a launcher's override invisible.
    /// </summary>
    /// <exception cref="CnaException">If no game is active -- the handle is required even though
    /// the path itself is not game-scoped.</exception>
    public static unsafe string Path =>
        NativeStringReader.Read(
            static (CnaHandle game, out ulong bytes) => Native.cna_title_location_get_path_size(game, out bytes),
            static (CnaHandle game, byte* destination, ulong capacity, out ulong bytes) =>
                Native.cna_title_location_copy_path(game, destination, capacity, out bytes),
            CnaAmbientGame.Current,
            nameof(Path));
}
