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

    // -- Error retrieval (real ABI, core.h:154-178, caller-owned two-call size-then-copy) ----

    /// <summary>Matches <c>cna_error_get_last_message_size</c> exactly (<c>core.h:163</c>) --
    /// the earlier <c>cna_get_last_error_message_length</c> guess returned the byte count
    /// directly instead of through a <see cref="CnaResult"/> out-param; the real function has no
    /// equivalent by that name. See <c>CnaError.GetLastErrorMessage</c>.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_error_get_last_message_size(out ulong outBytes);

    /// <summary>Matches <c>cna_error_copy_last_message</c> exactly (<c>core.h:175</c>) -- same
    /// correction as <see cref="cna_error_get_last_message_size"/>: returns <see cref="CnaResult"/>
    /// (<c>BufferTooSmall</c> when <paramref name="capacity"/> is insufficient) rather than a
    /// directly-returned byte count, and always reports the required size through
    /// <paramref name="outBytes"/> regardless of outcome.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_error_copy_last_message(byte* destination, ulong capacity, out ulong outBytes);

    // -- Game lifecycle (real ABI, runtime.h) -------------------------------------------------
    //
    // Replaces the old, guessed cna_managed_game_* names/shapes entirely -- none of them exist
    // upstream by those names. The real API splits lifecycle callbacks across two tables:
    // CnaManagedGameCallbacks (load_content/update/draw/unload_content/exiting, passed inside
    // CnaGameCreateInfo at creation) and CnaGameFrameHooks (initialize/begin_run/end_run/
    // begin_draw/end_draw, installed separately via cna_game_set_frame_hooks_ext after creation)
    // -- see Game.cs's constructor and NEXT.md's native-ABI-migration entry.

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_game_create(in CnaGameCreateInfo createInfo, out CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_game_set_frame_hooks_ext(CnaHandle game, in CnaGameFrameHooks hooks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_run(CnaHandle game);

    /// <summary>Steps exactly one native frame instead of blocking until exit -- a distinct real
    /// function <see cref="cna_game_run"/> wraps, with no equivalent in the old guessed API.
    /// Unused today (<c>Game.Run()</c> only calls <see cref="cna_game_run"/>); declared here since
    /// it is part of the real, confirmed ABI surface, matching this file's existing practice of
    /// declaring functions ahead of a managed caller that uses them (see the audio/media sections
    /// below).</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_run_one_frame(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_request_exit(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_destroy(CnaHandle game);

    /// <summary>Only succeeds from inside an active lifecycle callback for <paramref name="game"/>;
    /// the resulting handle is documented as valid only until that callback returns -- see
    /// <c>Game.GetNativeGraphicsDeviceHandle</c>'s own doc comment for how this project currently
    /// (and only partially correctly) handles that constraint.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_get_graphics_device(CnaHandle game, out CnaHandle graphicsDevice);

    /// <summary>Unlike <see cref="cna_game_get_graphics_device"/>, safe to call at any time with an
    /// owned or callback-borrowed <paramref name="game"/> handle -- a game's content manager is a
    /// stable value member, not a per-callback borrow (see <c>content.h</c>).</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_get_content_manager_ext(CnaHandle game, out CnaHandle contentManager);

    // -- GraphicsDevice (real ABI, graphics_device.h) -----------------------------------------
    //
    // Step 3 of the native-ABI migration (see NEXT.md) fixed the confirmed-shape functions below
    // (Clear, SetVertexBuffer, Indices' rename, DrawIndexedPrimitives' real 7-argument arity) and
    // the device-handle-sourcing problem project-wide; step 5 (RenderTarget2D) finished the job by
    // fixing SetRenderTarget's own native call too, once RenderTarget2D had its own real handle
    // type to pass it.

    /// <summary>Matches <c>cna_graphics_device_clear_options</c> exactly
    /// (<c>graphics_device.h:731</c>) -- the real ABI's general clear route; no bare
    /// <c>cna_graphics_device_clear</c> exists at all. See
    /// <c>CNA.Graphics.GraphicsDevice.Clear</c>'s own doc comment for why this specific real
    /// function (of the three real clear routes) is the right match for XNA's simple
    /// <c>Clear(Color)</c> overload.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_clear_options(
        CnaHandle device, CnaClearOptions options, CnaColor color, float depth, int stencil);

    /// <summary>Matches <c>cna_graphics_device_set_render_target2d</c> exactly
    /// (<c>render_target.h:210</c>) -- the old guessed <c>cna_graphics_device_set_render_target</c>
    /// has no real equivalent; render-target binding is type-specific (a separate
    /// <c>_set_render_target_cube</c> also exists, unused here since this project has no cube
    /// render targets). <see cref="CnaHandle.Zero"/> restores the back buffer, same sentinel this
    /// file already used elsewhere before this function's real name was confirmed.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_render_target2d(CnaHandle device, CnaHandle renderTarget);

    /// <summary>Matches <c>cna_graphics_device_set_vertex_buffer</c> exactly
    /// (<c>graphics_device.h:798</c>, "Binds one vertex buffer at vertex offset zero") -- a real,
    /// confirmed name-and-shape match, unlike most of this file before this migration.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_vertex_buffer(CnaHandle device, CnaHandle vertexBuffer);

    /// <summary>Matches <c>cna_graphics_device_set_index_buffer</c> exactly
    /// (<c>graphics_device.h:874</c>) -- the old guessed name was
    /// <c>cna_graphics_device_set_indices</c>; the real function is named for the resource it binds
    /// (<c>index_buffer</c>), not the device property that exposes it (<c>Indices</c>).</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_index_buffer(CnaHandle device, CnaHandle indexBuffer);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_draw_primitives(
        CnaHandle device,
        int primitiveType,
        int startVertex,
        int primitiveCount);

    /// <summary>Matches <c>cna_graphics_device_draw_indexed_primitives</c> exactly
    /// (<c>graphics_device.h:1013</c>) -- real XNA's own full 7-argument signature, confirmed by
    /// reading the header directly. The old guessed shape had only 5 parameters (missing
    /// <paramref name="minVertexIndex"/>/<paramref name="numVertices"/>) despite matching the real
    /// function's *name* exactly -- the clearest concrete case this migration found of a name match
    /// being no evidence of a shape match.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_draw_indexed_primitives(
        CnaHandle device,
        int primitiveType,
        int baseVertex,
        int minVertexIndex,
        int numVertices,
        int startIndex,
        int primitiveCount);

    // -- Effect / BasicEffect (real ABI, effects.h -- step 8 of the native-ABI migration) -----
    //
    // The old cna_graphics_device_apply_basic_effect (a device-scoped "push these 33 fields as
    // pending per-draw state" call) has no real equivalent at all. Real BasicEffect is a full
    // native object: cna_basic_effect_create returns its own CNA_EffectHandle, every property is a
    // real, immediate get/set round trip (not staged client-side state pushed once at Apply time
    // the way the old design needed, since there was no native object to push it to before now),
    // and cna_effect_apply(effect) selects it on its owning device. World/View/Projection route
    // through the shared IEffectMatrices contract (cna_effect_matrices_*); fog through the shared
    // IEffectFog contract (cna_effect_fog_*); ambient color and the three DirectionalLight members
    // through the shared IEffectLights contract (cna_effect_lights_*) -- all three contracts are
    // shared across every stock effect type, not BasicEffect-specific, matching this project's own
    // IEffectMatrices/IEffectFog/IEffectLights interfaces exactly (confirmed against the real C++
    // engine's own interfaces before this migration ever started). Only VertexColorEnabled/
    // PreferPerPixelLighting/DiffuseColor/EmissiveColor/SpecularColor/SpecularPower/Alpha/
    // TextureEnabled/Texture are BasicEffect-specific (cna_basic_effect_*).
    //
    // Each of the three DirectionalLight members is fetched via cna_effect_lights_get_directional_light
    // into its own CNA_DirectionalLightHandle -- confirmed directly against BasicEffectSmoke.c that
    // this handle is independently owned (it stays valid and usable even after the parent effect is
    // destroyed) and must be released with its own cna_directional_light_destroy call, not freed
    // implicitly with the effect. CNA.Graphics.BasicEffect fetches all three once, at construction,
    // and destroys them in its own Dispose -- matching how it now needs to be IDisposable at all
    // (the old design never allocated anything native until Apply(), so had nothing to dispose).

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_destroy(CnaHandle effect);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_apply(CnaHandle effect);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_matrices_get_world(CnaHandle effect, out CnaMatrix outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_matrices_set_world(CnaHandle effect, CnaMatrix value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_matrices_get_view(CnaHandle effect, out CnaMatrix outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_matrices_set_view(CnaHandle effect, CnaMatrix value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_matrices_get_projection(CnaHandle effect, out CnaMatrix outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_matrices_set_projection(CnaHandle effect, CnaMatrix value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_fog_get_color(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_fog_set_color(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_fog_get_enabled(CnaHandle effect, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_fog_set_enabled(CnaHandle effect, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_fog_get_start(CnaHandle effect, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_fog_set_start(CnaHandle effect, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_fog_get_end(CnaHandle effect, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_fog_set_end(CnaHandle effect, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_lights_get_ambient_color(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_lights_set_ambient_color(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_lights_get_directional_light(CnaHandle effect, uint index, out CnaHandle outLight);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_lights_get_enabled(CnaHandle effect, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_lights_set_enabled(CnaHandle effect, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_lights_enable_default(CnaHandle effect);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_create(CnaHandle graphicsDevice, out CnaHandle effect);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_get_vertex_color_enabled(CnaHandle effect, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_set_vertex_color_enabled(CnaHandle effect, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_get_prefer_per_pixel_lighting(CnaHandle effect, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_set_prefer_per_pixel_lighting(CnaHandle effect, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_get_diffuse_color(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_set_diffuse_color(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_get_emissive_color(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_set_emissive_color(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_get_specular_color(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_set_specular_color(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_get_specular_power(CnaHandle effect, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_set_specular_power(CnaHandle effect, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_get_alpha(CnaHandle effect, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_set_alpha(CnaHandle effect, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_get_texture_enabled(CnaHandle effect, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_set_texture_enabled(CnaHandle effect, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_get_texture(CnaHandle effect, out byte outHasTexture, out CnaHandle outTexture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_basic_effect_set_texture(CnaHandle effect, CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_directional_light_destroy(CnaHandle light);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_directional_light_get_diffuse_color(CnaHandle light, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_directional_light_set_diffuse_color(CnaHandle light, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_directional_light_get_direction(CnaHandle light, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_directional_light_set_direction(CnaHandle light, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_directional_light_get_specular_color(CnaHandle light, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_directional_light_set_specular_color(CnaHandle light, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_directional_light_get_enabled(CnaHandle light, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_directional_light_set_enabled(CnaHandle light, byte value);

    // -- Texture2D (real ABI, graphics.h -- step 5 of the native-ABI migration) --------------
    //
    // cna_texture2d_create here (graphics.h) is a different, simpler function from the several
    // creation routes in texture.h (create_standalone/create_from_rgba8/create_cpu_only_rgba8/
    // create_from_file[_with_device]/create_from_encoded_memory) -- this is the one that actually
    // allocates an empty, dimensions-only, device-attached texture, matching real XNA's own
    // new Texture2D(device, width, height). texture.h's own CNA_TextureInfo has no width/height at
    // all; graphics.h's CNA_Texture2DInfo does -- resolves what was an open question before this
    // migration read graphics.h directly.

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_texture2d_create(
        CnaHandle device,
        in CnaTexture2DCreateInfo createInfo,
        out CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_texture2d_destroy(CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_texture2d_get_info(CnaHandle texture, out CnaTexture2DInfo outInfo);

    /// <summary>Matches <c>cna_texture2d_set_data_rgba8</c> exactly (<c>graphics.h:674</c>) --
    /// takes a <c>const CNA_Color*</c> pixel array plus a pixel count, not a raw byte pointer plus
    /// a byte length the way the old guessed shape did.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_texture2d_set_data_rgba8(
        CnaHandle texture,
        CnaColor* pixels,
        ulong pixelCount);

    // -- RenderTarget2D (real ABI, render_target.h -- step 5) ---------------------------------
    //
    // RenderTarget2D is its own real resource type upstream, not a texture created with special
    // usage flags the way this project originally guessed: its own create/get_info/destroy routes,
    // none of which are cna_texture2d_*. CNA.Graphics.RenderTarget2D still subclasses Texture2D
    // (matching real XNA's own RenderTarget2D : Texture2D), but its release/Width/Height now
    // override Texture2D's own (now virtual) implementations to call these functions instead --
    // see RenderTarget2D.cs.

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_render_target2d_create(
        CnaHandle device,
        in CnaRenderTarget2DCreateInfo createInfo,
        out CnaHandle renderTarget);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_render_target_get_info(CnaHandle renderTarget, out CnaRenderTargetInfo outInfo);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_render_target_destroy(CnaHandle renderTarget);

    // -- SpriteBatch (real ABI, graphics.h -- step 7 of the native-ABI migration) -------------
    //
    // The best-preserved subsystem in this whole migration: this project's own pre-migration
    // design already funneled every Draw overload (position-based AND destination-rectangle-based)
    // down to one canonical position+scale form before ever touching the command buffer (see
    // SpriteBatch.cs's own DrawEx overloads) -- which turns out to be exactly
    // CNA_SpriteScaledCommand's own shape, one of the *two* real batched-submission routes
    // (cna_sprite_batch_submit_many takes a destination-rectangle CNA_SpriteCommand instead; this
    // project never needs it, since it already resolves rectangles to position+scale in managed
    // code). CnaSpriteDrawCommand's field order already matched CNA_SpriteScaledCommand's exactly
    // before this migration touched it -- only the struct_size/struct_version header was missing.
    // cna_sprite_batch_create/_end were also already an exact name+shape match. Only
    // cna_sprite_batch_begin (needs a real CNA_SpriteBatchBeginInfo now) and the draw/release names
    // needed fixing.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sprite_batch_create(CnaHandle device, out CnaHandle spriteBatch);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sprite_batch_destroy(CnaHandle spriteBatch);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sprite_batch_begin(CnaHandle spriteBatch, in CnaSpriteBatchBeginInfo beginInfo);

    /// <summary>Matches <c>cna_sprite_batch_submit_scaled_many</c> exactly
    /// (<c>graphics.h:533</c>) -- the old guessed name, <c>cna_sprite_batch_draw_many</c>, has no
    /// real equivalent, but <see cref="CnaSpriteDrawCommand"/>'s own field shape needed only a
    /// <c>struct_size</c>/<c>struct_version</c> header added, not restructuring -- see this file's
    /// own section comment above.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_sprite_batch_submit_scaled_many(
        CnaHandle spriteBatch,
        CnaSpriteDrawCommand* commands,
        ulong commandCount);

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

    // -- ContentManager (real ABI, content.h -- step 6 of the native-ABI migration) -----------
    //
    // Renamed throughout (cna_content_* -> cna_content_manager_*) and switched from
    // StringMarshalling.Utf8 (null-terminated) to CnaStringView (pointer+length) -- see
    // CnaStringMarshal.cs. content_manager itself is now always the handle
    // cna_game_get_content_manager_ext returns (see Game.cs step 2), a borrowed handle safe to
    // reuse across calls -- unaffected by that string-marshaling change.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_manager_set_root_directory(CnaHandle contentManager, CnaStringView rootDirectory);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_manager_load_texture2d(
        CnaHandle contentManager,
        CnaStringView assetName,
        out CnaHandle texture);

    /// <summary>No ABI shape for this exists upstream -- self-designed for this repository, see
    /// <see cref="CnaSpriteFontData"/>. Fails with <see cref="CnaResult"/> (not a silent
    /// truncation) if the asset has more than <see cref="CnaGlyphBuffer.MaxGlyphs"/> glyphs. Not
    /// renamed to the real <c>cna_content_manager_*</c> convention -- there is nothing real to
    /// match, so keeping the old name avoids implying a confirmed shape that doesn't exist.
    /// </summary>
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial CnaResult cna_content_load_spritefont(
        CnaHandle content,
        string assetName,
        out CnaSpriteFontData data);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_manager_load_sound_effect(
        CnaHandle contentManager,
        CnaStringView assetName,
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

    // -- VertexBuffer / IndexBuffer / VertexDeclaration (real ABI, step 4 of the native-ABI ------
    // migration -- vertex_resources.h/index_resources.h). Replaces the old cna_vertexbuffer_*/
    // cna_indexbuffer_* names entirely -- neither exists upstream by those names. VertexDeclaration
    // is now a real native resource of its own (create/destroy/get_stride/copy_elements); a vertex
    // buffer's create call takes a *handle* to one, "copied into the buffer" -- so
    // VertexBuffer.cs builds a declaration, passes it to cna_vertex_buffer_create, and destroys it
    // again immediately, rather than keeping it alive for the vertex buffer's own lifetime.
    //
    // Confirmed directly with cnabinding (openeggbert/cna's own binding author) rather than
    // inferred: no raw-bytes vertex readback route exists anywhere in the ABI (only typed transfer
    // for the 7 built-in CNA_VertexType values) because CNA's own C++ VertexBuffer has no generic
    // GetData<T> either -- 14 concrete typed overloads and nothing else -- so this is not a gap
    // this C binding introduced. IndexBuffer has no such restriction: cna_index_buffer_get_data
    // only selects a 16-/32-bit width, so any 2- or 4-byte unmanaged T maps onto it directly, same
    // as CNA's own C++ IndexBuffer (uint16_t/uint32_t overloads only, which is the whole story for
    // an index type anyway). See CnaVertexIndexResources.cs for the struct shapes and
    // CNA.Framework's VertexBuffer.cs/IndexBuffer.cs for how each is actually used.

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_vertex_declaration_create_with_stride(
        int vertexStride,
        CnaVertexElement* elements,
        ulong elementCount,
        out CnaHandle declaration);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_vertex_declaration_destroy(CnaHandle declaration);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_vertex_buffer_create(
        CnaHandle graphicsDevice,
        in CnaVertexBufferCreateInfo createInfo,
        out CnaHandle vertexBuffer);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_vertex_buffer_destroy(CnaHandle vertexBuffer);

    /// <summary>Matches <c>cna_vertex_buffer_set_data_raw</c> exactly
    /// (<c>vertex_resources.h:346</c>) -- always uploads starting at native buffer vertex zero (no
    /// offset parameter exists in the real function at all); see
    /// <c>CNA.Graphics.VertexBuffer.SetData</c>'s own doc comment for why a nonzero
    /// <c>offsetInBytes</c> can't be honored.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_vertex_buffer_set_data_raw(
        CnaHandle vertexBuffer,
        byte* data,
        ulong dataByteCount,
        ulong vertexCount,
        uint vertexStride);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_index_buffer_create(
        CnaHandle graphicsDevice,
        in CnaIndexBufferCreateInfo createInfo,
        out CnaHandle indexBuffer);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_index_buffer_destroy(CnaHandle indexBuffer);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_index_buffer_set_data(
        CnaHandle indexBuffer,
        in CnaIndexBufferTransfer transfer,
        byte* data,
        ulong capacity);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_index_buffer_get_data(
        CnaHandle indexBuffer,
        in CnaIndexBufferTransfer transfer,
        byte* destination,
        ulong capacity,
        out ulong outElementCount);

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
    /// bundled snapshot struct, matching the old (pre-migration) vertex/index buffer natives' own
    /// "explicit buffer, not a collection" convention for bulk binary data
    /// (<c>analysis_binding_sharp_runtime.md</c> §40) rather than inventing a new pattern for
    /// exactly two fixed-size arrays -- this function itself is not yet part of the native-ABI
    /// migration (see NEXT.md, step 10), so its own shape is still self-designed/unconfirmed.
    /// <paramref name="count"/> is <see cref="nuint"/> (a code-review finding: an earlier version
    /// used <see langword="int"/> here despite this doc comment's own claim to match that convention
    /// -- a real mismatch, not just cosmetic,
    /// since a native side declaring the equivalent parameter as <c>size_t</c> would read
    /// undefined upper bytes from a 4-byte argument under the platform calling convention).
    /// </summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_mediaplayer_get_visualization_data(float* frequencies, float* samples, nuint count);
}
