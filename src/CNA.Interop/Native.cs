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
    // before cna_runtime_initialize -- which is why CNA.Graphics.BlendState/DepthStencilState/
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
    internal static partial CnaResult cna_texturecube_create(CnaHandle device, in CnaTextureCubeCreateInfo createInfo, out CnaHandle texture);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_texturecube_get_info(CnaHandle texture, ref CnaTextureCubeInfo info);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_texturecube_destroy(CnaHandle texture);

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

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_adapter_get_current_display_mode(
        CnaHandle device, uint adapterIndex, out CnaDisplayMode outMode);

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_adapter_get_display_mode_count(
        CnaHandle device, uint adapterIndex, out ulong outCount);

    [LibraryImport(LibraryName)]
    internal static unsafe partial CnaResult cna_graphics_adapter_copy_display_modes(
        CnaHandle device, uint adapterIndex, CnaDisplayMode* destination, ulong capacity, out ulong outCount);

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

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_graphics_device_get_display_mode(CnaHandle device, out CnaDisplayMode outMode);

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

    // -- MediaPlayer (real ABI, media_player.h -- step 10) -------------------------------------
    //
    // Renamed throughout (cna_mediaplayer_* -> cna_media_player_*). The single biggest shape
    // change: every one of these now needs a game handle (CnaAmbientGame.Current) -- no
    // parameterless media route exists anywhere, matching audio's own step 9 finding. Play is a
    // much deeper change than a rename: cna_mediaplayer_play took a raw file-path string; the real
    // cna_media_player_play_song takes a CNA_SongHandle -- Song had to become a real native object
    // first (see this file's own Song section above) before this function could even be called
    // correctly. State/Volume/IsMuted/PlayPosition stay deliberately NOT native-backed, matching
    // this project's own pre-migration design choice to mirror the real C++ engine's plain static
    // state -- see CNA.Media.MediaPlayer.cs for why that choice still holds. This project also
    // deliberately does not adopt the real native queue (cna_media_queue_*,
    // cna_media_player_get_queue) or cna_media_player_move_next/_previous/get_is_repeating/
    // _is_shuffled in this pass -- genuine new capability (this project's own local
    // CNA.Media.MediaQueue-based queue management already reproduces the same observable XNA
    // behavior once its own native calls below are fixed), not something the ABI mismatch forces;
    // see NEXT.md's own "not yet acted on" note for the full reasoning.

    [LibraryImport(LibraryName)]
    internal static partial CnaResult cna_media_player_play_song(CnaHandle game, CnaHandle song);

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
}
