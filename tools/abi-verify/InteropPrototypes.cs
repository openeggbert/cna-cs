// SPDX-License-Identifier: MIT
using System.Reflection;
using System.Runtime.InteropServices;
using CNA.Interop;

/// <summary>
/// Generates a C translation unit that proves every native import CNA.Interop declares has the
/// signature the canonical CNA headers give it.
///
/// <b>The mechanism is assignment.</b> For each import a file-scope function pointer is declared
/// with the prototype derived from the *managed* declaration and initialised with the real C
/// function. C requires the two to be compatible, so a wrong return type, a wrong parameter count,
/// a wrong parameter type, a lost <c>const</c>, or a pointer at the wrong depth is a diagnostic --
/// and under <c>-Werror</c>/<c>/WX</c> a diagnostic is a failed build. The C compiler is the
/// authority on what the header means, which is the point: nothing here re-implements C's type
/// rules.
///
/// The pointers are file-scope and non-static so that no compiler reports them unused, and the unit
/// is compiled with <c>-c</c>, so nothing has to link.
///
/// <b>What the managed side cannot express</b> is recorded rather than skipped. A callback declared
/// <c>nint</c> carries no signature at all, so its import is emitted with the callback parameter
/// taken from the header instead, and the import is reported under
/// <c>PROTO_CALLBACK_TYPE_FROM_HEADER</c>. The callback's own shape is then proven separately, from
/// the managed delegate that is actually passed.
/// </summary>
static class InteropPrototypes
{
    /// <summary>
    /// Parameters whose C type C# has no way to spell, keyed <c>function#index</c>.
    ///
    /// There are exactly four reasons an entry exists, and none of them is an ABI difference:
    ///
    /// <list type="bullet">
    /// <item><c>const T*</c> -- C# cannot qualify a pointer, and <c>const</c> is not part of any
    /// calling convention, so the managed declaration is representationally identical.</item>
    /// <item><c>char*</c> -- C's <c>char</c> is a third type distinct from both <c>signed char</c>
    /// and <c>unsigned char</c>, so neither <c>byte*</c> nor <c>sbyte*</c> is exact. These are
    /// CNA's UTF-8 copy-out buffers and <c>byte*</c> is the right managed spelling.</item>
    /// <item><c>void*</c> against a managed <c>byte*</c> -- both are object pointers of identical
    /// size and alignment.</item>
    /// <item>a callback the managed side declares <c>nint</c>, which carries no signature at
    /// all.</item>
    /// </list>
    ///
    /// <b>Every entry was produced by the compiler, not by hand:</b> the generator emitted the
    /// managed-derived prototype, the C compiler rejected it, and the type recorded here is the one
    /// the diagnostic named. That matters because it means this table cannot quietly excuse a real
    /// difference -- it can only excuse the difference that was actually measured, and any *other*
    /// change to the same parameter still fails to compile.
    ///
    /// The callback entries are structural (<c>void (*)(void*)</c>) rather than the header's typedef
    /// name on purpose: what has to agree is the shape, not the spelling.
    /// </summary>
    public static readonly Dictionary<string, string> ParameterOverrides = new(StringComparer.Ordinal)
    {
        ["cna_cnb_writer_add_chunk#2"] = "const uint8_t*",   // const, which C# cannot express on a pointer
        ["cna_cnb_texture_data_create_rgba8#2"] = "const uint8_t*",   // const, which C# cannot express on a pointer
        ["cna_post_process_pass_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_cnb_model_copy_bone_name#2"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_cnb_model_copy_mesh_name#2"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_cnb_model_copy_part_external_effect#2"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_cnb_model_copy_part_name#2"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_cnb_model_copy_material_texture#3"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_cnb_model_set_part_index_bytes#2"] = "const uint8_t*",   // const, which C# cannot put on a pointer
        ["cna_cnb_model_set_part_vertex_bytes#2"] = "const uint8_t*",   // const, which C# cannot put on a pointer
        ["cna_cnb_model_set_skeleton#1"] = "const int32_t*",   // const, which C# cannot put on a pointer
        ["cna_cnb_model_set_skeleton#3"] = "const float*",   // const, which C# cannot put on a pointer
        ["cna_cnb_model_set_skeleton#4"] = "const float*",   // const, which C# cannot put on a pointer
        ["cna_cnb_model_set_skeleton#5"] = "const float*",   // const, which C# cannot put on a pointer
        ["cna_cnb_model_add_bone#3"] = "const float*",   // const, which C# cannot put on a pointer
        ["cna_cnb_model_add_mesh#3"] = "const uint32_t*",   // const, which C# cannot put on a pointer
        ["cna_cnb_texture_data_select_representation#1"] = "CNA_Bool (*)(CNA_CnbTextureFormat, void*)",   // callback
        ["cna_album_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_artist_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_audio_category_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_audio_engine_copy_renderer_friendly_name#2"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_audio_engine_copy_renderer_id#2"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_audio_engine_subscribe_disposing_ext#1"] = "void (*)(void*)",   // callback
        ["cna_content_manager_copy_root_directory#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_content_reader_copy_asset_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_content_type_reader_copy_target_type_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_content_type_reader_manager_register#1"] = "const CNA_ContentTypeReaderCallbacks*",   // const, which C# cannot express on a pointer
        ["cna_cue_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_cue_subscribe_disposing_ext#1"] = "void (*)(void*)",   // callback
        ["cna_dynamic_sound_effect_instance_submit_buffer#1"] = "const uint8_t*",   // const, which C# cannot express on a pointer
        ["cna_dynamic_sound_effect_instance_subscribe_buffer_needed#1"] = "void (*)(void*)",   // callback
        ["cna_effect_annotation_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_effect_annotation_copy_semantic#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_effect_annotation_copy_value_string#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_effect_create_compiled#1"] = "const uint8_t*",   // const, which C# cannot express on a pointer
        ["cna_effect_parameter_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_effect_parameter_copy_semantic#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_effect_parameter_copy_value_string#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_effect_parameter_set_value#2"] = "const void*",   // const, which C# cannot express on a pointer
        ["cna_effect_parameter_set_values#2"] = "const void*",   // const, which C# cannot express on a pointer
        ["cna_effect_pass_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_effect_technique_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_error_copy_last_message#0"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_game_launch_parameters_copy_key#2"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_game_launch_parameters_copy_value#2"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_game_subscribe#2"] = "void (*)(void*)",   // callback
        ["cna_game_window_copy_screen_device_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_game_window_copy_title#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_game_window_subscribe#2"] = "void (*)(void*)",   // callback
        ["cna_genre_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_graphics_adapter_copy_description#2"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_graphics_adapter_copy_device_name#2"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_graphics_device_copy_renderer_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_graphics_device_manager_subscribe#2"] = "void (*)(void*)",   // callback
        ["cna_graphics_device_reset_with_parameters#2"] = "const uint32_t*",   // const, which C# cannot express on a pointer
        ["cna_graphics_device_set_render_targets#1"] = "const CNA_RenderTargetBinding*",   // const, which C# cannot express on a pointer
        ["cna_graphics_device_set_vertex_buffers#1"] = "const CNA_VertexBufferBinding*",   // const, which C# cannot express on a pointer
        ["cna_graphics_device_subscribe_event#2"] = "void (*)(uint64_t,  void*)",   // callback
        ["cna_graphics_device_subscribe_resource_created#1"] = "void (*)(uint64_t,  const CNA_ResourceCreatedEventInfo*, void*)",   // callback
        ["cna_graphics_device_subscribe_resource_destroyed#1"] = "void (*)(uint64_t,  const CNA_ResourceDestroyedEventInfo*, void*)",   // callback
        ["cna_index_buffer_get_data#2"] = "void*",   // const void*, which C# cannot express
        ["cna_index_buffer_set_data#2"] = "const void*",   // const void*, which C# cannot express
        ["cna_index_buffer_set_data_at#2"] = "const CNA_IndexBufferTransfer*",   // const, which C# cannot express on a pointer
        ["cna_index_buffer_set_data_at#3"] = "const void*",   // const, which C# cannot express on a pointer
        ["cna_index_buffer_subscribe_content_lost#1"] = "void (*)(uint64_t,  void*)",   // callback
        ["cna_media_library_copy_media_source_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_media_library_save_picture#2"] = "const uint8_t*",   // const, which C# cannot express on a pointer
        ["cna_media_player_subscribe_active_song_changed_ext#0"] = "void (*)(void*)",   // callback
        ["cna_media_player_subscribe_media_state_changed_ext#0"] = "void (*)(void*)",   // callback
        ["cna_media_source_copy_name_at#2"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_microphone_copy_name_at#2"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_microphone_subscribe_buffer_ready_at#2"] = "void (*)(void*)",   // callback
        ["cna_picture_album_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_picture_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_picture_copy_token_ext#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_playlist_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_render_target_subscribe_content_lost#1"] = "void (*)(uint64_t,  void*)",   // callback
        ["cna_skinned_effect_set_bone_transforms#1"] = "const CNA_Matrix*",   // const, which C# cannot express on a pointer
        ["cna_song_collection_create#1"] = "const uint64_t*",   // const, which C# cannot express on a pointer
        ["cna_song_copy_handle_text_ext#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_song_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_sound_bank_subscribe_disposing_ext#1"] = "void (*)(void*)",   // callback
        ["cna_sound_effect_copy_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_sound_effect_create_from_encoded_ext#1"] = "const uint8_t*",   // const, which C# cannot express on a pointer
        ["cna_sound_effect_create_pcm16_range_ext#2"] = "const uint8_t*",   // const, which C# cannot express on a pointer
        ["cna_sound_effect_instance_apply_3d_multi_ext#1"] = "const CNA_AudioListener*",   // const, which C# cannot express on a pointer
        ["cna_sprite_batch_begin_with_effect#2"] = "const CNA_BlendState*",   // const, which C# cannot express on a pointer
        ["cna_sprite_batch_begin_with_effect#3"] = "const CNA_SamplerState*",   // const, which C# cannot express on a pointer
        ["cna_sprite_batch_begin_with_effect#4"] = "const CNA_DepthStencilState*",   // const, which C# cannot express on a pointer
        ["cna_sprite_batch_begin_with_effect#5"] = "const CNA_RasterizerState*",   // const, which C# cannot express on a pointer
        ["cna_sprite_batch_begin_with_effect#7"] = "const CNA_Matrix*",   // const, which C# cannot express on a pointer
        ["cna_sprite_batch_begin_with_states#2"] = "const CNA_BlendState*",   // const, which C# cannot express on a pointer
        ["cna_sprite_batch_begin_with_states#3"] = "const CNA_SamplerState*",   // const, which C# cannot express on a pointer
        ["cna_sprite_batch_begin_with_states#4"] = "const CNA_DepthStencilState*",   // const, which C# cannot express on a pointer
        ["cna_sprite_batch_begin_with_states#5"] = "const CNA_RasterizerState*",   // const, which C# cannot express on a pointer
        ["cna_sprite_batch_submit_scaled_many#1"] = "const CNA_SpriteScaledCommand*",   // const, which C# cannot express on a pointer
        ["cna_storage_container_copy_directory_name#3"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_storage_container_copy_display_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_storage_container_copy_file_name#3"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_storage_container_open#2"] = "void (*)(void*)",   // callback
        ["cna_storage_container_subscribe_disposing#1"] = "void (*)(void*)",   // callback
        ["cna_storage_device_show_selector#0"] = "void (*)(void*)",   // callback
        ["cna_storage_device_show_selector_for_player#1"] = "void (*)(void*)",   // callback
        ["cna_storage_device_show_selector_for_player_with_space#3"] = "void (*)(void*)",   // callback
        ["cna_storage_device_show_selector_with_space#2"] = "void (*)(void*)",   // callback
        ["cna_storage_device_subscribe_device_changed#0"] = "void (*)(void*)",   // callback
        ["cna_storage_stream_write#1"] = "const uint8_t*",   // const, which C# cannot express on a pointer
        ["cna_texture2d_create_from_encoded_memory#1"] = "const uint8_t*",   // const, which C# cannot express on a pointer
        ["cna_texture2d_create_from_encoded_memory#3"] = "const CNA_Texture2DDecodeInfo*",   // const, which C# cannot express on a pointer
        ["cna_texture2d_get_data#2"] = "const CNA_Texture2DTransfer*",   // const, which C# cannot express on a pointer
        ["cna_texture2d_set_data#2"] = "const CNA_Texture2DTransfer*",   // const, which C# cannot express on a pointer
        ["cna_texture2d_set_data#3"] = "const void*",   // const, which C# cannot express on a pointer
        ["cna_texture2d_set_data_rgba8#1"] = "const CNA_Color*",   // const, which C# cannot express on a pointer
        ["cna_texture3d_set_data#2"] = "const CNA_Color*",   // const, which C# cannot express on a pointer
        ["cna_texture3d_set_data_bytes#2"] = "const uint8_t*",   // const, which C# cannot express on a pointer
        ["cna_texturecube_set_data#2"] = "const CNA_Color*",   // const, which C# cannot express on a pointer
        ["cna_title_location_copy_path#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_vertex_buffer_set_data#2"] = "const void*",   // const, which C# cannot express on a pointer
        ["cna_vertex_buffer_set_data_raw#1"] = "const void*",   // const void*, which C# cannot express
        ["cna_vertex_buffer_set_data_raw_at#2"] = "const void*",   // const, which C# cannot express on a pointer
        ["cna_vertex_buffer_set_data_raw_at_with_options#2"] = "const void*",   // const, which C# cannot express on a pointer
        ["cna_vertex_buffer_set_data_raw_with_options#1"] = "const void*",   // const, which C# cannot express on a pointer
        ["cna_vertex_buffer_subscribe_content_lost#1"] = "void (*)(uint64_t,  void*)",   // callback
        ["cna_vertex_declaration_create_with_stride#1"] = "const CNA_VertexElement*",   // const, which C# cannot express on a pointer
        ["cna_video_copy_file_name#1"] = "char*",   // text buffer: C char, which C# has no type for
        ["cna_wave_bank_subscribe_disposing_ext#1"] = "void (*)(void*)",   // callback
    };

    /// <summary>Imports whose whole prototype is supplied verbatim. Empty: every import is covered
    /// parameter by parameter, so nothing needs excusing wholesale.</summary>
    public static readonly Dictionary<string, string> HeaderPrototypes = new(StringComparer.Ordinal);

    public static IEnumerable<MethodInfo> Imports() =>
        typeof(Native)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(m => m.GetCustomAttribute<LibraryImportAttribute>() is not null)
            .OrderBy(m => m.Name, StringComparer.Ordinal);

    public static string EntryPoint(MethodInfo method)
    {
        LibraryImportAttribute attribute = method.GetCustomAttribute<LibraryImportAttribute>()!;
        return string.IsNullOrWhiteSpace(attribute.EntryPoint) ? method.Name : attribute.EntryPoint!;
    }

    private static readonly Dictionary<string, string> Primitives = new(StringComparer.Ordinal)
    {
        ["Void"] = "void",
        ["Byte"] = "uint8_t",
        ["SByte"] = "int8_t",
        ["Int16"] = "int16_t",
        ["UInt16"] = "uint16_t",
        ["Int32"] = "int32_t",
        ["UInt32"] = "uint32_t",
        ["Int64"] = "int64_t",
        ["UInt64"] = "uint64_t",
        ["Single"] = "float",
        ["Double"] = "double",
        ["IntPtr"] = "void*",
        ["UIntPtr"] = "size_t",
    };

    /// <summary>The C spelling of a managed type, or null when it has none.</summary>
    public static string? CType(Type type)
    {
        if (type.IsPointer)
        {
            string? inner = CType(type.GetElementType()!);
            return inner is null ? null : inner + "*";
        }

        if (type.IsFunctionPointer)
        {
            // `delegate* unmanaged[Cdecl]<A, B, R>` is an exact C function-pointer type, so it is
            // spelled structurally and checked like anything else. These are the imports whose
            // callback the managed side *does* type; the ones declared `nint` cannot be checked
            // here at all and are listed in ParameterOverrides instead.
            string? functionReturn = CType(type.GetFunctionPointerReturnType());
            List<string?> functionParameters =
                [.. type.GetFunctionPointerParameterTypes().Select(CType)];
            if (functionReturn is null || functionParameters.Any(p => p is null))
            {
                return null;
            }

            string inner = functionParameters.Count == 0
                ? "void"
                : string.Join(", ", functionParameters);
            return $"{functionReturn} (*)({inner})";
        }

        if (Primitives.TryGetValue(type.Name, out string? primitive))
        {
            return primitive;
        }

        if (type.IsEnum)
        {
            // CNA's enum-like identities are typedefs over a fixed-width integer, and the managed
            // enum's underlying type is what crosses. Naming the typedef would check the spelling
            // rather than the representation, and the spelling is not what the ABI fixes.
            return CType(type.GetEnumUnderlyingType());
        }

        return type.Name.StartsWith("Cna", StringComparison.Ordinal)
            ? InteropLayout.NativeName(type)
            : null;
    }

    /// <summary>The C parameter type for one managed parameter, honouring by-ref direction.</summary>
    public static string? ParameterType(ParameterInfo parameter)
    {
        Type type = parameter.ParameterType;
        if (!type.IsByRef)
        {
            return CType(type);
        }

        string? inner = CType(type.GetElementType()!);
        if (inner is null)
        {
            return null;
        }

        // `in` is the read-only direction and C says so with const; `out` and `ref` are both a
        // plain pointer. Getting this backwards is precisely what B2 exists to catch, so it is
        // derived from the parameter's own metadata rather than from its name.
        return parameter.IsIn && !parameter.IsOut ? $"const {inner}*" : $"{inner}*";
    }

    public static string Generate(out List<string> unmappable, out List<string> fromHeader)
    {
        unmappable = [];
        fromHeader = [];

        var text = new System.Text.StringBuilder();
        text.AppendLine("// SPDX-License-Identifier: MIT");
        text.AppendLine("// Generated by CNA.AbiVerify from CNA.Interop.Native. Do not edit.");
        text.AppendLine("//");
        text.AppendLine("// Each declaration below fails to compile unless the canonical CNA header gives the");
        text.AppendLine("// function exactly the prototype the managed declaration claims.");
        text.AppendLine();
        text.AppendLine("#include <stddef.h>");
        text.AppendLine("#include <stdint.h>");
        text.AppendLine();
        text.AppendLine("#include \"CNA/C/cna.h\"");
        text.AppendLine();

        foreach (MethodInfo method in Imports())
        {
            string entryPoint = EntryPoint(method);

            if (HeaderPrototypes.TryGetValue(entryPoint, out string? declared))
            {
                fromHeader.Add(entryPoint);
                text.AppendLine($"{declared} = {entryPoint};");
                continue;
            }

            string? returnType = CType(method.ReturnType);
            ParameterInfo[] managedParameters = method.GetParameters();
            List<string?> parameters = [];
            for (int index = 0; index < managedParameters.Length; index++)
            {
                parameters.Add(
                    ParameterOverrides.TryGetValue($"{entryPoint}#{index}", out string? spelled)
                        ? spelled
                        : ParameterType(managedParameters[index]));
            }

            if (returnType is null || parameters.Any(p => p is null))
            {
                unmappable.Add(entryPoint);
                continue;
            }

            string signature = parameters.Count == 0 ? "void" : string.Join(", ", parameters);
            text.AppendLine($"{returnType} (*const p_{entryPoint})({signature}) = {entryPoint};");
        }

        return text.ToString();
    }
}
