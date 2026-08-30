// SPDX-License-Identifier: MIT

/// <summary>
/// Proves the prototype gate actually rejects wrong signatures.
///
/// A verifier that passes on the current tree has demonstrated nothing: it would pass just as
/// happily if it checked nothing at all. Each control below deliberately corrupts one generated
/// declaration in one specific way and requires the C compiler to refuse it. A control that
/// *compiles* is a hole in the gate and is reported as a failure.
///
/// The mutations are the ones that matter for an ABI: a wrong return type, a signedness change that
/// keeps the width, a pointer at the wrong depth, a by-ref direction flipped in either direction, a
/// different versioned descriptor, a different callback shape, and two same-width parameters
/// swapped. The last is the one a human reviewer misses most easily and the one a compiler catches
/// for free.
/// </summary>
static class PrototypeNegativeControls
{
    /// <summary>A control, and which generated unit it corrupts.</summary>
    internal sealed record Control(string Name, string Original, string Mutated, string Why, bool Layout = false);

    /// <summary>
    /// One real generated declaration, and the corruption each control applies to it.
    ///
    /// The originals are written out rather than looked up so a control cannot silently stop
    /// testing anything when a route changes: if the original no longer appears in the generated
    /// unit, the control reports that instead of quietly passing.
    /// </summary>
    public static IReadOnlyList<Control> All() =>
    [
        new("wrong-return-type",
            "uint32_t (*const p_cna_game_run)(CNA_Handle) = cna_game_run;",
            "void* (*const p_cna_game_run)(CNA_Handle) = cna_game_run;",
            "a route that returns a result code must not pass as one returning a pointer"),

        new("signedness-same-width",
            "uint32_t (*const p_cna_game_components_get_at)(CNA_Handle, uint64_t, CNA_Handle*) = cna_game_components_get_at;",
            "uint32_t (*const p_cna_game_components_get_at)(CNA_Handle, int64_t, CNA_Handle*) = cna_game_components_get_at;",
            "the index is unsigned; a signed parameter of the same width is still the wrong type"),

        new("pointer-depth",
            "uint32_t (*const p_cna_game_run)(CNA_Handle) = cna_game_run;",
            "uint32_t (*const p_cna_game_run)(CNA_Handle*) = cna_game_run;",
            "a handle passed by value must not pass as a handle passed by pointer"),

        new("in-becomes-out",
            "uint32_t (*const p_cna_game_create)(const CNA_GameCreateInfo*, CNA_Handle*) = cna_game_create;",
            "uint32_t (*const p_cna_game_create)(CNA_GameCreateInfo*, CNA_Handle*) = cna_game_create;",
            "a read-only descriptor must not pass as a writable one"),

        new("out-becomes-in",
            "uint32_t (*const p_cna_game_create)(const CNA_GameCreateInfo*, CNA_Handle*) = cna_game_create;",
            "uint32_t (*const p_cna_game_create)(const CNA_GameCreateInfo*, const CNA_Handle*) = cna_game_create;",
            "an output parameter must not pass as a read-only one"),

        new("wrong-descriptor-struct",
            "uint32_t (*const p_cna_game_create)(const CNA_GameCreateInfo*, CNA_Handle*) = cna_game_create;",
            "uint32_t (*const p_cna_game_create)(const CNA_SoundEffectCreateInfo*, CNA_Handle*) = cna_game_create;",
            "one versioned descriptor must not pass as another"),

        new("wrong-callback-shape",
            "uint32_t (*const p_cna_game_window_subscribe)(CNA_Handle, uint32_t, void (*)(void*), void*, CNA_Handle*) = cna_game_window_subscribe;",
            "uint32_t (*const p_cna_game_window_subscribe)(CNA_Handle, uint32_t, void (*)(int32_t), void*, CNA_Handle*) = cna_game_window_subscribe;",
            "a callback taking a context pointer must not pass as one taking an int"),

        new("swapped-same-width-parameters",
            "uint32_t (*const p_cna_texture2d_create)(CNA_Handle, const CNA_Texture2DCreateInfo*, CNA_Handle*) = cna_texture2d_create;",
            "uint32_t (*const p_cna_texture2d_create)(const CNA_Texture2DCreateInfo*, CNA_Handle, CNA_Handle*) = cna_texture2d_create;",
            "two pointer-width parameters in the wrong order must not pass"),

        new("field-signedness",
            "uint32_t* const pf_CNA_SpriteScaledCommand_effects = &s_CNA_SpriteScaledCommand.effects;",
            "int32_t* const pf_CNA_SpriteScaledCommand_effects = &s_CNA_SpriteScaledCommand.effects;",
            "a struct field's signedness must be checked; the offsets agree either way",
            Layout: true),

        new("field-wrong-width",
            "uint32_t* const pf_CNA_SpriteScaledCommand_effects = &s_CNA_SpriteScaledCommand.effects;",
            "uint64_t* const pf_CNA_SpriteScaledCommand_effects = &s_CNA_SpriteScaledCommand.effects;",
            "a struct field's width must be checked",
            Layout: true),

        new("absent-import",
            "uint32_t (*const p_cna_game_run)(CNA_Handle) = cna_game_run;",
            "uint32_t (*const p_cna_game_run_that_does_not_exist)(CNA_Handle) = cna_game_run_that_does_not_exist;",
            "an import the headers do not declare must not pass as verified"),
    ];
}
