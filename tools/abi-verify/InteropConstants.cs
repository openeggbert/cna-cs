// SPDX-License-Identifier: MIT
using System.Reflection;

/// <summary>
/// B3: every enum-like identity this binding consumes, checked against the canonical header.
///
/// These are the values that cross the ABI as plain integers -- result codes, capability and event
/// identities, formats, presets, profiles. Nothing about a struct layout or a function prototype
/// constrains them: a renumbered constant is invisible to every other check here, compiles cleanly,
/// and produces wrong behaviour at run time rather than a failure.
///
/// Each managed member becomes a <c>_Static_assert</c> against the macro of the same identity, so
/// the C preprocessor supplies the value and the managed literal has to agree. The macro name is
/// derived (<c>CnaGraphicsProfile.HiDef</c> is <c>CNA_GRAPHICS_PROFILE_HI_DEF</c>) because a list
/// would need extending by hand for every new identity, which is how coverage decays.
/// </summary>
static class InteropConstants
{
    /// <summary>
    /// Prefixes that do not follow the derived rule, and identities deliberately not consumed.
    ///
    /// An empty prefix means the enum is checked member by member through
    /// <see cref="MemberOverrides"/> instead.
    /// </summary>
    private static readonly Dictionary<string, string> PrefixOverrides = new(StringComparer.Ordinal)
    {
        // C names the individual option, not the set.
        ["CnaClearOptions"] = "CNA_CLEAR_OPTION_",

        // C drops the "type"/"state" noun these managed names carry.
        ["CnaEffectValueType"] = "CNA_EFFECT_VALUE_",
        ["CnaTouchLocationState"] = "CNA_TOUCH_LOCATION_",
        ["CnaEffectTextureType"] = "CNA_EFFECT_TEXTURE_",

        // Same shape in CNB: C drops the trailing "slot"/"set" noun. Worth noting that the two are
        // not interchangeable despite the similar spelling -- CNA_CNB_MATERIAL_TEXTURE_* names the
        // eight *name* slots, while CNB's per-slot arrays are a seven-element importer space with a
        // different order. See CnbMaterialTextureSlotMap.
        ["CnaCnbMaterialTextureSlot"] = "CNA_CNB_MATERIAL_TEXTURE_",
        ["CnaCnbSkeletonMatrixSet"] = "CNA_CNB_SKELETON_MATRIX_",
    };

    /// <summary>Individual enum members with no macro of their own, and why.</summary>
    public static readonly HashSet<string> NotHeaderMembers = new(StringComparer.Ordinal)
    {
        // C# spells "no bits set" as a None member on a [Flags] enum; C spells it as the absence of
        // any bit and declares no macro for it. CNA_RENDERER_FORMAT_USAGE_ALL exists and
        // CNA_RENDERER_FORMAT_USAGE_NONE does not, which is the header being consistent rather than
        // incomplete. Excluded by name so a *bit* that loses its macro still fails.
        "CnaRendererFormatUsage.None",
    };

    /// <summary>
    /// Managed identities with no macro of their own in the header, and why.
    ///
    /// <c>CnaVertexType</c> and <c>CnaUserVertexSource</c> describe how this binding hands vertex
    /// data across a route; they are its own vocabulary, not CNA's, and no header declares them.
    /// </summary>
    public static readonly HashSet<string> NotHeaderIdentities = new(StringComparer.Ordinal)
    {
        "CnaVertexType",
        "CnaUserVertexSource",
    };

    /// <summary>
    /// CNA.Framework enums that CNA declares a macro group for, and whose values therefore have to
    /// agree with it.
    ///
    /// Chosen by measurement: the derived prefix for each of these exists among the 883 macro groups
    /// the headers define. The remaining framework enums are listed in
    /// <see cref="FrameworkIdentitiesWithoutAMacroGroup"/> with the reason, so the split is explicit
    /// rather than a silent subset.
    /// </summary>
    public static readonly HashSet<string> FrameworkIdentities = new(StringComparer.Ordinal)
    {
        "AudioChannels",
        "AudioStopOptions",
        "Blend",
        "BlendFunction",
        "BufferUsage",
        "CurveContinuity",
        "CurveTangent",
        "DepthFormat",
        "DisplayOrientation",
        "EffectParameterClass",
        "EffectParameterType",
        "GestureType",
        "GraphicsCapability",
        "GraphicsDeviceStatus",
        "GraphicsProfile",
        "KeyState",
        "MediaState",
        "MicrophoneState",
        "MouseCursorStock",
        "PlayerIndex",
        "PresentInterval",
        "PresentationMode",
        "ShaderDialect",
        "SoundState",
        "SpriteSortMode",
        "SurfaceFormat",
        "TextureFilter",
        "VertexElementFormat",
        "VertexElementUsage",
        "VideoSoundtrackType",
    };

    /// <summary>
    /// Framework enums CNA declares no macro group of the same identity for.
    ///
    /// Three reasons, and none of them is "unchecked". Some are pure managed XNA vocabulary that
    /// never crosses -- <c>ContainmentType</c> and <c>PlaneIntersectionType</c> are results of
    /// managed geometry. Some cross but CNA spells the group differently (<c>ClearOptions</c> is
    /// <c>CNA_CLEAR_OPTION_</c>, <c>TouchLocationState</c> is <c>CNA_TOUCH_LOCATION_</c>), and those
    /// are already checked through their CNA.Interop twin. And <c>Keys</c> is a 100-plus identity
    /// set CNA names per key rather than as a group.
    ///
    /// Recorded so that "not checked here" always has a stated reason.
    /// </summary>
    public static readonly string[] FrameworkIdentitiesWithoutAMacroGroup =
    [
        "ButtonState", "Buttons", "ClearOptions", "ColorWriteChannels", "CompareFunction",
        "ContainmentType", "CubeMapFace", "CullMode", "CurveLoopType", "FillMode",
        "GamePadDeadZone", "GamePadType", "IndexElementSize", "Keys", "MediaSourceType",
        "PlaneIntersectionType", "PrimitiveType", "RenderTargetUsage", "SetDataOptions",
        "SpriteEffects", "StencilOperation", "TextureAddressMode", "TouchLocationState",
        "UserVertexSource",
    ];

    /// <summary>Individual members whose macro is spelled differently from the derived name.</summary>
    public static readonly Dictionary<string, string> MemberOverrides = new(StringComparer.Ordinal)
    {
        // The managed names repeat the "Texture" the C prefix already carries.
        ["CnaEffectTextureType.Texture2D"] = "CNA_EFFECT_TEXTURE_2D",
        ["CnaEffectTextureType.Texture3D"] = "CNA_EFFECT_TEXTURE_3D",
        ["CnaEffectTextureType.TextureCube"] = "CNA_EFFECT_TEXTURE_CUBE",

        // Nine places where CNA spells a member differently from the derived name. Each was found
        // by the compiler refusing the derived spelling, not by reading the header hopefully.
        ["Blend.BlendFactor"] = "CNA_BLEND_FACTOR",
        ["Blend.InverseBlendFactor"] = "CNA_BLEND_INVERSE_FACTOR",
        ["DepthFormat.Depth24Stencil8"] = "CNA_DEPTH_FORMAT_DEPTH24_STENCIL8",
        ["GraphicsCapability.Texture3D"] = "CNA_GRAPHICS_CAPABILITY_TEXTURE_3D",
        ["MouseCursorStock.IBeam"] = "CNA_MOUSE_CURSOR_STOCK_IBEAM",
        ["SurfaceFormat.Bc7Ext"] = "CNA_SURFACE_FORMAT_BC7_EXT",
        ["SurfaceFormat.Bc7SrgbExt"] = "CNA_SURFACE_FORMAT_BC7_SRGB_EXT",
        ["SurfaceFormat.Dxt5SrgbExt"] = "CNA_SURFACE_FORMAT_DXT5_SRGB_EXT",
        ["SurfaceFormat.UShortExt"] = "CNA_SURFACE_FORMAT_USHORT_EXT",

        // Twelve CNB texture formats where the "a capital after a digit takes no separator" rule --
        // right for CNA_SURFACE_FORMAT_TEXTURE1D and CNA_MATH_VECTOR2 -- is wrong: cnb.h writes a
        // separator after the digit. Every one was produced by the compiler refusing the derived
        // spelling, and the rule is left as it is rather than special-cased per header, because a
        // derivation rule with a second exception clause is a rule nobody can predict.
        ["CnaCnbTextureFormat.Rgba8Srgb"] = "CNA_CNB_TEXTURE_FORMAT_RGBA8_SRGB",
        ["CnaCnbTextureFormat.Rg8Snorm"] = "CNA_CNB_TEXTURE_FORMAT_RG8_SNORM",
        ["CnaCnbTextureFormat.Rgba8Snorm"] = "CNA_CNB_TEXTURE_FORMAT_RGBA8_SNORM",
        ["CnaCnbTextureFormat.Rgb10A2"] = "CNA_CNB_TEXTURE_FORMAT_RGB10_A2",
        ["CnaCnbTextureFormat.R32Float"] = "CNA_CNB_TEXTURE_FORMAT_R32_FLOAT",
        ["CnaCnbTextureFormat.Rg32Float"] = "CNA_CNB_TEXTURE_FORMAT_RG32_FLOAT",
        ["CnaCnbTextureFormat.Rgba32Float"] = "CNA_CNB_TEXTURE_FORMAT_RGBA32_FLOAT",
        ["CnaCnbTextureFormat.R16Float"] = "CNA_CNB_TEXTURE_FORMAT_R16_FLOAT",
        ["CnaCnbTextureFormat.Rg16Float"] = "CNA_CNB_TEXTURE_FORMAT_RG16_FLOAT",
        ["CnaCnbTextureFormat.Rgba16Float"] = "CNA_CNB_TEXTURE_FORMAT_RGBA16_FLOAT",
        ["CnaCnbTextureFormat.Bc3Srgb"] = "CNA_CNB_TEXTURE_FORMAT_BC3_SRGB",
        ["CnaCnbTextureFormat.Bc7Srgb"] = "CNA_CNB_TEXTURE_FORMAT_BC7_SRGB",

        // CNA writes this one without the separator the derived name inserts.
        ["CnaRendererFormatUsage.MultiSample"] = "CNA_RENDERER_FORMAT_USAGE_MULTISAMPLE",
    };

    /// <summary>
    /// The identities to check: CNA.Interop's own <c>Cna*</c> enums, and the CNA.Framework enums
    /// whose values cross to native as plain integers.
    ///
    /// The second group matters because a framework enum that is cast to <c>uint</c> at a call site
    /// is every bit as much an ABI value as one declared in the interop assembly -- and nothing else
    /// checks it. A framework enum is included when CNA declares a macro group of the same identity;
    /// one that does not is either XNA-only vocabulary CNA has no opinion about, or a naming
    /// difference, and both are recorded rather than silently dropped.
    /// </summary>
    public static IEnumerable<Type> Enums()
    {
        IEnumerable<Type> interopEnums = typeof(CNA.Interop.CnaHandle).Assembly
            .GetTypes()
            .Where(type => type.IsEnum && type.Name.StartsWith("Cna", StringComparison.Ordinal));

        IEnumerable<Type> frameworkEnums = typeof(CNA.Game).Assembly
            .GetTypes()
            .Where(type => type is { IsEnum: true, IsPublic: true })
            .Where(type => FrameworkIdentities.Contains(type.Name));

        return interopEnums.Concat(frameworkEnums).OrderBy(type => type.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// PascalCase to the UPPER_SNAKE spelling CNA's macros use.
    ///
    /// Separate from the struct-field rule in one respect: a capital that follows a *digit* gets no
    /// separator, because C writes <c>TEXTURE1D</c>, <c>VECTOR2</c> and <c>INT32</c>. Deriving that
    /// from the field rule produced <c>TEXTURE1_D</c>, which the compiler rejected as undeclared --
    /// which is how the difference was found rather than assumed.
    /// </summary>
    private static string UpperSnake(string name)
    {
        var text = new System.Text.StringBuilder(name.Length + 8);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            bool afterLower = i > 0 && char.IsLower(name[i - 1]);
            bool endOfRun = i > 0 && char.IsUpper(name[i - 1]) &&
                            i + 1 < name.Length && char.IsLower(name[i + 1]);

            if (char.IsUpper(c) && (afterLower || endOfRun))
            {
                text.Append('_');
            }

            text.Append(char.ToUpperInvariant(c));
        }

        return text.ToString();
    }

    public static string Prefix(Type type)
    {
        if (PrefixOverrides.TryGetValue(type.Name, out string? prefix))
        {
            return prefix;
        }

        string bare = type.Name.StartsWith("Cna", StringComparison.Ordinal)
            ? type.Name["Cna".Length..]
            : type.Name;
        return "CNA_" + UpperSnake(bare) + "_";
    }

    public static string MacroName(Type type, string member) =>
        MemberOverrides.TryGetValue($"{type.Name}.{member}", out string? macro)
            ? macro
            : Prefix(type) + UpperSnake(member);

    public static string Generate(out int checkedCount, out List<string> skipped, out int skippedMembers)
    {
        checkedCount = 0;
        skipped = [];
        skippedMembers = 0;

        var text = new System.Text.StringBuilder();
        text.AppendLine("// SPDX-License-Identifier: MIT");
        text.AppendLine("// Generated by CNA.AbiVerify from the enum-like identities CNA.Interop consumes.");
        text.AppendLine();
        text.AppendLine("#include <stdint.h>");
        text.AppendLine();
        text.AppendLine("#include \"CNA/C/cna.h\"");
        text.AppendLine();

        foreach (Type type in Enums())
        {
            if (NotHeaderIdentities.Contains(type.Name))
            {
                skipped.Add(type.Name);
                continue;
            }

            text.AppendLine($"// {type.Name}");
            foreach (string member in Enum.GetNames(type))
            {
                if (NotHeaderMembers.Contains($"{type.Name}.{member}"))
                {
                    skippedMembers++;
                    continue;
                }

                object value = Enum.Parse(type, member);
                ulong numeric = Convert.ToUInt64(Convert.ChangeType(value, type.GetEnumUnderlyingType()));
                string macro = MacroName(type, member);
                text.AppendLine(
                    $"_Static_assert({macro} == {numeric}u, \"{type.Name}.{member} disagrees with {macro}\");");
                checkedCount++;
            }

            text.AppendLine();
        }

        return text.ToString();
    }
}
