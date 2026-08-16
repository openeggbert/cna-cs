using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// Raw P/Invoke surface over the CNA stable C ABI (<c>modules/c-api/include/CNA/C/cna.h</c> in
/// <c>openeggbert/cna</c>). This is the *only* file in the solution that names the native
/// library. Nothing here is ergonomic on purpose -- see CNA for the idiomatic layer.
///
/// This is a minimal slice covering exactly what the <c>samples/HelloGame</c> vertical slice
/// needs (clear screen, load a texture, draw with SpriteBatch, read the keyboard, exit cleanly),
/// per the "first .NET milestone" in ../../cnabinding/analysis_binding.md §38. It intentionally
/// does not try to guess the full upstream ABI signature set before that ABI exists -- see
/// plan.md Phase 1 / Phase 4.
///
/// Signatures follow the ABI conventions from analysis_binding.md §8-§16: opaque handles,
/// <see cref="CnaResult"/> instead of exceptions, UTF-8 strings, fixed-width primitives, and
/// explicit buffers instead of collections.
/// </summary>
internal static partial class Native
{
    private const string LibraryName = "cna-native";

    // -- ABI versioning (§14) --------------------------------------------------------------

    [LibraryImport(LibraryName)]
    internal static partial uint cna_get_abi_version();

    // -- Runtime lifecycle (§70) -------------------------------------------------------------

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_runtime_initialize();

    [LibraryImport(LibraryName)]
    internal static partial void cna_runtime_shutdown();

    // -- Error retrieval (§13, caller-owned buffer pattern) ----------------------------------

    [LibraryImport(LibraryName)]
    internal static partial nuint cna_get_last_error_message_length();

    [LibraryImport(LibraryName)]
    internal static unsafe partial nuint cna_copy_last_error_message(byte* buffer, nuint capacity);

    // -- Managed game lifecycle and callback bridge (§20) ------------------------------------

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_managed_game_create(
        in CnaManagedGameCallbacks callbacks,
        nint context,
        out CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_managed_game_run(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial void cna_managed_game_exit(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial void cna_managed_game_release(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial CnaHandle cna_game_get_graphics_device(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial CnaHandle cna_game_get_content(CnaHandle game);

    // -- GraphicsDevice -----------------------------------------------------------------------

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_clear(CnaHandle device, CnaColor color);

    /// <summary>
    /// Sets the active render target, or restores the back buffer when <paramref name="renderTarget"/>
    /// is <see cref="CnaHandle.Zero"/>. No ABI shape exists upstream for this call (or for
    /// render targets at all) -- self-designed for this repository, following the general §8/§9
    /// conventions (opaque handle, <see cref="CnaResult"/> return, zero-handle-as-null sentinel
    /// already used elsewhere in this file). See <c>CNA.Graphics.RenderTarget2D</c>.
    /// </summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_render_target(CnaHandle device, CnaHandle renderTarget);

    // -- Texture2D (§9, §24 SafeHandle-backed resource) --------------------------------------

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_texture2d_create(
        CnaHandle device,
        int width,
        int height,
        out CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial void cna_texture2d_release(CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial int cna_texture2d_get_width(CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial int cna_texture2d_get_height(CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_texture2d_set_data(
        CnaHandle texture,
        byte* data,
        nuint byteLength);

    // -- RenderTarget2D (no upstream ABI shape exists yet; self-designed, see NEXT.md) -------

    /// <summary>
    /// Creates a render-target-usage texture. The resulting handle is released through the
    /// ordinary <see cref="cna_texture2d_release"/> and read back through
    /// <see cref="cna_texture2d_get_width"/>/<see cref="cna_texture2d_get_height"/> -- deliberately
    /// *not* given its own release/getter functions, since <c>CNA.Graphics.RenderTarget2D</c>
    /// subclasses <c>Texture2D</c> and the native handle is texture-shaped either way; only
    /// creation needs render-target-specific usage flags on the native side.
    /// </summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_render_target2d_create(
        CnaHandle device,
        int width,
        int height,
        out CnaHandle renderTarget);

    // -- SpriteBatch (§22 -- DrawMany batching is a Phase 5 addition, not here) --------------

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sprite_batch_create(CnaHandle device, out CnaHandle spriteBatch);

    [LibraryImport(LibraryName)]
    internal static partial void cna_sprite_batch_release(CnaHandle spriteBatch);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sprite_batch_begin(CnaHandle spriteBatch);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sprite_batch_draw(
        CnaHandle spriteBatch,
        CnaHandle texture,
        CnaVector2 position,
        CnaColor color);

    /// <summary>
    /// The full XNA <c>Draw</c> overload family's primitive: source rectangle, rotation, origin,
    /// scale, <c>SpriteEffects</c>, and layer depth, matching the <c>CNA_SpriteDrawCommand</c>
    /// example struct in ../../cnabinding/analysis_binding.md §22 field-for-field. See
    /// <see cref="CnaSpriteDrawCommand"/> for why this is a single-draw call, not the batched
    /// <c>cna_sprite_batch_draw_many</c> the doc example illustrates.
    /// </summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sprite_batch_draw_ex(
        CnaHandle spriteBatch,
        in CnaSpriteDrawCommand command);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sprite_batch_end(CnaHandle spriteBatch);

    // -- Keyboard (§25 snapshot pattern) ------------------------------------------------------

    [LibraryImport(LibraryName)]
    internal static partial void cna_keyboard_get_state(out CnaKeyboardState state);

    // -- Mouse (§25 snapshot pattern) ---------------------------------------------------------

    [LibraryImport(LibraryName)]
    internal static partial void cna_mouse_get_state(out CnaMouseState state);

    // -- GamePad (§25 snapshot pattern) -------------------------------------------------------

    [LibraryImport(LibraryName)]
    internal static partial void cna_gamepad_get_state(int playerIndex, out CnaGamePadState state);

    /// <summary>No ABI shape exists upstream for this call -- self-designed for this repository,
    /// see <see cref="CnaGamePadCapabilities"/>.</summary>
    [LibraryImport(LibraryName)]
    internal static partial void cna_gamepad_get_capabilities(int playerIndex, out CnaGamePadCapabilities capabilities);

    // -- ContentManager (§26, §44) -------------------------------------------------------------

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial CnaResult cna_content_set_root_directory(CnaHandle content, string rootDirectory);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial CnaResult cna_content_load_texture2d(
        CnaHandle content,
        string assetName,
        out CnaHandle texture);

    /// <summary>No ABI shape for this exists upstream -- self-designed for this repository, see
    /// <see cref="CnaSpriteFontData"/>. Fails with <see cref="CnaResult"/> (not a silent
    /// truncation) if the asset has more than <see cref="CnaGlyphBuffer.MaxGlyphs"/> glyphs.
    /// </summary>
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial CnaResult cna_content_load_spritefont(
        CnaHandle content,
        string assetName,
        out CnaSpriteFontData data);
}
