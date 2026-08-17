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

    /// <summary>Binds the active vertex buffer for subsequent <c>Draw*</c> calls, or unbinds when
    /// <paramref name="vertexBuffer"/> is <see cref="CnaHandle.Zero"/>. Same grounding as
    /// <c>VertexBuffer</c> itself -- no doc-backed shape, shaped to match the real
    /// openeggbert/cna C++ engine's own (not yet C-ABI-exposed) implementation.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_vertex_buffer(CnaHandle device, CnaHandle vertexBuffer);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_indices(CnaHandle device, CnaHandle indexBuffer);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_draw_primitives(
        CnaHandle device,
        int primitiveType,
        int startVertex,
        int primitiveCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_draw_indexed_primitives(
        CnaHandle device,
        int primitiveType,
        int baseVertex,
        int startIndex,
        int primitiveCount);

    /// <summary>Pushes <see cref="CnaBasicEffectParams"/> as pending per-draw effect state for the
    /// next <c>Draw*</c> call to use -- see that struct's own doc comment for the full grounding
    /// (a reduced, faithfully-derived subset of the real C++ engine's internal
    /// <c>GpuDrawParams</c>, not invented from nothing).</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_apply_basic_effect(CnaHandle device, in CnaBasicEffectParams effectParams);

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

    // -- SpriteBatch (§22) ---------------------------------------------------------------------
    //
    // Phase 5 (plan.md): every Draw/DrawString call buffers a CnaSpriteDrawCommand in managed code
    // instead of calling native immediately; End() flushes the whole batch through one
    // cna_sprite_batch_draw_many call, matching §22's own "managed draw-command buffer, one or a
    // few native calls" example exactly. This replaced the original per-draw
    // cna_sprite_batch_draw/cna_sprite_batch_draw_ex calls entirely (removed here, not kept
    // alongside the batched form) -- once every draw funnels through the same buffer, a
    // single-draw native primitive has no remaining caller.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sprite_batch_create(CnaHandle device, out CnaHandle spriteBatch);

    [LibraryImport(LibraryName)]
    internal static partial void cna_sprite_batch_release(CnaHandle spriteBatch);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sprite_batch_begin(CnaHandle spriteBatch);

    /// <summary>Matches the <c>CNA_SpriteDrawCommand</c>/<c>cna_sprite_batch_draw_many</c>
    /// example in ../../cnabinding/analysis_binding.md §22 exactly -- the one place in this
    /// project's ABI surface where the analysis docs already specified the batched shape directly,
    /// not just a naming convention to extrapolate from.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_sprite_batch_draw_many(
        CnaHandle spriteBatch,
        CnaSpriteDrawCommand* commands,
        nuint commandCount);

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

    /// <summary>No ABI shape for this exists upstream -- self-designed for this repository, see
    /// <see cref="CnaSpriteFontData"/>.</summary>
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial CnaResult cna_content_load_soundeffect(
        CnaHandle content,
        string assetName,
        out CnaHandle soundEffect);

    // -- SoundEffect / SoundEffectInstance -----------------------------------------------------
    //
    // No ABI shape for audio exists in the analysis docs at all (confirmed by a full-text grep of
    // both -- unlike SpriteBatch's §22, audio gets no concrete struct anywhere, just class names
    // to preserve and one "cna_audio_*" naming-convention bullet). This whole surface is
    // self-designed, but with better grounding than RenderTarget2D/GamePadCapabilities had: the
    // real openeggbert/cna C++ engine already has a working (if not yet C-ABI-exposed)
    // Microsoft::Xna::Framework::Audio::SoundEffect/SoundEffectInstance implementation over
    // SDL3_mixer (modules/audio/include/Microsoft/Xna/Framework/Audio/). Every function/parameter
    // here is deliberately shaped to match that real C++ class's actual method surface and
    // documented semantics (Volume/Pitch pass through unclamped; Pan validates to [-1,1] and
    // IsLooped's "already played" check happen in managed code before reaching native, matching
    // where the real C++ implementation itself performs them -- see SoundEffectInstance.cs) --
    // this is this repository's best guess at what a future cna_soundeffect_* C API would need to
    // expose over that existing implementation, not a guess made from nothing.

    /// <summary>Raw PCM audio must be headerless, little-endian, signed 16-bit samples -- not a
    /// WAV/RIFF file and not an XNB asset, matching the real C++ SoundEffect(byte[], ...)
    /// constructor's own documented requirement exactly.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_soundeffect_create(
        byte* data,
        nuint byteLength,
        int sampleRate,
        int channels,
        int loopStart,
        int loopLength,
        out CnaHandle soundEffect);

    [LibraryImport(LibraryName)]
    internal static partial void cna_soundeffect_release(CnaHandle soundEffect);

    [LibraryImport(LibraryName)]
    internal static partial long cna_soundeffect_get_duration_ticks(CnaHandle soundEffect);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_soundeffectinstance_create(CnaHandle soundEffect, out CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial void cna_soundeffectinstance_release(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_soundeffectinstance_play(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_soundeffectinstance_pause(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_soundeffectinstance_resume(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_soundeffectinstance_stop(CnaHandle instance, byte immediate);

    [LibraryImport(LibraryName)]
    internal static partial int cna_soundeffectinstance_get_state(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial float cna_soundeffectinstance_get_volume(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_soundeffectinstance_set_volume(CnaHandle instance, float volume);

    [LibraryImport(LibraryName)]
    internal static partial float cna_soundeffectinstance_get_pitch(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_soundeffectinstance_set_pitch(CnaHandle instance, float pitch);

    [LibraryImport(LibraryName)]
    internal static partial float cna_soundeffectinstance_get_pan(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_soundeffectinstance_set_pan(CnaHandle instance, float pan);

    [LibraryImport(LibraryName)]
    internal static partial byte cna_soundeffectinstance_get_is_looped(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_soundeffectinstance_set_is_looped(CnaHandle instance, byte looped);

    // -- VertexBuffer / IndexBuffer ------------------------------------------------------------
    //
    // Same situation as audio: no ABI shape for either exists in the analysis docs (confirmed by
    // grep -- neither struct nor a naming-convention bullet, unlike audio's "cna_audio_*"
    // mention), but the real openeggbert/cna C++ engine's modules/graphics/ already has full,
    // tested, renderer-backend-wired VertexBuffer/IndexBuffer implementations (a std::unique_ptr
    // to a renderer-owned GPU handle plus a CPU-side "shadow" byte buffer enabling GetData()
    // readback) -- every function here is shaped to match that real implementation, not invented
    // from nothing. See CNA.Framework's VertexBuffer.cs/IndexBuffer.cs for the object-model side.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_vertexbuffer_create(
        CnaHandle device,
        int vertexStride,
        int vertexCount,
        int bufferUsage,
        out CnaHandle vertexBuffer);

    [LibraryImport(LibraryName)]
    internal static partial void cna_vertexbuffer_release(CnaHandle vertexBuffer);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_vertexbuffer_set_data(
        CnaHandle vertexBuffer,
        int offsetInBytes,
        byte* data,
        nuint byteLength,
        int vertexStride);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_vertexbuffer_get_data(
        CnaHandle vertexBuffer,
        int offsetInBytes,
        byte* data,
        nuint byteLength,
        int vertexStride);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_indexbuffer_create(
        CnaHandle device,
        int indexElementSize,
        int indexCount,
        int bufferUsage,
        out CnaHandle indexBuffer);

    [LibraryImport(LibraryName)]
    internal static partial void cna_indexbuffer_release(CnaHandle indexBuffer);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_indexbuffer_set_data(
        CnaHandle indexBuffer,
        int offsetInBytes,
        byte* data,
        nuint byteLength);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_indexbuffer_get_data(
        CnaHandle indexBuffer,
        int offsetInBytes,
        byte* data,
        nuint byteLength);

    // -- MediaPlayer (Microsoft.Xna.Framework.Media) --------------------------------------------
    //
    // No ABI shape for music/media playback exists in the analysis docs at all (confirmed by a
    // full-text grep of both, same as audio). Better grounded than that makes it sound, same
    // reasoning as SoundEffect above: the real openeggbert/cna C++ engine already has a working
    // (if not yet C-ABI-exposed) Microsoft::Xna::Framework::Media::MediaPlayer implementation over
    // SDL3_mixer (modules/media/), and these six functions are shaped to match its actual
    // Play/Pause/Resume/Stop/Volume/Muted semantics. MediaPlayer is process-global/static in real
    // XNA (not tied to a GraphicsDevice or any other handle) -- these take no CnaHandle parameter,
    // matching the existing Keyboard/Mouse/GamePad state calls' own no-handle shape rather than
    // inventing a new calling convention for this project's first static-subsystem native surface.
    // State/Volume/IsMuted/PlayPosition are deliberately NOT native calls: the real C++ engine's
    // own MediaPlayer tracks all of that in plain C++ static state (state_/volume_/a chrono-based
    // timer), not by querying the audio backend, so CNA.Media.MediaPlayer reproduces that as plain
    // C# static state too -- see MediaPlayer.cs.

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial CnaResult cna_mediaplayer_play(string filePath);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_mediaplayer_pause();

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_mediaplayer_resume();

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_mediaplayer_stop();

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_mediaplayer_set_volume(float volume);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_mediaplayer_set_muted(byte muted);

    // Visualization capture/FFT (real openeggbert/cna implementation:
    // modules/media/src/Internal/VisualizationCapture.cpp + VisualizationFFT.cpp) is real work done
    // entirely in native code -- a lock-free ring buffer fed from SDL3_mixer's post-mix callback,
    // and a from-scratch 512-point FFT over it -- so, unlike State/Volume/IsMuted above, this needs
    // a real native round trip every call, no local C# cache possible for the data itself.
    // IsVisualizationEnabled's own get/set split still matches State/Volume's pattern: the native
    // call installs/removes the real post-mix callback (a real, meaningful side effect, avoided
    // entirely when disabled), but the flag value itself is cached in C# afterward, matching
    // Volume/IsMuted's own "call native on write, read the cache" shape.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_mediaplayer_set_visualization_enabled(byte enabled);

    /// <summary>
    /// <paramref name="frequencies"/>/<paramref name="samples"/> are both exactly
    /// <c>CNA.Media.VisualizationData.Size</c> (256) elements -- explicit raw pointers, not a
    /// bundled snapshot struct, matching <c>cna_vertexbuffer_set_data</c>/<c>get_data</c>'s own
    /// "explicit buffer, not a collection" convention for bulk binary data
    /// (<c>analysis_binding_sharp_runtime.md</c> §40) rather than inventing a new pattern for
    /// exactly two fixed-size arrays. <paramref name="count"/> is <see cref="nuint"/>, matching
    /// <c>cna_vertexbuffer_get_data</c>/<c>cna_indexbuffer_get_data</c>'s own length-parameter type
    /// exactly (a code-review finding: an earlier version used <see langword="int"/> here despite
    /// this doc comment's own claim to match that convention -- a real mismatch, not just cosmetic,
    /// since a native side declaring the equivalent parameter as <c>size_t</c> would read
    /// undefined upper bytes from a 4-byte argument under the platform calling convention).
    /// </summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_mediaplayer_get_visualization_data(float* frequencies, float* samples, nuint count);
}
