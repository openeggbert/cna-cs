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

    // -- ABI versioning (abi.h:114) ----------------------------------------------------------

    [LibraryImport(LibraryName)]
    internal static partial uint cna_get_abi_version();

    // There is deliberately no cna_runtime_initialize / cna_runtime_shutdown here. Both were
    // declared, neither exists in any header, and nothing ever called them -- so unlike
    // cna_content_load_spritefont (which was reachable and would have crashed) these were merely
    // dead fabrications. Found by sweeping every declaration in this file against the headers;
    // 713 of 715 matched. The C API has no explicit runtime lifecycle: a game handle is created
    // and destroyed, and that is the whole of it.

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

    /// <summary>Matches <c>cna_game_set_window_title</c> exactly (<c>runtime.h:246</c>) -- takes an
    /// owned game handle, safe to call any time (not callback-scoped), which is why
    /// <c>CNA.GameWindow.Title</c>'s setter can run from a game's own constructor, the same way
    /// <c>HelloGame</c> in cna-cs-template does.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_set_window_title(CnaHandle game, CnaStringView title);

    /// <summary>Matches <c>cna_game_window_get_title_size</c> exactly
    /// (<c>runtime_window.h:126</c>) -- same two-call size-then-copy pattern as
    /// <see cref="cna_error_get_last_message_size"/>, see <c>CNA.GameWindow.Title</c>'s getter.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_window_get_title_size(CnaHandle game, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_game_window_copy_title(CnaHandle game, byte* destination, ulong capacity, out ulong outBytes);

    // The rest of runtime_window.h, added when a sweep of unbound header functions showed
    // GameWindow had exactly one member bound out of a real XNA surface of eight.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_window_get_allow_user_resizing(CnaHandle game, out byte outAllowed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_window_set_allow_user_resizing(CnaHandle game, byte allowed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_window_get_client_bounds(CnaHandle game, out CnaRect outBounds);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_window_get_current_orientation(CnaHandle game, out uint outOrientation);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_window_get_screen_device_name_size(CnaHandle game, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_game_window_copy_screen_device_name(
        CnaHandle game, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_window_begin_screen_device_change(CnaHandle game, byte willBeFullScreen);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_window_end_screen_device_change(
        CnaHandle game, CnaStringView screenDeviceName, int clientWidth, int clientHeight);

    /// <summary>Subscribes to one <c>CNA_GAME_WINDOW_EVENT_*</c>. Released with
    /// <c>cna_game_unsubscribe</c>, like every other runtime registration.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_window_subscribe(
        CnaHandle game, uint eventId, nint callback, nint context, out CnaHandle outRegistration);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_get_is_mouse_visible(CnaHandle game, out byte outVisible);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_set_is_mouse_visible(CnaHandle game, byte visible);

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

    // -- Graphics state (real ABI, graphics_state.h) -------------------------------------------
    //
    // Part of the post-migration "missing XNA surface" push (see NEXT.md): BlendState/
    // DepthStencilState/RasterizerState were guessed-and-never-built during the original
    // migration since the old HelloGame smoke test never touched them. The preset init functions
    // take no device handle at all -- pure value computation, safe to call at any time, including
    // before any game exists -- which is why CNA.Graphics.BlendState/DepthStencilState/
    // RasterizerState's static presets can be plain `static readonly` fields instead of needing
    // lazy/deferred initialization.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_blend_state_init(CnaBlendStatePreset preset, out CnaBlendState outState);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_depth_stencil_state_init(CnaDepthStencilStatePreset preset, out CnaDepthStencilState outState);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_rasterizer_state_init(CnaRasterizerStatePreset preset, out CnaRasterizerState outState);

    /// <summary>Matches <c>cna_graphics_device_get_blend_state</c> exactly
    /// (<c>graphics_state.h:393</c>).</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_get_blend_state(CnaHandle device, out CnaBlendState outState);

    /// <summary>Matches <c>cna_graphics_device_set_blend_state</c> exactly
    /// (<c>graphics_state.h:404</c>) -- <paramref name="state"/> is "copied during the call" per
    /// the header, so a plain <c>in</c> by-value parameter (not <c>ref</c>) is correct here, unlike
    /// this project's self-populating-constructor structs elsewhere.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_blend_state(CnaHandle device, in CnaBlendState state);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_get_depth_stencil_state(CnaHandle device, out CnaDepthStencilState outState);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_depth_stencil_state(CnaHandle device, in CnaDepthStencilState state);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_get_rasterizer_state(CnaHandle device, out CnaRasterizerState outState);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_rasterizer_state(CnaHandle device, in CnaRasterizerState state);

    // -- Texture3D / TextureCube / RenderTargetCube (real ABI, texture_volume.h + render_target.h,
    // Phase 8 WP3b). Each kind has its own create/get_info/destroy triple -- there is no shared
    // texture create or destroy -- which is why CNA.Graphics.Texture leaves ReleaseNative abstract.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_texture3d_create(CnaHandle device, in CnaTexture3DCreateInfo createInfo, out CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_texture3d_get_info(CnaHandle texture, ref CnaTexture3DInfo info);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_texture3d_destroy(CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_texture3d_set_data(
        CnaHandle texture, in CnaTexture3DTransfer transfer, CnaColor* data, ulong dataCapacity);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_texture3d_get_data(
        CnaHandle texture, in CnaTexture3DTransfer transfer, CnaColor* destination, ulong destinationCapacity, out ulong outRequiredElements);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_texturecube_create(CnaHandle device, in CnaTextureCubeCreateInfo createInfo, out CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_texturecube_get_info(CnaHandle texture, ref CnaTextureCubeInfo info);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_texturecube_destroy(CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_texturecube_set_data(
        CnaHandle texture, in CnaTextureCubeTransfer transfer, CnaColor* data, ulong dataCapacity);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_texturecube_get_data(
        CnaHandle texture, in CnaTextureCubeTransfer transfer, CnaColor* destination, ulong destinationCapacity, out ulong outRequiredElements);

    /// <summary>Matches <c>cna_render_target_cube_create</c> exactly
    /// (<c>render_target.h:187</c>). Released through the shared
    /// <see cref="cna_render_target_destroy"/>, same as the 2D form -- render targets are the one
    /// family whose destroy route *is* shared across shapes.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_render_target_cube_create(
        CnaHandle device, in CnaRenderTargetCubeCreateInfo createInfo, out CnaHandle renderTarget);

    /// <summary>Matches <c>cna_graphics_device_set_render_target_cube</c> exactly
    /// (<c>render_target.h</c>) -- the cube counterpart of
    /// <see cref="cna_graphics_device_set_render_target2d"/>, which this project noticed existed
    /// during the native-ABI migration but had no cube render target to call it with until
    /// now.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_render_target_cube(
        CnaHandle device, CnaHandle renderTarget, uint cubeMapFace);

    /// <summary>Replaces the whole active binding array (<c>render_target.h:238</c>). Zero
    /// bindings restores the backbuffer.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_graphics_device_set_render_targets(
        CnaHandle graphicsDevice, CnaRenderTargetBinding* bindings, ulong bindingCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_get_render_target_count(
        CnaHandle graphicsDevice, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_graphics_device_copy_render_targets(
        CnaHandle graphicsDevice, CnaRenderTargetBinding* destination, ulong capacity, out ulong outCount);

    /// <summary>Matches <c>cna_texture_get_info</c> exactly (<c>texture.h:130</c>) -- documented
    /// as accepting a "Texture2D, Texture3D, TextureCube or matching render-target handle", which
    /// is what lets <c>CNA.Graphics.Texture</c> expose <c>LevelCount</c>/<c>Format</c> on the base
    /// class instead of duplicating a per-subclass info call.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_texture_get_info(CnaHandle texture, ref CnaTextureInfo info);

    /// <summary>Matches <c>cna_graphics_device_set_texture</c> exactly
    /// (<c>graphics_device.h:642</c>). <see cref="CnaHandle.Zero"/> empties the slot. Binding
    /// stores no ownership native-side -- a destroyed texture unbinds itself.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_texture(
        CnaHandle device, CnaShaderStage stage, uint slot, CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sampler_state_init(CnaSamplerStatePreset preset, out CnaSamplerState outState);

    /// <summary>Matches <c>cna_graphics_device_get_sampler_state</c> exactly
    /// (<c>graphics_state.h:461</c>) -- unlike the blend/depth/rasterizer trio, a device has two
    /// whole *collections* of these (<paramref name="stage"/>) of
    /// <see cref="CnaSamplerState.MaxSamplers"/> entries each (<paramref name="slot"/>), rather
    /// than one current value.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_get_sampler_state(
        CnaHandle device, CnaShaderStage stage, uint slot, out CnaSamplerState outState);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_sampler_state(
        CnaHandle device, CnaShaderStage stage, uint slot, in CnaSamplerState state);

    /// <summary>Matches <c>cna_graphics_device_get_graphics_profile</c> exactly
    /// (<c>graphics_device.h:368</c>).</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_get_graphics_profile(CnaHandle device, out CnaGraphicsProfile outProfile);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_get_viewport(CnaHandle device, out CnaViewport outViewport);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_viewport(CnaHandle device, CnaViewport viewport);

    /// <summary>Matches <c>cna_graphics_device_draw_user_primitives</c> exactly
    /// (<c>graphics_device.h:1058</c>) -- "no vertex array is retained after the call returns", so
    /// the pinned pointer inside <paramref name="primitives"/> only needs to stay valid for the
    /// duration of this call, matching <c>CNA.Graphics.GraphicsDevice.DrawUserPrimitives</c>'s own
    /// <see langword="fixed"/> block.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_graphics_device_draw_user_primitives(CnaHandle device, in CnaUserPrimitives primitives);

    /// <summary>Matches <c>cna_graphics_device_draw_user_indexed_primitives</c> exactly
    /// (<c>graphics_device.h:1071</c>). Unlike the non-indexed form, <c>num_vertices</c> in the
    /// primitives descriptor is meaningful here -- the header notes it is "used only by the indexed
    /// route".</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_graphics_device_draw_user_indexed_primitives(
        CnaHandle device, in CnaUserPrimitives primitives, in CnaUserIndices indices);

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
    internal static unsafe partial CnaResult cna_texture2d_get_info(CnaHandle texture, ref CnaTexture2DInfo outInfo);

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
    internal static unsafe partial CnaResult cna_render_target_get_info(CnaHandle renderTarget, ref CnaRenderTargetInfo outInfo);

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

    // -- Keyboard / Mouse / GamePad (real ABI, input.h/input_gamepad.h -- step 11 of the --------
    // native-ABI migration, the last one). Every one of these now needs a game handle
    // (CnaAmbientGame.Current) and returns a real CnaResult (the old guessed shapes were void,
    // silently discarding any failure -- an ABI-independent bug this step also fixes: none of
    // these three static classes checked a result at all before this migration, unlike every
    // other native call site in this codebase). Every out-parameter below is `ref`, not `out` --
    // deliberately, not a style choice: these structs are self-populating (see
    // CnaGameFrameHooks's own constructor doc comment), and `ref` makes it a compile error to
    // pass one without constructing it first, unlike `out`, which would silently let a
    // freshly-declared (zero-initialized, so struct_size/version both zero) struct through to a
    // native call that then rejects it with CNA_RESULT_INVALID_ARGUMENT -- caught only because
    // this exact mistake was made and found earlier in this same migration (see NEXT.md's
    // native-ABI-migration entry for this step; cna_texture2d_get_info/cna_render_target_get_info/
    // cna_sound_effect_instance_get_info were fixed the same way, retroactively).

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_keyboard_get_state(CnaHandle game, ref CnaKeyboardState state);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_mouse_get_state(CnaHandle game, ref CnaMouseState state);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_gamepad_get_state(CnaHandle game, uint playerIndex, ref CnaGamePadState state);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_gamepad_get_capabilities(CnaHandle game, uint playerIndex, ref CnaGamePadCapabilities capabilities);

    /// <summary>Rumble (<c>input_gamepad.h</c>). <c>out_applied</c> reports whether the controller
    /// accepted it -- a pad without motors answers false rather than failing, which is what real
    /// XNA's <c>bool</c> return means.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_gamepad_set_vibration(
        CnaHandle game, uint playerIndex, float leftMotor, float rightMotor, out byte outApplied);

    /// <summary>The dead-zone-mode overload of the state capture (<c>input_gamepad.h</c>), matching
    /// real XNA's <c>GamePad.GetState(PlayerIndex, GamePadDeadZone)</c>.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_gamepad_get_state_with_dead_zone(
        CnaHandle game, uint playerIndex, uint deadZoneMode, ref CnaGamePadState outState);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_keyboard_get_state_for_player(
        CnaHandle game, uint playerIndex, ref CnaKeyboardState outState);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_mouse_set_position(CnaHandle game, int x, int y);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_mouse_get_window_handle(CnaHandle game, out ulong outWindow);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_mouse_set_window_handle(CnaHandle game, ulong window);

    /// <summary>The real device query. Distinct from <c>cna_touch_capabilities_init</c>, which only
    /// fills a default value -- see <c>CNA.Input.TouchPanel.GetCapabilities</c>.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_get_capabilities(CnaHandle game, ref CnaTouchCapabilities outCapabilities);

    // -- Content reader extensibility (real ABI, content_readers.h -- Phase 8 WP14) -----------
    //
    // This is the *extensibility* half of the content system: ContentManager.Load<T> covers the
    // built-in types, while a ContentTypeReader lets a game deserialize its own. The reader reads
    // typed values straight off the stream, which is why the value-reading functions are one per
    // type rather than a generic route.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_create(in CnaContentReaderCreateInfo createInfo, out CnaHandle outReader);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_destroy(CnaHandle reader);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_get_content_manager(CnaHandle reader, out CnaHandle outContentManager);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_get_asset_name_size(CnaHandle reader, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_content_reader_copy_asset_name(
        CnaHandle reader, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_get_version(CnaHandle reader, out int outVersion);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_get_platform(CnaHandle reader, out byte outPlatform);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_read_matrix(CnaHandle reader, out CnaMatrix outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_read_quaternion(CnaHandle reader, out CnaQuaternion outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_read_vector2(CnaHandle reader, out CnaVector2 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_read_vector3(CnaHandle reader, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_read_vector4(CnaHandle reader, out CnaVector4 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_read_color(CnaHandle reader, out CnaColor outValue);

    /// <summary>Reports whether the tag introduced an object at all -- an <c>.xnb</c> object graph
    /// encodes null as a tag with no value, which is why this returns a flag rather than an object
    /// reference.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_reader_read_object_tag(CnaHandle reader, out byte outHasValue);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_content_reader_read_bytes_exact(
        CnaHandle reader, int count, CnaStringView readerName, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_type_reader_manager_get_is_registered(CnaStringView canonicalName, out byte outIsRegistered);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_type_reader_manager_create_reader(CnaStringView canonicalName, out CnaHandle outTypeReader);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_type_reader_manager_clear_type_creators();

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_type_reader_destroy(CnaHandle typeReader);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_type_reader_get_can_deserialize_into_existing_object(CnaHandle typeReader, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_type_reader_get_target_type_name_size(CnaHandle typeReader, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_content_type_reader_copy_target_type_name(
        CnaHandle typeReader, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_type_reader_get_type_version(CnaHandle typeReader, out int outVersion);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_type_reader_supports_version(CnaHandle typeReader, int serializedVersion, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_type_reader_initialize(CnaHandle typeReader);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_type_reader_read_untyped(CnaHandle typeReader, CnaHandle reader, out byte outHasValue);

    // -- Game components (real ABI, runtime_components.h -- Phase 8 WP7b) --------------------
    //
    // The native game owns the component collection and drives each component through its own
    // callback table -- which is why this is bound rather than reimplemented managed-side (a
    // managed-only component model would compile and never run; see plan.md WP7).

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_game_component_create(
        CnaHandle game, in CnaGameComponentCallbacks callbacks, out CnaHandle outComponent);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_drawable_game_component_create(
        CnaHandle game, in CnaGameComponentCallbacks callbacks, out CnaHandle outComponent);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_component_destroy(CnaHandle component);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_component_get_enabled(CnaHandle component, out byte outEnabled);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_component_set_enabled(CnaHandle component, byte enabled);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_component_get_update_order(CnaHandle component, out int outOrder);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_component_set_update_order(CnaHandle component, int order);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_component_get_is_drawable(CnaHandle component, out byte outIsDrawable);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_drawable_game_component_get_draw_order(CnaHandle component, out int outOrder);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_drawable_game_component_set_draw_order(CnaHandle component, int order);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_drawable_game_component_get_visible(CnaHandle component, out byte outVisible);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_drawable_game_component_set_visible(CnaHandle component, byte visible);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_components_get_count(CnaHandle game, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_components_get_at(CnaHandle game, ulong index, out CnaHandle outComponent);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_components_add(CnaHandle game, CnaHandle component);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_components_remove(CnaHandle game, CnaHandle component, out byte outRemoved);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_components_remove_at(CnaHandle game, ulong index);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_components_clear(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_components_contains(CnaHandle game, CnaHandle component, out byte outContains);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_components_index_of(CnaHandle game, CnaHandle component, out int outIndex);

    // -- Microphone + DynamicSoundEffectInstance (real ABI, audio.h -- Phase 8 WP11b) ---------
    //
    // Microphones are addressed by index against the game handle, not by their own handle -- the
    // same shape GraphicsAdapter has, and the reason CNA.Audio.Microphone holds an index rather
    // than wrapping a resource.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_get_count(CnaHandle game, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_get_default_index_ext(CnaHandle game, out ulong outIndex, out byte outAvailable);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_get_name_size_at(CnaHandle game, ulong index, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_microphone_copy_name_at(
        CnaHandle game, ulong index, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_get_buffer_duration_ticks_at(CnaHandle game, ulong index, out long outTicks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_set_buffer_duration_ticks_at(CnaHandle game, ulong index, long ticks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_get_is_headset_at(CnaHandle game, ulong index, out byte outHeadset);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_get_sample_rate_at(CnaHandle game, ulong index, out int outSampleRate);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_get_state_at(CnaHandle game, ulong index, out uint outState);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_start_at(CnaHandle game, ulong index);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_stop_at(CnaHandle game, ulong index);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_microphone_get_data_at(
        CnaHandle game, ulong index, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_get_sample_duration_ticks_at(
        CnaHandle game, ulong index, int sizeInBytes, out long outTicks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_get_sample_size_in_bytes_at(
        CnaHandle game, ulong index, long durationTicks, out int outBytes);

    /// <summary>Matches <c>cna_sound_effect_instance_apply_3d</c> exactly
    /// (<c>audio.h:1189</c>) -- positional audio on an *instance*, which is why
    /// <c>SoundEffect.Apply3D</c> creates one rather than being a fire-and-forget call.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_instance_apply_3d(
        CnaHandle instance, in CnaAudioListener listener, in CnaAudioEmitter emitter);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dynamic_sound_effect_instance_create(
        CnaHandle game, int sampleRate, uint channels, out CnaHandle outInstance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dynamic_sound_effect_instance_get_pending_buffer_count(CnaHandle instance, out int outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dynamic_sound_effect_instance_subscribe_buffer_needed(
        CnaHandle instance, nint callback, nint context, out CnaHandle outRegistration);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_unsubscribe_ext(CnaHandle registration);

    /// <summary>Matches <c>cna_microphone_subscribe_buffer_ready_at</c> (<c>audio.h:1017</c>).
    /// Indexed like every other microphone route, because microphones are entries in a runtime list
    /// rather than resources a caller owns. See the sibling manager subscribe for why
    /// <paramref name="callback"/> is <see cref="nint"/>.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_microphone_subscribe_buffer_ready_at(
        CnaHandle game, ulong index, nint callback, nint context, out CnaHandle outRegistration);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_dynamic_sound_effect_instance_submit_buffer(
        CnaHandle instance, byte* bytes, ulong byteCount, int offset, int count);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dynamic_sound_effect_instance_get_sample_duration_ticks(
        CnaHandle instance, int sizeInBytes, out long outTicks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dynamic_sound_effect_instance_get_sample_size_in_bytes(
        CnaHandle instance, long durationTicks, out int outBytes);

    // -- XACT + 3D audio (real ABI, xact.h + audio.h -- Phase 8 WP11a) ------------------------
    //
    // XACT is the authored-audio half of XNA: an AudioEngine loads a project settings file, wave
    // and sound banks load asset files against it, and cues are played by name. Everything here is
    // handle-based except the listener/emitter, which are plain versioned value structs passed by
    // pointer.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_engine_create(CnaHandle game, CnaStringView settingsFile, out CnaHandle outEngine);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_engine_destroy(CnaHandle engine);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_engine_get_is_disposed(CnaHandle engine, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_engine_update(CnaHandle engine);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_engine_get_global_variable(CnaHandle engine, CnaStringView name, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_engine_set_global_variable(CnaHandle engine, CnaStringView name, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_engine_get_category(CnaHandle engine, CnaStringView name, out CnaHandle outCategory);

    // AudioEngine.RendererDetails (xact.h:122-...). Index-addressed rather than a value struct,
    // because a detail carries two strings -- the header says so explicitly.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_engine_get_renderer_count(CnaHandle engine, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_engine_get_renderer_friendly_name_size(
        CnaHandle engine, ulong rendererIndex, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_audio_engine_copy_renderer_friendly_name(
        CnaHandle engine, ulong rendererIndex, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_engine_get_renderer_id_size(
        CnaHandle engine, ulong rendererIndex, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_audio_engine_copy_renderer_id(
        CnaHandle engine, ulong rendererIndex, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_engine_get_renderer_hash_code(
        CnaHandle engine, ulong rendererIndex, out int outHashCode);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_category_destroy(CnaHandle category);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_category_get_name_size(CnaHandle category, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_audio_category_copy_name(CnaHandle category, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_category_pause(CnaHandle category);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_category_resume(CnaHandle category);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_category_set_volume(CnaHandle category, float volume);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_category_stop(CnaHandle category, uint options);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_category_equals(CnaHandle a, CnaHandle b, out byte outEqual);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_audio_category_get_hash_code(CnaHandle category, out int outHashCode);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_wave_bank_create(CnaHandle engine, CnaStringView fileName, out CnaHandle outWaveBank);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_wave_bank_create_streaming(
        CnaHandle engine, CnaStringView fileName, int offset, short packetSize, out CnaHandle outWaveBank);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_wave_bank_destroy(CnaHandle waveBank);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_wave_bank_get_is_disposed(CnaHandle waveBank, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_wave_bank_get_is_prepared(CnaHandle waveBank, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_wave_bank_get_is_in_use(CnaHandle waveBank, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_bank_create(CnaHandle engine, CnaStringView fileName, out CnaHandle outSoundBank);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_bank_destroy(CnaHandle soundBank);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_bank_get_is_disposed(CnaHandle soundBank, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_bank_get_is_in_use(CnaHandle soundBank, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_bank_get_cue(CnaHandle soundBank, CnaStringView name, out CnaHandle outCue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_bank_play_cue(CnaHandle soundBank, CnaStringView name);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_bank_play_cue_3d(
        CnaHandle soundBank, CnaStringView name, in CnaAudioListener listener, in CnaAudioEmitter emitter);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_cue_destroy(CnaHandle cue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_cue_get_info(CnaHandle cue, ref CnaCueInfo info);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_cue_get_name_size(CnaHandle cue, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_cue_copy_name(CnaHandle cue, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_cue_apply_3d(CnaHandle cue, in CnaAudioListener listener, in CnaAudioEmitter emitter);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_cue_get_variable(CnaHandle cue, CnaStringView name, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_cue_set_variable(CnaHandle cue, CnaStringView name, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_cue_play(CnaHandle cue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_cue_pause(CnaHandle cue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_cue_resume(CnaHandle cue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_cue_stop(CnaHandle cue, uint options);

    // -- Storage (real ABI, storage.h -- Phase 8 WP13) ----------------------------------------
    //
    // The C API deliberately collapses XNA's fake-async BeginXxx/EndXxx pairs into one synchronous
    // call each, invoking CNA_StorageCompletionCallback before returning "so the canonical
    // completion contract is preserved" (storage.h:76-78). CNA.Storage therefore implements
    // Begin/End over an already-completed IAsyncResult rather than inventing real async, and never
    // passes a callback here -- the managed side owns the completion contract.
    //
    // CNA_FileMode and CNA_SeekOrigin map one-to-one onto System.IO.FileMode/SeekOrigin, so those
    // BCL enums cross the boundary by cast rather than getting CNA-flavoured duplicates (design
    // invariant #7).

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_storage_device_show_selector(nint callback, nint context, out CnaHandle outDevice);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_device_destroy(CnaHandle device);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_device_get_free_space(CnaHandle device, out long outFreeSpace);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_device_get_total_space(CnaHandle device, out long outTotalSpace);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_device_get_is_connected(CnaHandle device, out byte outIsConnected);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_device_delete_container(CnaHandle device, CnaStringView titleName);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_storage_container_open(
        CnaHandle device, CnaStringView displayName, nint callback, nint context, out CnaHandle outContainer);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_destroy(CnaHandle container);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_dispose(CnaHandle container);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_get_is_disposed(CnaHandle container, out byte outIsDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_get_display_name_size(CnaHandle container, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_storage_container_copy_display_name(
        CnaHandle container, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_create_directory(CnaHandle container, CnaStringView directory);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_directory_exists(CnaHandle container, CnaStringView directory, out byte outExists);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_delete_directory(CnaHandle container, CnaStringView directory);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_file_exists(CnaHandle container, CnaStringView file, out byte outExists);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_delete_file(CnaHandle container, CnaStringView file);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_get_file_name_count(
        CnaHandle container, CnaStringView searchPattern, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_storage_container_copy_file_name(
        CnaHandle container, CnaStringView searchPattern, ulong index, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_get_directory_name_count(
        CnaHandle container, CnaStringView searchPattern, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_storage_container_copy_directory_name(
        CnaHandle container, CnaStringView searchPattern, ulong index, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_create_file(CnaHandle container, CnaStringView file, out CnaHandle outStream);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_container_open_file(
        CnaHandle container, CnaStringView file, uint fileMode, out CnaHandle outStream);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_storage_stream_read(CnaHandle stream, byte* destination, ulong capacity, out ulong outRead);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_storage_stream_write(CnaHandle stream, byte* data, ulong count);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_stream_seek(CnaHandle stream, long offset, uint origin, out long outPosition);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_stream_get_position(CnaHandle stream, out long outPosition);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_stream_get_length(CnaHandle stream, out long outLength);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_stream_set_length(CnaHandle stream, long length);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_stream_get_can_read(CnaHandle stream, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_stream_get_can_write(CnaHandle stream, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_stream_get_can_seek(CnaHandle stream, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_stream_flush(CnaHandle stream);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_storage_stream_close(CnaHandle stream);

    // -- Video / VideoPlayer (real ABI, video.h -- Phase 8 WP12) ------------------------------
    //
    // A Video is created against a graphics device (it decodes into a texture); a VideoPlayer is
    // created against the *game*, not the device -- confirmed from the header, and the reason
    // VideoPlayer's managed constructor takes no argument and uses the ambient game handle the way
    // Keyboard/Mouse/TouchPanel already do.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_create(CnaHandle device, CnaStringView fileName, out CnaHandle outVideo);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_destroy(CnaHandle video);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_get_width(CnaHandle video, out int outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_get_height(CnaHandle video, out int outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_get_frames_per_second(CnaHandle video, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_get_duration(CnaHandle video, out long outTicks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_get_soundtrack_type(CnaHandle video, out uint outType);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_get_file_name_size(CnaHandle video, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_video_copy_file_name(CnaHandle video, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_create(CnaHandle game, out CnaHandle outPlayer);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_destroy(CnaHandle player);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_play(CnaHandle player, CnaHandle video);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_stop(CnaHandle player);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_pause(CnaHandle player);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_resume(CnaHandle player);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_get_state(CnaHandle player, out uint outState);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_get_is_looped(CnaHandle player, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_set_is_looped(CnaHandle player, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_get_is_muted(CnaHandle player, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_set_is_muted(CnaHandle player, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_get_volume(CnaHandle player, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_set_volume(CnaHandle player, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_get_play_position_ticks(CnaHandle player, out long outTicks);

    /// <summary>Reports availability separately from the handle: between frames, or before
    /// playback starts, there is genuinely no texture yet -- which is why
    /// <c>CNA.Media.VideoPlayer.GetTexture</c> can return <see langword="null"/> rather than
    /// failing.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_get_texture(CnaHandle player, out CnaHandle outTexture, out byte outAvailable);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_video_player_get_video(CnaHandle player, out CnaHandle outVideo, out byte outAvailable);

    // -- Effect reflection surface (real ABI, effects.h -- Phase 8 WP4a) ----------------------
    //
    // Parameters, techniques, passes and annotations are all real native objects reachable from an
    // effect handle. Note the collections and their elements are *borrowed*: cna_effect_get_parameters
    // hands back a collection owned by the effect, and get_at hands back a parameter owned by that
    // collection -- only the explicitly-created ones (cna_effect_parameter_create and friends, which
    // this project never calls) need destroying, which is why nothing here is wrapped in a
    // NativeResourceHandle.

    // Every handle these hand out is documented "Owned" (effects.h:462-465, 809-818, 1252-1270,
    // and every collection get_at/find_*), and each mints a fresh registry slot per call -- they
    // are NOT views aliasing something the effect owns. An earlier revision of this binding claimed
    // the opposite and destroyed none of them, leaking on the per-frame ModelMesh.Draw path.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_destroy(CnaHandle parameter);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_collection_destroy(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_destroy(CnaHandle annotation);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_collection_destroy(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_technique_destroy(CnaHandle technique);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_technique_collection_destroy(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_pass_destroy(CnaHandle pass);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_pass_collection_destroy(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_get_parameters(CnaHandle effect, out CnaHandle outCollection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_get_techniques(CnaHandle effect, out CnaHandle outCollection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_get_current_technique(CnaHandle effect, out CnaHandle outTechnique);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_set_current_technique(CnaHandle effect, CnaHandle technique);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_collection_get_count(CnaHandle collection, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_collection_get_at(CnaHandle collection, ulong index, out CnaHandle outParameter);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_collection_find_name(
        CnaHandle collection, CnaStringView name, out byte outFound, out CnaHandle outParameter);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_collection_find_semantic(
        CnaHandle collection, CnaStringView semantic, out byte outFound, out CnaHandle outParameter);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_get_info(CnaHandle parameter, ref CnaEffectParameterInfo info);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_get_name_byte_count(CnaHandle parameter, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_effect_parameter_copy_name(
        CnaHandle parameter, byte* destination, ulong capacity, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_get_semantic_byte_count(CnaHandle parameter, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_effect_parameter_copy_semantic(
        CnaHandle parameter, byte* destination, ulong capacity, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_get_elements(CnaHandle parameter, out CnaHandle outCollection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_get_structure_members(CnaHandle parameter, out CnaHandle outCollection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_get_annotations(CnaHandle parameter, out CnaHandle outCollection);

    /// <summary>One function for every scalar/vector/matrix type, discriminated by
    /// <paramref name="valueType"/> -- <paramref name="outValue"/> must point at storage of the
    /// matching size, which is why <c>CNA.Graphics.EffectParameter</c> wraps each call in a typed
    /// method rather than exposing this shape.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_effect_parameter_get_value(CnaHandle parameter, CnaEffectValueType valueType, void* outValue);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_effect_parameter_set_value(CnaHandle parameter, CnaEffectValueType valueType, void* value);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_effect_parameter_get_values(
        CnaHandle parameter, CnaEffectValueType valueType, ulong requestedCount, void* destination, ulong capacity, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_effect_parameter_set_values(
        CnaHandle parameter, CnaEffectValueType valueType, void* values, ulong count);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_get_value_texture(
        CnaHandle parameter, CnaEffectTextureType textureType, out CnaHandle outTexture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_set_value_texture(
        CnaHandle parameter, CnaEffectTextureType textureType, CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_get_value_string_byte_count(CnaHandle parameter, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_effect_parameter_copy_value_string(
        CnaHandle parameter, byte* destination, ulong capacity, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_parameter_set_value_string(CnaHandle parameter, CnaStringView value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_technique_collection_get_count(CnaHandle collection, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_technique_collection_get_at(CnaHandle collection, ulong index, out CnaHandle outTechnique);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_technique_collection_find(
        CnaHandle collection, CnaStringView name, out byte outFound, out CnaHandle outTechnique);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_technique_get_name_byte_count(CnaHandle technique, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_effect_technique_copy_name(
        CnaHandle technique, byte* destination, ulong capacity, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_technique_get_passes(CnaHandle technique, out CnaHandle outCollection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_technique_get_annotations(CnaHandle technique, out CnaHandle outCollection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_pass_collection_get_count(CnaHandle collection, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_pass_collection_get_at(CnaHandle collection, ulong index, out CnaHandle outPass);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_pass_collection_find(
        CnaHandle collection, CnaStringView name, out byte outFound, out CnaHandle outPass);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_pass_get_name_byte_count(CnaHandle pass, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_effect_pass_copy_name(CnaHandle pass, byte* destination, ulong capacity, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_pass_get_annotations(CnaHandle pass, out CnaHandle outCollection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_pass_apply(CnaHandle pass);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_collection_get_count(CnaHandle collection, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_collection_get_at(CnaHandle collection, ulong index, out CnaHandle outAnnotation);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_collection_find(
        CnaHandle collection, CnaStringView name, out byte outFound, out CnaHandle outAnnotation);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_get_info(CnaHandle annotation, ref CnaEffectAnnotationInfo info);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_get_name_byte_count(CnaHandle annotation, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_effect_annotation_copy_name(
        CnaHandle annotation, byte* destination, ulong capacity, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_get_semantic_byte_count(CnaHandle annotation, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_effect_annotation_copy_semantic(
        CnaHandle annotation, byte* destination, ulong capacity, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_get_value_boolean(CnaHandle annotation, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_get_value_int32(CnaHandle annotation, out int outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_get_value_single(CnaHandle annotation, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_get_value_matrix(CnaHandle annotation, out CnaMatrix outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_get_value_vector2(CnaHandle annotation, out CnaVector2 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_get_value_vector3(CnaHandle annotation, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_get_value_vector4(CnaHandle annotation, out CnaVector4 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_annotation_get_value_string_byte_count(CnaHandle annotation, out ulong outByteCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_effect_annotation_copy_value_string(
        CnaHandle annotation, byte* destination, ulong capacity, out ulong outByteCount);

    // -- Stock effects beyond BasicEffect (real ABI, effects.h -- Phase 8 WP4b) ---------------
    //
    // Each is created from a device and then driven through its own property setters, exactly as
    // BasicEffect already is; the shared IEffectMatrices/IEffectFog/IEffectLights contracts
    // (cna_effect_matrices_*/cna_effect_fog_*/cna_effect_lights_*) are already declared above and
    // apply to these too, which is why only the effect-specific members appear here.

    /// <summary>Matches <c>cna_effect_material_create</c> exactly (<c>effects.h:1216</c>) -- note
    /// it clones an *existing* effect rather than taking a device, which is what an EffectMaterial
    /// is: a per-material copy of a shared effect with its own parameter values.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_effect_material_create(CnaHandle cloneSource, out CnaHandle outEffect);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_create(CnaHandle device, out CnaHandle outEffect);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_get_diffuse_color(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_set_diffuse_color(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_get_alpha(CnaHandle effect, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_set_alpha(CnaHandle effect, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_get_texture(CnaHandle effect, out byte outHasTexture, out CnaHandle outTexture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_set_texture(CnaHandle effect, CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_get_vertex_color_enabled(CnaHandle effect, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_set_vertex_color_enabled(CnaHandle effect, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_get_alpha_function(CnaHandle effect, out uint outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_set_alpha_function(CnaHandle effect, uint value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_get_reference_alpha(CnaHandle effect, out int outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_alpha_test_effect_set_reference_alpha(CnaHandle effect, int value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dual_texture_effect_create(CnaHandle device, out CnaHandle outEffect);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dual_texture_effect_get_diffuse_color(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dual_texture_effect_set_diffuse_color(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dual_texture_effect_get_alpha(CnaHandle effect, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dual_texture_effect_set_alpha(CnaHandle effect, float value);

    /// <summary>Matches <c>cna_dual_texture_effect_get_texture</c> exactly
    /// (<c>effects.h:1851</c>). <paramref name="textureIndex"/> selects the layer -- "zero or one"
    /// per the header, which is what makes <c>DualTextureEffect.Texture2</c> bindable at all; an
    /// earlier declaration omitted it, shifting every later argument by one slot.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dual_texture_effect_get_texture(
        CnaHandle effect, uint textureIndex, out byte outHasTexture, out CnaHandle outTexture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dual_texture_effect_set_texture(CnaHandle effect, uint textureIndex, CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dual_texture_effect_get_vertex_color_enabled(CnaHandle effect, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_dual_texture_effect_set_vertex_color_enabled(CnaHandle effect, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_create(CnaHandle device, out CnaHandle outEffect);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_get_diffuse_color(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_set_diffuse_color(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_get_emissive_color(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_set_emissive_color(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_get_alpha(CnaHandle effect, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_set_alpha(CnaHandle effect, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_get_texture(CnaHandle effect, out byte outHasTexture, out CnaHandle outTexture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_set_texture(CnaHandle effect, CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_get_environment_map(CnaHandle effect, out byte outHasTexture, out CnaHandle outTexture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_set_environment_map(CnaHandle effect, CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_get_amount(CnaHandle effect, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_set_amount(CnaHandle effect, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_get_specular(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_set_specular(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_get_fresnel_factor(CnaHandle effect, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_environment_map_effect_set_fresnel_factor(CnaHandle effect, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_create(CnaHandle device, out CnaHandle outEffect);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_get_diffuse_color(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_set_diffuse_color(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_get_emissive_color(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_set_emissive_color(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_get_specular_color(CnaHandle effect, out CnaVector3 outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_set_specular_color(CnaHandle effect, CnaVector3 value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_get_specular_power(CnaHandle effect, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_set_specular_power(CnaHandle effect, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_get_alpha(CnaHandle effect, out float outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_set_alpha(CnaHandle effect, float value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_get_prefer_per_pixel_lighting(CnaHandle effect, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_set_prefer_per_pixel_lighting(CnaHandle effect, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_get_texture(CnaHandle effect, out byte outHasTexture, out CnaHandle outTexture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_set_texture(CnaHandle effect, CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_get_vertex_color_enabled(CnaHandle effect, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_set_vertex_color_enabled(CnaHandle effect, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_get_weights_per_vertex(CnaHandle effect, out int outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_skinned_effect_set_weights_per_vertex(CnaHandle effect, int value);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_skinned_effect_set_bone_transforms(CnaHandle effect, CnaMatrix* transforms, ulong transformCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_skinned_effect_copy_bone_transforms(
        CnaHandle effect, ulong requestedCount, CnaMatrix* destination, ulong capacity, out ulong outCount);

    // -- OcclusionQuery + multi-stream vertex binding (real ABI, graphics_device.h + ------------
    // -- vertex_resources.h -- Phase 8 WP10)

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_occlusion_query_create(CnaHandle device, out CnaHandle outQuery);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_occlusion_query_destroy(CnaHandle query);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_occlusion_query_begin(CnaHandle query);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_occlusion_query_end(CnaHandle query);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_occlusion_query_get_is_complete(CnaHandle query, out byte outIsComplete);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_occlusion_query_get_pixel_count(CnaHandle query, out int outPixelCount);

    /// <summary>Matches <c>cna_graphics_device_set_vertex_buffers</c> exactly
    /// (<c>graphics_device.h:826</c>) -- the multi-stream form
    /// <see cref="cna_graphics_device_set_vertex_buffer"/> is the single-stream shorthand for. An
    /// empty array unbinds every stream; every binding is validated before any is applied.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_graphics_device_set_vertex_buffers(
        CnaHandle device, CnaVertexBufferBinding* bindings, ulong bindingCount);

    // -- GraphicsDeviceManager (real ABI, runtime_graphics_manager.h -- Phase 8 WP6) ----------
    //
    // cna_graphics_device_manager_create is documented as registering the new manager as the
    // game's graphics device manager and graphics device service, and refuses a second manager per
    // game -- which is exactly real XNA's own `new GraphicsDeviceManager(this)` contract, so the
    // managed constructor maps one-to-one onto it rather than deferring creation.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_create(CnaHandle game, out CnaHandle outManager);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_destroy(CnaHandle manager);

    /// <summary>Matches <c>cna_graphics_device_manager_subscribe</c>
    /// (<c>runtime_graphics_manager.h:518</c>). <paramref name="callback"/> is a
    /// <c>CNA_GameEventCallback</c>, i.e. <c>void(void*)</c> -- declared as <see cref="nint"/>
    /// rather than as a function-pointer type so no call site needs an <c>unsafe</c> context; the
    /// one place that produces the pointer (<c>CNA.NativeEventBridge</c>) is already unsafe, and
    /// nothing else has any business synthesising one. The same consequence applies as for the
    /// game-component table: nothing may unwind out of that callback.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_subscribe(
        CnaHandle manager, uint eventId, nint callback, nint context, out CnaHandle outRegistration);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_game_unsubscribe(CnaHandle registration);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_apply_changes(CnaHandle manager);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_create_device(CnaHandle manager);

    /// <summary>Returns whether the frame should be drawn -- the native counterpart of XNA's
    /// <c>IGraphicsDeviceManager.BeginDraw</c>, which reports <see langword="false"/> while the
    /// device cannot present.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_begin_draw(CnaHandle manager, out byte outShouldDraw);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_end_draw(CnaHandle manager);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_toggle_full_screen(CnaHandle manager);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_get_graphics_profile(CnaHandle manager, out CnaGraphicsProfile outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_set_graphics_profile(CnaHandle manager, CnaGraphicsProfile value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_get_is_full_screen(CnaHandle manager, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_set_is_full_screen(CnaHandle manager, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_get_prefer_multi_sampling(CnaHandle manager, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_set_prefer_multi_sampling(CnaHandle manager, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_get_preferred_back_buffer_format(CnaHandle manager, out uint outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_set_preferred_back_buffer_format(CnaHandle manager, uint value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_get_preferred_back_buffer_width(CnaHandle manager, out int outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_set_preferred_back_buffer_width(CnaHandle manager, int value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_get_preferred_back_buffer_height(CnaHandle manager, out int outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_set_preferred_back_buffer_height(CnaHandle manager, int value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_get_preferred_depth_stencil_format(CnaHandle manager, out uint outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_set_preferred_depth_stencil_format(CnaHandle manager, uint value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_get_synchronize_with_vertical_retrace(CnaHandle manager, out byte outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_set_synchronize_with_vertical_retrace(CnaHandle manager, byte value);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_get_supported_orientations(CnaHandle manager, out uint outValue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_manager_set_supported_orientations(CnaHandle manager, uint value);

    // -- Display / adapter / presentation (real ABI, display.h -- Phase 8 WP5) ----------------
    //
    // Every adapter function takes a graphics-device handle plus an adapter index rather than an
    // adapter handle: adapters are not resources with a lifetime here, they are indices into a
    // list the device can enumerate, refreshed by cna_graphics_adapters_refresh. That is why
    // CNA.Graphics.GraphicsAdapter holds a device + index, not a handle.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_display_mode_init(int width, int height, uint format, out CnaDisplayMode outMode);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_adapter_get_count(CnaHandle device, out ulong outCount);

    /// <summary>Matches <c>cna_graphics_device_get_adapter_index</c> exactly
    /// (<c>graphics_device.h:357</c>) -- the index of the adapter *this device* renders with,
    /// which is not necessarily the machine's default one.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_get_adapter_index(CnaHandle device, out uint outAdapterIndex);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_adapter_get_info(CnaHandle device, uint adapterIndex, ref CnaGraphicsAdapterInfo info);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_graphics_adapter_copy_description(
        CnaHandle device, uint adapterIndex, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_graphics_adapter_copy_device_name(
        CnaHandle device, uint adapterIndex, byte* destination, ulong capacity, out ulong outBytes);

    /// <summary><c>ref</c>, not <c>out</c>: the header documents <c>out_mode</c> as a
    /// "Caller-initialized versioned output structure", so its
    /// <c>struct_size</c>/<c>struct_version</c> must already be filled in -- which <c>out</c> would
    /// zero, since it does not run the struct's parameterless constructor.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_adapter_get_current_display_mode(
        CnaHandle device, uint adapterIndex, ref CnaDisplayMode mode);

    /// <summary>Matches <c>cna_graphics_adapter_get_display_mode_count</c> exactly
    /// (<c>display.h:255</c>) -- including <paramref name="filterByFormat"/>/<paramref name="format"/>,
    /// which an earlier declaration omitted, shifting <c>out_count</c> into the filter slot.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_adapter_get_display_mode_count(
        CnaHandle device, uint adapterIndex, byte filterByFormat, uint format, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_graphics_adapter_copy_display_modes(
        CnaHandle device, uint adapterIndex, byte filterByFormat, uint format,
        CnaDisplayMode* destination, ulong capacity, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_adapter_is_profile_supported(
        CnaHandle device, uint adapterIndex, CnaGraphicsProfile profile, out byte outSupported);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_adapters_refresh(CnaHandle device);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_presentation_parameters_init(out CnaPresentationParameters outParameters);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_get_presentation_parameters(
        CnaHandle device, ref CnaPresentationParameters parameters);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_set_presentation_parameters(
        CnaHandle device, in CnaPresentationParameters parameters);

    /// <summary><c>ref</c> for the same caller-initialized reason as
    /// <see cref="cna_graphics_adapter_get_current_display_mode"/>.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_get_display_mode(CnaHandle device, ref CnaDisplayMode mode);

    // -- TouchPanel (real ABI, input.h + input_touch.h -- Phase 8 WP9) ------------------------
    //
    // Split across two headers: the state snapshot lives with the rest of input (cna_touch_get_state
    // in input.h, alongside keyboard/mouse/gamepad), while the panel's own configuration and the
    // gesture queue live in input_touch.h. The _ext-suffixed panel functions (enqueue_gesture,
    // raise_touch_event, set_finger, update, reset_for_tests) are CNA test hooks with no XNA
    // counterpart and are deliberately not bound.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_get_state(CnaHandle game, ref CnaTouchState state);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_capabilities_init(ref CnaTouchCapabilities capabilities);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_panel_get_display_width(CnaHandle game, out int outWidth);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_panel_set_display_width(CnaHandle game, int width);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_panel_get_display_height(CnaHandle game, out int outHeight);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_panel_set_display_height(CnaHandle game, int height);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_panel_get_display_orientation(CnaHandle game, out uint outOrientation);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_panel_set_display_orientation(CnaHandle game, uint orientation);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_panel_get_enabled_gestures(CnaHandle game, out uint outGestures);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_panel_set_enabled_gestures(CnaHandle game, uint gestures);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_panel_get_is_gesture_available(CnaHandle game, out byte outAvailable);

    /// <summary>Dequeues the next gesture. <c>CNA_RESULT_INVALID_STATE</c> when the queue is empty,
    /// which <c>CNA.Input.TouchPanel.ReadGesture</c> turns into the documented
    /// <see cref="System.InvalidOperationException"/> real XNA throws in the same
    /// situation.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_touch_panel_read_gesture(CnaHandle game, ref CnaGestureSample sample);

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

    // There is deliberately no `cna_content_load_spritefont` here. One used to be declared, and it
    // named a function that exists in no header -- every Load<SpriteFont> would have died with an
    // EntryPointNotFoundException. content.h loads textures, sound effects and texture cubes and
    // nothing else, so the .xnb SpriteFont container is parsed managed-side instead
    // (CNA.Content.Xnb.XnbSpriteFontReader).

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_content_manager_load_sound_effect(
        CnaHandle contentManager,
        CnaStringView assetName,
        out CnaHandle soundEffect);

    // -- SoundEffect / SoundEffectInstance (real ABI, audio.h -- step 9 of the native-ABI --------
    // migration). Renamed throughout (cna_soundeffect_* -> cna_sound_effect_*,
    // cna_soundeffectinstance_* -> cna_sound_effect_instance_*) and every creation/instance call
    // now needs the CnaAmbientGame.Current handle from step 2's design -- the real ABI has no
    // parameterless audio route anywhere. cna_sound_effect_create_pcm16_range_ext ("the canonical
    // seven-argument constructor") is a genuine, confirmed match for this project's own
    // offset/count/loopStart/loopLength constructor shape -- see CnaSoundEffectCreateInfo's own
    // doc comment for why the loop region is a route parameter, not a CreateInfo field.
    // cna_sound_effect_instance_get_state/_get_volume/_get_pitch/_get_pan/_get_is_looped don't
    // exist at all -- only one combined cna_sound_effect_instance_get_info snapshot -- but every
    // individual setter does still exist, an asymmetric shape confirmed directly, not assumed.

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_sound_effect_create_pcm16_range_ext(
        CnaHandle game,
        in CnaSoundEffectCreateInfo createInfo,
        byte* pcmBytes,
        ulong byteCount,
        int offset,
        int count,
        int loopStart,
        int loopLength,
        out CnaHandle soundEffect);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_destroy(CnaHandle soundEffect);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_get_duration_ticks(CnaHandle soundEffect, out long outDurationTicks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_create_instance(CnaHandle soundEffect, out CnaHandle instance);

    /// <summary>Fire-and-forget playback (<c>audio.h:483</c>). <c>out_played</c> is the canonical
    /// "instance limit reached" answer -- <c>CNA_FALSE</c> rather than a failure, which is also
    /// what a disposed effect reports.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_play(CnaHandle soundEffect, out byte outPlayed);

    /// <summary>(<c>audio.h:499</c>.) The canonical asymmetry is preserved by native: pan is
    /// range-checked and pitch is clamped.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_play_with_settings(
        CnaHandle soundEffect, float volume, float pitch, float pan, out byte outPlayed);

    // The four process-wide audio settings (audio.h:408-471). Game-handle addressed, like every
    // other route in this header.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_get_master_volume(CnaHandle game, out float outVolume);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_set_master_volume(CnaHandle game, float volume);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_get_distance_scale(CnaHandle game, out float outScale);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_set_distance_scale(CnaHandle game, float scale);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_get_doppler_scale(CnaHandle game, out float outScale);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_set_doppler_scale(CnaHandle game, float scale);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_get_speed_of_sound(CnaHandle game, out float outSpeed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_set_speed_of_sound(CnaHandle game, float speed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_instance_destroy(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_instance_play(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_instance_pause(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_instance_resume(CnaHandle instance);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_instance_stop(CnaHandle instance, byte immediate);

    /// <summary>The only real way to read state/volume/pitch/pan/is_looped -- see this section's
    /// own comment.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_sound_effect_instance_get_info(CnaHandle instance, ref CnaSoundEffectInstanceInfo outInfo);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_instance_set_volume(CnaHandle instance, float volume);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_instance_set_pitch(CnaHandle instance, float pitch);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_instance_set_pan(CnaHandle instance, float pan);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_sound_effect_instance_set_is_looped(CnaHandle instance, byte isLooped);

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

    /// <summary>Caller-initialized versioned output, so <c>ref</c> over a <c>new</c>-constructed
    /// local rather than <c>out</c> -- <c>out</c> skips the parameterless constructor that fills
    /// the struct header.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_vertex_buffer_get_info(CnaHandle vertexBuffer, ref CnaVertexBufferInfo info);

    /// <summary>Typed readback for the built-in vertex layouts (<c>vertex_resources.h:329</c>).
    /// There is no raw-bytes equivalent, which is why <c>VertexBuffer.GetData</c> is limited to
    /// the types <see cref="CnaVertexType"/> names.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_vertex_buffer_get_data(
        CnaHandle vertexBuffer,
        in CnaVertexBufferTransfer transfer,
        void* destination,
        ulong capacity,
        out ulong outElementCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_index_buffer_get_info(CnaHandle indexBuffer, ref CnaIndexBufferInfo info);

    /// <summary>The ContentLost callback is <c>void(handle, void* context)</c> -- two arguments,
    /// unlike the game/audio families -- so it goes through
    /// <c>NativeEventBridge.SubscribeWithSender</c>.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_vertex_buffer_subscribe_content_lost(
        CnaHandle vertexBuffer, nint callback, nint context, out CnaHandle outRegistration);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_vertex_buffer_unsubscribe_content_lost(CnaHandle registration);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_index_buffer_subscribe_content_lost(
        CnaHandle indexBuffer, nint callback, nint context, out CnaHandle outRegistration);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_index_buffer_unsubscribe_content_lost(CnaHandle registration);

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

    // -- Song (real ABI, media.h -- step 10 of the native-ABI migration) ---------------------
    //
    // Song is a real native object upstream (CNA_SongHandle, cna_song_*) -- this project's own
    // pre-migration Song was pure C# state with a *file path string* masquerading as "Handle",
    // never crossing the ABI until MediaPlayer.Play (which then passed that path straight to a
    // fictional cna_mediaplayer_play(path) route). cna_song_create/_create_with_duration are a
    // genuine, confirmed match for this project's own two constructors (the "explicit duration in
    // milliseconds" one already matched real XNA's real 3-argument constructor exactly). See
    // CNA.Media.Song.cs for how the file-path text is still kept client-side (for
    // MediaPlayer.LoadSong's defensive-copy pattern) alongside the new native handle.

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_song_create(CnaHandle game, CnaStringView fileName, CnaStringView name, out CnaHandle outSong);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_song_create_with_duration(
        CnaHandle game, CnaStringView fileName, CnaStringView assetName, int durationMilliseconds, out CnaHandle outSong);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_destroy(CnaHandle song);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_duration(CnaHandle song, out long outTicks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_set_duration(CnaHandle song, long ticks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_is_protected(CnaHandle song, out byte outProtected);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_is_rated(CnaHandle song, out byte outRated);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_play_count(CnaHandle song, out int outPlayCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_set_play_count(CnaHandle song, int playCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_rating(CnaHandle song, out int outRating);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_track_number(CnaHandle song, out int outTrackNumber);

    // The rest of media.h's song surface, added with the media-library rebinding: a song reached
    // through a library collection has no client-side name or path for the managed wrapper to have
    // kept, so both have to come from native.

    /// <summary>Media-source enumeration (<c>media.h:128-178</c>). Index-addressed rather than
    /// handle-based, and the header is explicit that the list is "a point-in-time snapshot -- an
    /// index is valid only until the device set changes", which is why nothing here caches
    /// one.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_source_get_available_count(CnaHandle game, out uint outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_source_get_type_at(CnaHandle game, uint index, out uint outType);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_source_get_name_size_at(CnaHandle game, uint index, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_media_source_copy_name_at(
        CnaHandle game, uint index, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_name_size(CnaHandle song, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_song_copy_name(
        CnaHandle song, byte* destination, ulong capacity, out ulong outBytes);

    /// <summary>The file path or handle a song plays from -- "the string song equality and hashing
    /// are computed from, not the display name" (<c>media.h:319</c>).</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_handle_text_size_ext(CnaHandle song, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_song_copy_handle_text_ext(
        CnaHandle song, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_is_disposed(CnaHandle song, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_dispose(CnaHandle song);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_equals(CnaHandle left, CnaHandle right, out byte outEqual);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_hash_code(CnaHandle song, out int outHash);

    /// <summary>Builds a collection over existing songs. The collection keeps every song it was
    /// given alive, so a caller may release its own handles immediately afterwards
    /// (<c>media.h:525-528</c>).</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_song_collection_create(
        CnaHandle game, CnaHandle* songs, ulong count, out CnaHandle outCollection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_collection_get_at(CnaHandle collection, int index, out CnaHandle outSong);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_collection_get_count(CnaHandle collection, out int outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_collection_get_is_disposed(CnaHandle collection, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_collection_dispose(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_collection_destroy(CnaHandle collection);

    // -- MediaPlayer (media_player.h) ------------------------------------------------------
    //
    // Every route takes a game handle (CnaAmbientGame.Current); no parameterless media route
    // exists anywhere, the same finding audio produced.
    //
    // The whole 41-function surface is bound. A previous pass bound eight of them and recorded
    // that State/Volume/IsMuted/PlayPosition/Queue "stay deliberately NOT native-backed", mirroring
    // the C++ engine's plain static fields, and that the native queue and move_next/_previous/
    // is_repeating/is_shuffled were "genuine new capability ... not something the ABI mismatch
    // forces". A header audit showed the cost of that: the managed side had reimplemented the
    // engine's queue management, shuffle order, state machine and playback timer, so two
    // independent implementations of the same behaviour could disagree, and native's was the one
    // actually driving the audio device.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_play_song(CnaHandle game, CnaHandle song);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_play_songs(CnaHandle game, CnaHandle songs);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_play_songs_from(CnaHandle game, CnaHandle songs, int index);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_move_next(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_move_previous(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_get_state(CnaHandle game, out uint outState);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_get_volume(CnaHandle game, out float outVolume);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_get_is_muted(CnaHandle game, out byte outMuted);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_get_is_repeating(CnaHandle game, out byte outRepeating);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_set_is_repeating(CnaHandle game, byte repeating);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_get_is_shuffled(CnaHandle game, out byte outShuffled);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_set_is_shuffled(CnaHandle game, byte shuffled);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_get_play_position_ticks(CnaHandle game, out long outTicks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_get_game_has_control(CnaHandle game, out byte outHasControl);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_get_is_visualization_enabled(CnaHandle game, out byte outEnabled);

    /// <summary>The canonical timer and state-transition pump (<c>media_player.h:276</c>).</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_update_ext(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_program_exit_ext(CnaHandle game);

    /// <summary>The fallback song-end detector (<c>media_player.h:301</c>). A song whose duration
    /// is unknown never reports ended, rather than reporting it immediately.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_detect_song_ended_by_elapsed_time_ext(
        CnaHandle song, long elapsedTicks, out byte outEnded);

    /// <summary>The canonical event is <b>static</b>, so this subscription belongs to the process
    /// rather than to a game and takes no game handle (<c>media_player.h:317</c>). See
    /// <see cref="nint"/> callback note on the graphics-device-manager subscribe.</summary>
    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_subscribe_active_song_changed_ext(
        nint callback, nint context, out CnaHandle outRegistration);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_subscribe_media_state_changed_ext(
        nint callback, nint context, out CnaHandle outRegistration);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_unsubscribe_ext(CnaHandle registration);

    // ---- MediaQueue (media_player.h:373-487) ----
    //
    // The queue handle is borrowed from the player, which owns the process-wide queue; releasing it
    // with cna_media_queue_destroy releases the handle, not the queue.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_get_queue(CnaHandle game, out CnaHandle outQueue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_queue_get_count(CnaHandle queue, out int outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_queue_get_active_song_index(CnaHandle queue, out int outIndex);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_queue_set_active_song_index(CnaHandle queue, int index);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_queue_get_active_song(
        CnaHandle queue, out CnaHandle outSong, out byte outAvailable);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_queue_get_at(CnaHandle queue, int index, out CnaHandle outSong);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_queue_add(CnaHandle queue, CnaHandle song);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_queue_clear(CnaHandle queue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_queue_destroy(CnaHandle queue);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_pause(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_resume(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_stop(CnaHandle game);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_set_volume(CnaHandle game, float volume);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_set_is_muted(CnaHandle game, byte muted);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_set_is_visualization_enabled(CnaHandle game, byte enabled);

    /// <summary>Matches <c>cna_media_player_get_visualization_data</c> exactly
    /// (<c>media_player.h:168</c>) -- takes one caller-provided <see cref="CnaVisualizationData"/>
    /// struct filled in place, not the old guessed shape's three flat pointer/pointer/count
    /// arguments.</summary>
    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_media_player_get_visualization_data(CnaHandle game, ref CnaVisualizationData data);

    // ---- Media library (media_library.h) ----
    //
    // Generated straight from the header's own declarations rather than hand-typed, then read back
    // against it -- 148 functions is more than hand-transcription stays honest across.
    //
    // The 26 `*_get_type_name_size`/`*_copy_type_name` routes are deliberately not bound. They
    // report a .NET type's fully-qualified name for hosts that have no reflection; C# has
    // `GetType().FullName` natively, so binding them would add a native round trip that answers a
    // question the runtime already answers, and none of it is XNA 4.0 API surface.
    //
    // Handle ownership, from the header: the library handle is OWNED, and every album/artist/genre/
    // playlist/picture/picture-album/collection handle reached through it is a borrowed *view of a
    // library-owned object* whose HANDLE the caller still has to release with its own `_destroy`
    // ("Releases an album handle. The album itself belongs to its media library and is untouched").
    // Holding any of them keeps the library alive, so releasing the library first is safe.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_create(CnaHandle game, out CnaHandle outLibrary);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_create_from_source(
        CnaHandle game, uint sourceIndex, out CnaHandle outLibrary);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_get_is_disposed(
        CnaHandle library, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_dispose(CnaHandle library);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_destroy(CnaHandle library);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_get_media_source_type(
        CnaHandle library, out uint outType);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_get_media_source_name_size(
        CnaHandle library, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_media_library_copy_media_source_name(
        CnaHandle library, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_get_songs(CnaHandle library, out CnaHandle outSongs);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_get_albums(CnaHandle library, out CnaHandle outAlbums);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_get_artists(
        CnaHandle library, out CnaHandle outArtists);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_get_genres(CnaHandle library, out CnaHandle outGenres);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_get_playlists(
        CnaHandle library, out CnaHandle outPlaylists);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_get_name_size(CnaHandle album, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_album_copy_name(
        CnaHandle album, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_get_is_disposed(CnaHandle album, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_dispose(CnaHandle album);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_destroy(CnaHandle album);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_get_hash_code(CnaHandle album, out int outHash);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_get_songs(CnaHandle album, out CnaHandle outSongs);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_collection_get_count(CnaHandle collection, out int outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_collection_get_at(
        CnaHandle collection, int index, out CnaHandle outAlbum);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_collection_get_is_disposed(
        CnaHandle collection, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_collection_dispose(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_collection_destroy(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_get_name_size(CnaHandle artist, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_artist_copy_name(
        CnaHandle artist, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_get_is_disposed(CnaHandle artist, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_dispose(CnaHandle artist);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_destroy(CnaHandle artist);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_get_hash_code(CnaHandle artist, out int outHash);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_get_songs(CnaHandle artist, out CnaHandle outSongs);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_collection_get_count(CnaHandle collection, out int outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_collection_get_at(
        CnaHandle collection, int index, out CnaHandle outArtist);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_collection_get_is_disposed(
        CnaHandle collection, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_collection_dispose(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_collection_destroy(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_get_name_size(CnaHandle genre, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_genre_copy_name(
        CnaHandle genre, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_get_is_disposed(CnaHandle genre, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_dispose(CnaHandle genre);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_destroy(CnaHandle genre);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_get_hash_code(CnaHandle genre, out int outHash);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_get_songs(CnaHandle genre, out CnaHandle outSongs);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_collection_get_count(CnaHandle collection, out int outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_collection_get_at(
        CnaHandle collection, int index, out CnaHandle outGenre);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_collection_get_is_disposed(
        CnaHandle collection, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_collection_dispose(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_collection_destroy(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_get_name_size(CnaHandle playlist, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_playlist_copy_name(
        CnaHandle playlist, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_get_is_disposed(CnaHandle playlist, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_dispose(CnaHandle playlist);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_destroy(CnaHandle playlist);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_get_hash_code(CnaHandle playlist, out int outHash);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_get_songs(CnaHandle playlist, out CnaHandle outSongs);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_collection_get_count(CnaHandle collection, out int outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_collection_get_at(
        CnaHandle collection, int index, out CnaHandle outPlaylist);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_collection_get_is_disposed(
        CnaHandle collection, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_collection_dispose(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_collection_destroy(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_equals(CnaHandle left, CnaHandle right, out byte outEqual);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_equals(CnaHandle left, CnaHandle right, out byte outEqual);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_equals(CnaHandle left, CnaHandle right, out byte outEqual);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_equals(CnaHandle left, CnaHandle right, out byte outEqual);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_get_artist(
        CnaHandle album, out CnaHandle outArtist, out byte outAvailable);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_get_genre(
        CnaHandle album, out CnaHandle outGenre, out byte outAvailable);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_get_duration(CnaHandle album, out long outTicks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_get_has_art(CnaHandle album, out byte outHasArt);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_get_art_size(CnaHandle album, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_album_copy_art(
        CnaHandle album, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_album_get_thumbnail_size(CnaHandle album, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_album_copy_thumbnail(
        CnaHandle album, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_artist_get_albums(CnaHandle artist, out CnaHandle outAlbums);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_genre_get_albums(CnaHandle genre, out CnaHandle outAlbums);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_playlist_get_duration(CnaHandle playlist, out long outTicks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_album(
        CnaHandle song, out CnaHandle outAlbum, out byte outAvailable);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_artist(
        CnaHandle song, out CnaHandle outArtist, out byte outAvailable);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_song_get_genre(
        CnaHandle song, out CnaHandle outGenre, out byte outAvailable);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_get_name_size(CnaHandle picture, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_picture_copy_name(
        CnaHandle picture, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_get_token_size_ext(CnaHandle picture, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_picture_copy_token_ext(
        CnaHandle picture, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_get_album(
        CnaHandle picture, out CnaHandle outAlbum, out byte outAvailable);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_get_date_unix_ticks(CnaHandle picture, out long outUnixTicks);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_get_width(CnaHandle picture, out int outWidth);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_get_height(CnaHandle picture, out int outHeight);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_get_image_size(CnaHandle picture, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_picture_copy_image(
        CnaHandle picture, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_get_thumbnail_size(CnaHandle picture, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_picture_copy_thumbnail(
        CnaHandle picture, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_get_is_disposed(CnaHandle picture, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_dispose(CnaHandle picture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_destroy(CnaHandle picture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_equals(CnaHandle left, CnaHandle right, out byte outEqual);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_get_hash_code(CnaHandle picture, out int outHash);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_get_name_size(CnaHandle album, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_picture_album_copy_name(
        CnaHandle album, byte* destination, ulong capacity, out ulong outBytes);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_get_parent(
        CnaHandle album, out CnaHandle outParent, out byte outAvailable);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_get_albums(CnaHandle album, out CnaHandle outAlbums);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_get_pictures(
        CnaHandle album, out CnaHandle outPictures);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_get_is_disposed(CnaHandle album, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_dispose(CnaHandle album);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_destroy(CnaHandle album);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_equals(
        CnaHandle left, CnaHandle right, out byte outEqual);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_get_hash_code(CnaHandle album, out int outHash);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_get_pictures(
        CnaHandle library, out CnaHandle outPictures);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_get_saved_pictures(
        CnaHandle library, out CnaHandle outPictures);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_get_root_picture_album(
        CnaHandle library, out CnaHandle outAlbum, out byte outAvailable);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_get_picture_from_token(
        CnaHandle library, CnaStringView token, out CnaHandle outPicture, out byte outAvailable);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_media_library_save_picture(
        CnaHandle library, CnaStringView name, byte* imageData, ulong imageByteCount, out CnaHandle outPicture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_library_save_picture_from_stream(
        CnaHandle library, CnaStringView name, CnaHandle source, out CnaHandle outPicture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_collection_get_count(CnaHandle collection, out int outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_collection_get_at(
        CnaHandle collection, int index, out CnaHandle outPicture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_collection_get_is_disposed(
        CnaHandle collection, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_collection_dispose(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_collection_destroy(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_collection_get_count(
        CnaHandle collection, out int outCount);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_collection_get_at(
        CnaHandle collection, int index, out CnaHandle outAlbum);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_collection_get_is_disposed(
        CnaHandle collection, out byte outDisposed);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_collection_dispose(CnaHandle collection);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_picture_album_collection_destroy(CnaHandle collection);
}
