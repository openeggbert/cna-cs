using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using CNA.Interop;

string? includeDirectory = null;
string? outputPath = null;
string? compiler = Environment.GetEnvironmentVariable("CC");

for (int index = 0; index < args.Length; index++)
{
    switch (args[index])
    {
        case "--include":
            includeDirectory = RequireValue(args, ref index);
            break;
        case "--output":
            outputPath = RequireValue(args, ref index);
            break;
        case "--compiler":
            compiler = RequireValue(args, ref index);
            break;
        default:
            throw new ArgumentException($"Unknown argument: {args[index]}");
    }
}

if (string.IsNullOrWhiteSpace(includeDirectory))
{
    Console.Error.WriteLine("Usage: CNA.AbiVerify --include <CNA C include directory> [--compiler <cc>] [--output <report.json>]");
    return 2;
}

includeDirectory = Path.GetFullPath(includeDirectory);
if (!File.Exists(Path.Combine(includeDirectory, "CNA", "C", "cna.h")))
{
    Console.Error.WriteLine($"CNA umbrella header was not found below: {includeDirectory}");
    return 2;
}

bool selfTest = Environment.GetEnvironmentVariable("CNA_ABI_SELF_TEST") == "1";
compiler = ResolveCompiler(compiler);
string sourceDirectory = AppContext.BaseDirectory;
string temporaryDirectoryForSource = Path.Combine(Path.GetTempPath(), $"cna-abi-src-{Guid.NewGuid():N}");
Directory.CreateDirectory(temporaryDirectoryForSource);
// Generated from the managed structs rather than hand-written, which is what makes B1's
// completion criterion automatic: every struct CNA.Interop declares emits both a managed value and
// a C line, so a managed struct with no native counterpart is a *compile error* in the generated
// probe rather than a struct quietly nobody measured. The hand-written probe measured fourteen of
// the eighty-two.
string layoutSource = Path.Combine(temporaryDirectoryForSource, "native_layout_probe.c");
// Generated from CNA.Interop.Native for the same reason the layout probe is: a hand-written probe
// measures whatever someone added, and this one had four routes out of 861.
string prototypeSource = Path.Combine(temporaryDirectoryForSource, "native_prototype_probe.c");
string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"cna-abi-{Guid.NewGuid():N}");
Directory.CreateDirectory(temporaryDirectory);
string layoutUnit = InteropLayout.Generate();
File.WriteAllText(layoutSource, layoutUnit);
string prototypeUnit = InteropPrototypes.Generate(
    out List<string> prototypeUnmappable, out List<string> prototypeFromHeader);
File.WriteAllText(prototypeSource, prototypeUnit);
string callbackSource = Path.Combine(temporaryDirectoryForSource, "native_callback_probe.c");
string callbackUnit = InteropCallbacks.Generate(out List<string> callbackUnresolved);
File.WriteAllText(callbackSource, callbackUnit);
string constantSource = Path.Combine(temporaryDirectoryForSource, "native_constant_probe.c");
string constantUnit = InteropConstants.Generate(
    out int constantsChecked, out List<string> constantsSkipped, out int constantsSkippedMembers);
File.WriteAllText(constantSource, constantUnit);

try
{
    string executable = Path.Combine(temporaryDirectory, OperatingSystem.IsWindows() ? "native-layout.exe" : "native-layout");
    string objectPath = Path.Combine(temporaryDirectory, OperatingSystem.IsWindows() ? "native-prototype.obj" : "native-prototype.o");
    bool isMsvc = Path.GetFileNameWithoutExtension(compiler).Equals("cl", StringComparison.OrdinalIgnoreCase);

    List<string> layoutArguments = isMsvc
        ? ["/nologo", "/std:c11", "/W4", "/WX", $"/I{includeDirectory}", layoutSource, $"/Fe:{executable}"]
        : ["-std=c11", "-Wall", "-Wextra", "-Werror", "-I", includeDirectory, layoutSource, "-o", executable];
    Run(compiler, layoutArguments, temporaryDirectory);

    List<string> prototypeArguments = isMsvc
        ? ["/nologo", "/std:c11", "/W4", "/WX", "/c", $"/I{includeDirectory}", prototypeSource, $"/Fo:{objectPath}"]
        : ["-std=c11", "-Wall", "-Wextra", "-Werror", "-c", "-I", includeDirectory, prototypeSource, "-o", objectPath];
    Run(compiler, prototypeArguments, temporaryDirectory);

    // The callbacks this binding provides, against the typedefs CNA declares for them.
    string callbackObject = Path.Combine(temporaryDirectory, OperatingSystem.IsWindows() ? "native-callback.obj" : "native-callback.o");
    List<string> callbackArguments = isMsvc
        ? ["/nologo", "/std:c11", "/W4", "/WX", "/c", $"/I{includeDirectory}", callbackSource, $"/Fo:{callbackObject}"]
        : ["-std=c11", "-Wall", "-Wextra", "-Werror", "-c", "-I", includeDirectory, callbackSource, "-o", callbackObject];
    Run(compiler, callbackArguments, temporaryDirectory);

    // B3: the enum-like identities, whose values no other check constrains.
    string constantObject = Path.Combine(temporaryDirectory, OperatingSystem.IsWindows() ? "native-constant.obj" : "native-constant.o");
    List<string> constantArguments = isMsvc
        ? ["/nologo", "/std:c11", "/W4", "/WX", "/c", $"/I{includeDirectory}", constantSource, $"/Fo:{constantObject}"]
        : ["-std=c11", "-Wall", "-Wextra", "-Werror", "-c", "-I", includeDirectory, constantSource, "-o", constantObject];
    Run(compiler, constantArguments, temporaryDirectory);

    string nativeOutput = Run(executable, [], temporaryDirectory);
    Dictionary<string, long> native = ParseValues(nativeOutput);
    if (!native.Remove("abi.version", out long nativeAbiValue) || nativeAbiValue is < 0 or > uint.MaxValue)
    {
        throw new InvalidDataException("The native layout probe did not report a valid encoded CNA ABI version.");
    }

    uint nativeAbiVersion = (uint)nativeAbiValue;
    Dictionary<string, long> managed = BuildManagedValues();
    List<object> mismatches = [];

    CnaNativeAbiProfile? acceptedProfile = null;
    if (CnaNativeAbiPolicy.TryGetProfile(nativeAbiVersion, out CnaNativeAbiProfile profile))
    {
        acceptedProfile = profile;
    }
    else
    {
        mismatches.Add(new
        {
            key = "abi.version",
            native = (long?)nativeAbiVersion,
            managed = (long?)CnaNativeAbiPolicy.ConsumerVersion,
            issue = CnaNativeAbiPolicy.ExplainRejection(nativeAbiVersion),
        });
    }

    foreach ((string key, long managedValue) in managed.OrderBy(pair => pair.Key, StringComparer.Ordinal))
    {
        if (!native.TryGetValue(key, out long nativeValue))
        {
            mismatches.Add(new { key, native = (long?)null, managed = managedValue, issue = "missing-native-value" });
        }
        else if (nativeValue != managedValue)
        {
            mismatches.Add(new { key, native = (long?)nativeValue, managed = managedValue, issue = "value-mismatch" });
        }
    }

    foreach (string extra in native.Keys.Except(managed.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
    {
        mismatches.Add(new { key = extra, native = (long?)native[extra], managed = (long?)null, issue = "missing-managed-value" });
    }

    List<string> prototypeDiagnostics = VerifyManagedPrototypes();
    foreach (string diagnostic in prototypeDiagnostics)
    {
        mismatches.Add(new { key = "prototype", native = (long?)null, managed = (long?)null, issue = diagnostic });
    }

    string compilerVersion = GetCompilerVersion(compiler, isMsvc);
    if (selfTest)
    {
        int rejected = 0;
        int holes = 0;
        int stale = 0;

        foreach (PrototypeNegativeControls.Control control in PrototypeNegativeControls.All())
        {
            string unit = control.In switch
            {
                PrototypeNegativeControls.Unit.Layout => layoutUnit,
                PrototypeNegativeControls.Unit.Constants => constantUnit,
                _ => prototypeUnit,
            };
            if (!unit.Contains(control.Original, StringComparison.Ordinal))
            {
                // The declaration this control corrupts is no longer generated, so the control is
                // testing nothing. Reported rather than skipped: a silently inert control is worse
                // than none, because it still counts as coverage.
                stale++;
                Console.WriteLine($"PROTO_CONTROL_STALE={control.Name}");
                continue;
            }

            string mutatedPath = Path.Combine(temporaryDirectory, $"mutation-{control.Name}.c");
            File.WriteAllText(
                mutatedPath,
                unit.Replace(control.Original, control.Mutated, StringComparison.Ordinal));

            string mutantObject = Path.Combine(temporaryDirectory, $"mutation-{control.Name}.o");
            List<string> mutationArguments = isMsvc
                ? ["/nologo", "/std:c11", "/W4", "/WX", "/c", $"/I{includeDirectory}", mutatedPath, $"/Fo:{mutantObject}"]
                : ["-std=c11", "-Wall", "-Wextra", "-Werror", "-c", "-I", includeDirectory, mutatedPath, "-o", mutantObject];

            if (TryRun(compiler, mutationArguments, temporaryDirectory))
            {
                holes++;
                Console.WriteLine($"PROTO_CONTROL_NOT_REJECTED={control.Name} ({control.Why})");
            }
            else
            {
                rejected++;
            }
        }

        Console.WriteLine($"PROTO_CONTROLS={PrototypeNegativeControls.All().Count}");
        Console.WriteLine($"PROTO_CONTROLS_REJECTED={rejected}");
        Console.WriteLine($"PROTO_CONTROLS_STALE={stale}");
        Console.WriteLine($"PROTO_CONTROLS_NOT_REJECTED={holes}");

        if (holes > 0 || stale > 0)
        {
            mismatches.Add(new
            {
                key = "prototype-self-test",
                native = (long?)null,
                managed = (long?)null,
                issue = $"{holes} negative control(s) compiled and {stale} were stale",
            });
        }
    }

    var report = new
    {
        schemaVersion = 1,
        status = mismatches.Count == 0 ? "passed" : "failed",
        platform = RuntimeInformation.OSDescription,
        rid = RuntimeInformation.RuntimeIdentifier,
        architecture = RuntimeInformation.ProcessArchitecture.ToString(),
        compiler,
        compilerVersion,
        includeDirectory,
        abiPolicyVersion = CnaNativeAbiPolicy.PolicyVersion,
        headerAbiVersion = FormatVersion(nativeAbiVersion),
        headerAbiProfile = acceptedProfile?.Compatibility ?? "rejected",
        categories = new[]
        {
            "sizeof", "alignof", "field-offsets", "enum-widths", "bool-representation",
            "handles", "string-views", "callbacks", "return-types", "parameter-types",
            "pointer-depth", "signedness", "calling-convention"
        },
        nativeValueCount = native.Count,
        managedValueCount = managed.Count,
        prototypeCompileStatus = "passed",
        mismatchCount = mismatches.Count,
        mismatches,
    };

    string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n";
    if (outputPath is not null)
    {
        outputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, json, new System.Text.UTF8Encoding(false));
    }

    Console.WriteLine($"ABI_PLATFORM={RuntimeInformation.RuntimeIdentifier}");
    Console.WriteLine($"ABI_COMPILER={compiler}");
    Console.WriteLine($"ABI_HEADER_VERSION={FormatVersion(nativeAbiVersion)}");
    Console.WriteLine($"ABI_POLICY={CnaNativeAbiPolicy.PolicyVersion}");
    Console.WriteLine($"ABI_NATIVE_VALUES={native.Count}");
    Console.WriteLine($"ABI_MANAGED_VALUES={managed.Count}");
    // B2: every import CNA.Interop declares now carries signature evidence. The counts are printed
    // rather than merely asserted so that a shrinking number is visible in a log.
    int importCount = InteropPrototypes.Imports().Count();
    Console.WriteLine($"PROTO_IMPORTS={importCount}");
    Console.WriteLine($"PROTO_VERIFIED={importCount - prototypeUnmappable.Count}");
    Console.WriteLine($"PROTO_PARAM_OVERRIDES={InteropPrototypes.ParameterOverrides.Count}");
    Console.WriteLine($"PROTO_UNMAPPABLE={prototypeUnmappable.Count}");
    Console.WriteLine($"CALLBACKS_CHECKED={InteropCallbacks.Pairings.Length - callbackUnresolved.Count}");
    Console.WriteLine($"CALLBACKS_UNRESOLVED={callbackUnresolved.Count}");
    Console.WriteLine($"CONSTANTS_CHECKED={constantsChecked}");
    Console.WriteLine($"CONSTANTS_NOT_HEADER_IDENTITIES={constantsSkipped.Count}");
    Console.WriteLine($"CONSTANTS_NOT_HEADER_MEMBERS={constantsSkippedMembers}");
    Console.WriteLine(
        $"CONSTANTS_FRAMEWORK_WITHOUT_MACRO_GROUP={InteropConstants.FrameworkIdentitiesWithoutAMacroGroup.Length}");
    if (constantsSkipped.Count > 0)
    {
        Console.WriteLine($"CONSTANTS_NOT_HEADER_IDENTITY_NAMES={string.Join(",", constantsSkipped)}");
    }
    if (callbackUnresolved.Count > 0)
    {
        Console.WriteLine($"CALLBACKS_UNRESOLVED_NAMES={string.Join(",", callbackUnresolved)}");
        mismatches.Add(new
        {
            key = "callback-pairing",
            native = (long?)null,
            managed = (long?)null,
            issue = $"could not find managed callback(s): {string.Join(", ", callbackUnresolved)}",
        });
    }
    if (prototypeUnmappable.Count > 0)
    {
        Console.WriteLine($"PROTO_UNMAPPABLE_NAMES={string.Join(",", prototypeUnmappable)}");
    }

    Console.WriteLine($"ABI_MISMATCHES={mismatches.Count}");
    Console.WriteLine($"ABI_STATUS={report.status}");
    return mismatches.Count == 0 ? 0 : 1;
}
finally
{
    Directory.Delete(temporaryDirectory, recursive: true);
}

static string FormatVersion(uint version) =>
    $"{(version >> 16) & 0xFFFF}.{(version >> 8) & 0xFF}.{version & 0xFF}";

static string RequireValue(string[] values, ref int index)
{
    if (++index >= values.Length)
    {
        throw new ArgumentException($"Missing value after {values[index - 1]}");
    }

    return values[index];
}

static string ResolveCompiler(string? configured)
{
    if (!string.IsNullOrWhiteSpace(configured))
    {
        return configured;
    }

    string[] candidates = OperatingSystem.IsWindows() ? ["cl", "clang", "gcc"] : ["cc", "clang", "gcc"];
    foreach (string candidate in candidates)
    {
        try
        {
            Process process = Process.Start(new ProcessStartInfo(candidate, "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            })!;
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                return candidate;
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }

    throw new InvalidOperationException("No platform C compiler was found. Set CC or pass --compiler.");
}


// Runs a command and reports whether it succeeded, for the negative controls, where a failure is
// the expected and required outcome.
static bool TryRun(string executable, IReadOnlyList<string> arguments, string workingDirectory)
{
    try
    {
        Run(executable, arguments, workingDirectory);
        return true;
    }
    catch (InvalidOperationException)
    {
        return false;
    }
}

static string Run(string executable, IReadOnlyList<string> arguments, string workingDirectory)
{
    ProcessStartInfo start = new(executable)
    {
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    foreach (string argument in arguments)
    {
        start.ArgumentList.Add(argument);
    }

    using Process process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {executable}.");

    // Both pipes are drained concurrently. Reading one to the end and then the other deadlocks the
    // moment the child fills the pipe it is not being read from -- which a C compiler does as soon
    // as it has a hundred diagnostics to report, and did: the first full prototype run produced no
    // output at all and hung until it was killed.
    Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
    Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
    string standardOutput = standardOutputTask.GetAwaiter().GetResult();
    string standardError = standardErrorTask.GetAwaiter().GetResult();
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            $"Command failed ({process.ExitCode}): {executable} {string.Join(' ', arguments)}\n{standardOutput}{standardError}");
    }

    return standardOutput;
}

static string GetCompilerVersion(string compiler, bool isMsvc)
{
    try
    {
        string output = Run(compiler, isMsvc ? [] : ["--version"], Directory.GetCurrentDirectory());
        return output.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "unknown";
    }
    catch
    {
        return "unknown";
    }
}

static Dictionary<string, long> ParseValues(string output)
{
    Dictionary<string, long> result = new(StringComparer.Ordinal);
    foreach (string line in output.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
        int separator = line.IndexOf('=');
        if (separator <= 0 || !long.TryParse(line[(separator + 1)..], out long value) || !result.TryAdd(line[..separator], value))
        {
            throw new InvalidDataException($"Invalid or duplicate native layout observation: {line}");
        }
    }

    return result;
}

static Dictionary<string, long> BuildManagedValues()
{
    Dictionary<string, long> values = new(StringComparer.Ordinal)
    {
        ["sizeof.void*"] = IntPtr.Size,
        ["alignof.void*"] = AlignmentOf<nint>(),
        ["sizeof.CNA_Result"] = Unsafe.SizeOf<CnaResult>(),
        ["alignof.CNA_Result"] = AlignmentOf<CnaResult>(),
        ["sizeof.CNA_Bool"] = Unsafe.SizeOf<byte>(),
        ["alignof.CNA_Bool"] = AlignmentOf<byte>(),
        ["sizeof.CNA_GraphicsDeviceEvent"] = Unsafe.SizeOf<CnaGraphicsDeviceEvent>(),
        ["sizeof.CNA_GraphicsProfile"] = Unsafe.SizeOf<CnaGraphicsProfile>(),
    };

    foreach (Type type in InteropLayout.Structs())
    {
        string native = InteropLayout.NativeName(type);
        Record(values, $"sizeof.{native}", Marshal.SizeOf(type), type);
        Record(values, $"alignof.{native}", AlignmentOfType(type), type);

        if (InteropLayout.IsScalarTypedef(type))
        {
            continue;
        }

        bool paddingRunMeasured = false;
        foreach (FieldInfo field in InteropLayout.Fields(type))
        {
            if (InteropLayout.IsPadding(field.Name))
            {
                if (paddingRunMeasured)
                {
                    continue;
                }

                paddingRunMeasured = true;
                Record(values, $"offsetof.{native}.reserved", Marshal.OffsetOf(type, field.Name).ToInt64(), type);
                continue;
            }

            if (InteropLayout.Skip(type, field))
            {
                continue;
            }

            Record(
                values,
                $"offsetof.{native}.{InteropLayout.FieldName(type, field)}",
                Marshal.OffsetOf(type, field.Name).ToInt64(),
                type);
        }
    }

    return values;
}

// Records a measurement, refusing to let two managed spellings of one C type disagree. CnaRect and
// CnaRectangle are both CNA_Rectangle; letting the second assignment win would hide exactly the case
// worth catching -- two managed views of one native type that have drifted apart -- behind a
// dictionary write.
static void Record(Dictionary<string, long> values, string key, long value, Type type)
{
    if (values.TryGetValue(key, out long existing) && existing != value)
    {
        throw new InvalidDataException(
            $"Two managed types disagree about {key}: {existing} was already measured, and " +
            $"{type.Name} measures {value}.");
    }

    values[key] = value;
}

// The same measurement AlignmentOf<T> makes, for a type only known at run time: a byte followed by
// the type, so the padding the compiler inserts is the alignment.
static int AlignmentOfType(Type type)
{
    Type probe = typeof(AlignmentProbe<>).MakeGenericType(type);
    return checked((int)Marshal.OffsetOf(probe, "Value").ToInt64());
}

static int AlignmentOf<T>() where T : unmanaged =>
    checked((int)Marshal.OffsetOf<AlignmentProbe<T>>(nameof(AlignmentProbe<T>.Value)));

static List<string> VerifyManagedPrototypes()
{
    List<string> failures = [];
    VerifyMethod("cna_game_create", typeof(CnaResult), failures,
        typeof(CnaGameCreateInfo).MakeByRefType(), typeof(CnaHandle).MakeByRefType());
    VerifyMethod("cna_graphics_device_subscribe_event", typeof(CnaResult), failures,
        typeof(CnaHandle), typeof(uint), typeof(nint), typeof(nint), typeof(CnaHandle).MakeByRefType());
    VerifyMethod("cna_sound_effect_instance_apply_3d_multi_ext", typeof(CnaResult), failures,
        typeof(CnaHandle), typeof(CnaAudioListener).MakePointerType(), typeof(ulong), typeof(CnaAudioEmitter).MakeByRefType());
    VerifyMethod("cna_audio_engine_create_with_renderer", typeof(CnaResult), failures,
        typeof(CnaHandle), typeof(CnaStringView), typeof(long), typeof(CnaStringView), typeof(CnaHandle).MakeByRefType());

    foreach (string fieldName in new[] { "LoadContent", "Update", "Draw", "UnloadContent", "Exiting" })
    {
        Type fieldType = typeof(CnaManagedGameCallbacks).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public)!.FieldType;
        Type[] conventions = fieldType.GetFunctionPointerCallingConventions();
        if (!fieldType.IsFunctionPointer || (conventions.Length > 0 && !conventions.Contains(typeof(CallConvCdecl))))
        {
            failures.Add($"CnaManagedGameCallbacks.{fieldName} is not an unmanaged Cdecl function pointer " +
                $"(reported conventions: {string.Join(", ", conventions.Select(type => type.Name))})");
        }
    }

    return failures;
}

static void VerifyMethod(string name, Type returnType, List<string> failures, params Type[] parameterTypes)
{
    MethodInfo? method = typeof(Native).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
    if (method is null)
    {
        failures.Add($"managed method {name} is missing");
        return;
    }

    if (method.ReturnType != returnType)
    {
        failures.Add($"{name} return type is {method.ReturnType}, expected {returnType}");
    }

    Type[] actual = method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
    if (!actual.SequenceEqual(parameterTypes))
    {
        failures.Add($"{name} parameter types are [{string.Join(", ", actual.Select(type => type.ToString()))}]");
    }
}

[StructLayout(LayoutKind.Sequential)]
struct AlignmentProbe<T> where T : unmanaged
{
    public byte Prefix;
    public T Value;
}

/// <summary>
/// Every struct CNA.Interop declares, and how its name and fields map onto CNA's C names.
///
/// The mapping is derived, not listed: <c>CnaFoo</c> is <c>CNA_Foo</c> and <c>StructSize</c> is
/// <c>struct_size</c>. Derivation is what makes this cover all of them -- a list would have to be
/// extended by hand for every new struct, which is exactly how the hand-written probe ended up
/// measuring fourteen of eighty-two.
///
/// <see cref="Overrides"/> carries the names that do not follow the rule, and
/// <see cref="NotPartOfTheAbi"/> the managed-only helpers that have no C counterpart at all. Both
/// are small, explicit and have to be justified, which is the point: an unexplained gap is a
/// finding, not a silence.
/// </summary>
static class InteropLayout
{
    private static readonly Dictionary<string, string> Overrides = new(StringComparer.Ordinal)
    {
        ["CnaManagedGameCallbacks"] = "CNA_GameCallbacks",

        // Two managed spellings of one C type. Both are measured, and a disagreement between them
        // is raised rather than silently overwritten -- see the duplicate check in
        // BuildManagedValues.
        ["CnaRect"] = "CNA_Rectangle",

        // The buffered sprite path submits through cna_sprite_batch_submit_scaled_many, whose
        // element type C calls CNA_SpriteScaledCommand.
        ["CnaSpriteDrawCommand"] = "CNA_SpriteScaledCommand",

        // C spells the extension suffix in capitals. Derivation would ask for CNA_VideoFrameExt,
        // and the compiler refusing that is how this entry came to exist.
        ["CnaVideoFrameExt"] = "CNA_VideoFrameEXT",
    };

    /// <summary>
    /// Types whose C counterpart is a scalar typedef rather than a struct, so there are no member
    /// offsets to take. <c>CNA_Handle</c> is a <c>uint64_t</c>; the managed side wraps it in a
    /// one-field struct, and <c>offsetof</c> on a typedef does not compile.
    /// </summary>
    private static readonly HashSet<string> ScalarTypedefs = new(StringComparer.Ordinal)
    {
        "CnaHandle",
    };

    public static bool IsScalarTypedef(Type type) => ScalarTypedefs.Contains(type.Name);

    private static readonly HashSet<string> NotPartOfTheAbi = new(StringComparer.Ordinal)
    {
        // A generic helper this verifier uses to measure alignment; it is not an ABI type.
        "AlignmentProbe`1",

        // Managed-only. CnaFloatBuffer256 is a fixed-size scratch buffer this binding uses to hand
        // effect parameter data across; CnaNativeAbiProfile is the admission policy's own record.
        // Neither is declared in any CNA header, so neither has a layout to agree with.
        "CnaFloatBuffer256",
        "CnaNativeAbiProfile",

        // How this binding spells a run of padding bytes inside another struct. C writes the run
        // inline as uint8_t reserved[N], so there is no separate type to measure -- the containing
        // struct's own offsets and size are what has to agree, and they are measured.
        "CnaReservedBytes2",
        "CnaReservedBytes3",
        "CnaReservedBytes5",
        "CnaReservedBytes7",
    };

    public static IEnumerable<Type> Structs() =>
        typeof(CnaHandle).Assembly
            .GetTypes()
            .Where(type => type.IsValueType && !type.IsEnum && !type.IsGenericType)
            .Where(type => type.Name.StartsWith("Cna", StringComparison.Ordinal))
            .Where(type => !NotPartOfTheAbi.Contains(type.Name))
            .OrderBy(type => type.Name, StringComparer.Ordinal);

    public static string NativeName(Type type) =>
        Overrides.TryGetValue(type.Name, out string? mapped)
            ? mapped
            : "CNA_" + type.Name["Cna".Length..];

    public static IEnumerable<FieldInfo> Fields(Type type) =>
        type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => !field.IsStatic);

    /// <summary>PascalCase to snake_case: a separator before any capital that follows a lowercase
    /// letter or a digit, so <c>TargetElapsedTimeTicks</c> becomes <c>target_elapsed_time_ticks</c>
    /// and <c>Reserved0</c> stays <c>reserved0</c>.</summary>
    public static string Generate()
    {
        var text = new System.Text.StringBuilder();
        text.AppendLine("// SPDX-License-Identifier: MIT");
        text.AppendLine("// Generated by CNA.AbiVerify from the structs CNA.Interop declares. Do not edit.");
        text.AppendLine();
        text.AppendLine("#include <stddef.h>");
        text.AppendLine("#include <stdint.h>");
        text.AppendLine("#include <stdio.h>");
        text.AppendLine();
        text.AppendLine("#include \"CNA/C/cna.h\"");
        text.AppendLine();
        text.AppendLine("#define PRINT_SIZE(type) printf(\"sizeof.\" #type \"=%zu\\n\", sizeof(type))");
        text.AppendLine("#define PRINT_ALIGN(type) printf(\"alignof.\" #type \"=%zu\\n\", _Alignof(type))");
        text.AppendLine("#define PRINT_OFFSET(type, field) printf(\"offsetof.\" #type \".\" #field \"=%zu\\n\", offsetof(type, field))");
        text.AppendLine();
        var fieldChecked = new HashSet<string>(StringComparer.Ordinal);

        // Field *types*, not only their offsets.
        //
        // The offsets agree whenever the widths do, so a field declared int against a uint32_t is
        // invisible to everything above -- and CnaUserPrimitives.PrimitiveType was exactly that
        // until B2's prototype work found it from the other side. Taking the address of each field
        // and assigning it to a pointer to the managed-derived type makes the difference a
        // diagnostic: &s.primitive_type is an int32_t* and will not initialise a uint32_t*.
        //
        // Padding runs are skipped: C declares them as arrays, so their address is a pointer to
        // array rather than to element, and padding is the one thing whose type carries no meaning.
        foreach (Type type in InteropLayout.Structs())
        {
            if (InteropLayout.IsScalarTypedef(type))
            {
                continue;
            }

            string native = InteropLayout.NativeName(type);
            if (!fieldChecked.Add(native))
            {
                continue;
            }

            text.AppendLine($"static {native} s_{native};");
            foreach (FieldInfo field in InteropLayout.Fields(type))
            {
                if (InteropLayout.IsPadding(field.Name) || InteropLayout.Skip(type, field))
                {
                    continue;
                }

                string fieldName = InteropLayout.FieldName(type, field);
                if (InteropLayout.PaddingLikeFieldNames.Contains(fieldName))
                {
                    continue;
                }

                string? spelled = InteropLayout.FieldTypeOverrides.TryGetValue(
                    $"{native}.{fieldName}", out string? overridden)
                    ? overridden
                    : InteropPrototypes.CType(field.FieldType);
                if (spelled is null)
                {
                    continue;
                }

                if (spelled.Length == 0)
                {
                    continue;
                }

                string declaration = spelled.Contains("(*)", StringComparison.Ordinal)
                    ? spelled.Replace("(*)", $"(**const pf_{native}_{fieldName})", StringComparison.Ordinal)
                    : $"{spelled}* const pf_{native}_{fieldName}";
                text.AppendLine($"{declaration} = &s_{native}.{fieldName};");
            }

            text.AppendLine();
        }

        text.AppendLine("int main(void)");
        text.AppendLine("{");
        text.AppendLine("    printf(\"abi.version=%u\\n\", CNA_ABI_VERSION);");
        text.AppendLine("    PRINT_SIZE(void*);");
        text.AppendLine("    PRINT_ALIGN(void*);");
        text.AppendLine("    PRINT_SIZE(CNA_Result);");
        text.AppendLine("    PRINT_ALIGN(CNA_Result);");
        text.AppendLine("    PRINT_SIZE(CNA_Bool);");
        text.AppendLine("    PRINT_ALIGN(CNA_Bool);");
        text.AppendLine("    PRINT_SIZE(CNA_GraphicsDeviceEvent);");
        text.AppendLine("    PRINT_SIZE(CNA_GraphicsProfile);");

        var emitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (Type type in InteropLayout.Structs())
        {
            string native = InteropLayout.NativeName(type);

            // One C type, however many managed spellings it has. The managed side measures each
            // spelling and refuses to let them disagree, so emitting the C type twice would only
            // duplicate identical lines.
            if (!emitted.Add(native))
            {
                continue;
            }

            text.AppendLine();
            text.AppendLine($"    PRINT_SIZE({native});");
            text.AppendLine($"    PRINT_ALIGN({native});");
            if (InteropLayout.IsScalarTypedef(type))
            {
                continue;
            }

            bool paddingRunEmitted = false;
            foreach (FieldInfo field in InteropLayout.Fields(type))
            {
                if (InteropLayout.IsPadding(field.Name))
                {
                    if (paddingRunEmitted)
                    {
                        continue;
                    }

                    paddingRunEmitted = true;
                    text.AppendLine($"    PRINT_OFFSET({native}, reserved);");
                    continue;
                }

                if (InteropLayout.Skip(type, field))
                {
                    continue;
                }

                text.AppendLine($"    PRINT_OFFSET({native}, {InteropLayout.FieldName(type, field)});");
            }
        }

        text.AppendLine();
        text.AppendLine("    return 0;");
        text.AppendLine("}");
        return text.ToString();
    }

    /// <summary>
    /// PascalCase to snake_case.
    ///
    /// A separator goes before a capital that follows a lowercase letter or a digit, and also before
    /// the last capital of a run when a lowercase follows it -- so <c>HasYButton</c> is
    /// <c>has_y_button</c> rather than <c>has_ybutton</c>, which is what the simpler rule produced
    /// and what the C compiler rejected.
    /// </summary>
    public static string NativeField(string managed)
    {
        var text = new System.Text.StringBuilder(managed.Length + 8);
        for (int i = 0; i < managed.Length; i++)
        {
            char c = managed[i];
            bool afterLowerOrDigit = i > 0 && (char.IsLower(managed[i - 1]) || char.IsDigit(managed[i - 1]));
            bool endOfCapitalRun = i > 0 && char.IsUpper(managed[i - 1]) &&
                                   i + 1 < managed.Length && char.IsLower(managed[i + 1]);

            if (char.IsUpper(c) && (afterLowerOrDigit || endOfCapitalRun))
            {
                text.Append('_');
            }

            text.Append(char.ToLowerInvariant(c));
        }

        return text.ToString();
    }

    /// <summary>
    /// Whether a field is explicit padding, spelled here as a run of separate bytes.
    ///
    /// C declares padding as one array -- <c>uint8_t reserved[5]</c> -- and C# cannot express that
    /// in a struct without <c>fixed</c> and <c>unsafe</c>, so the managed side spells it
    /// <c>Reserved0</c>..<c>Reserved4</c>. That is a representation difference, not an ABI one: the
    /// first byte of the run sits at the array's offset and the rest follow by construction, and the
    /// struct's total size still has to agree. So the run is measured at its first element, against
    /// the array's own name.
    /// </summary>
    /// <summary>
    /// Field types C# cannot spell, keyed <c>CNA_Struct.field</c>. An empty value means the field is
    /// measured for offset only.
    ///
    /// The reasons are the same three as the prototype manifest's, in a struct instead of a
    /// parameter list: <c>const</c> that C# cannot put on a pointer, C's <c>char</c>, and a C array
    /// member, whose address is a pointer-to-array and which this binding models as a struct. None
    /// is an ABI difference; the offsets and the total size still have to agree, and they do.
    /// </summary>
    public static readonly Dictionary<string, string> FieldTypeOverrides = new(StringComparer.Ordinal)
    {
        ["CNA_GameCallbacks.draw"] = "CNA_Result (*)(CNA_Handle,  const CNA_GameTime*, void*, CNA_CallbackError*)",
        ["CNA_GameCallbacks.exiting"] = "CNA_Result (*)(CNA_Handle,  const CNA_GameTime*, void*, CNA_CallbackError*)",
        ["CNA_GameCallbacks.load_content"] = "CNA_Result (*)(CNA_Handle,  const CNA_GameTime*, void*, CNA_CallbackError*)",
        ["CNA_GameCallbacks.unload_content"] = "CNA_Result (*)(CNA_Handle,  const CNA_GameTime*, void*, CNA_CallbackError*)",
        ["CNA_GameCallbacks.update"] = "CNA_Result (*)(CNA_Handle,  const CNA_GameTime*, void*, CNA_CallbackError*)",
        ["CNA_GameComponentCallbacks.draw"] = "void (*)(const CNA_GameTime*, void*)",
        ["CNA_GameComponentCallbacks.update"] = "void (*)(const CNA_GameTime*, void*)",
        ["CNA_GameCreateInfo.callbacks"] = "const CNA_GameCallbacks*",
        // A borrowed pointer to a type this binding deliberately does not declare: upstream records
        // that CNA_RenderPipelineSettingsEXT's C form is still a subset of its canonical type, so
        // the field is carried as a null pointer and never dereferenced. The spelling is the one the
        // compiler named when nint produced `void**`.
        ["CNA_PostProcessContext.settings"] = "const struct CNA_RenderPipelineSettingsEXT*",
        ["CNA_GameFrameHooks.begin_draw"] = "CNA_Result (*)(CNA_Handle,  const CNA_GameTime*, void*, CNA_Bool*, CNA_CallbackError*)",
        ["CNA_GameFrameHooks.begin_run"] = "CNA_Result (*)(CNA_Handle,  const CNA_GameTime*, void*, CNA_CallbackError*)",
        ["CNA_GameFrameHooks.end_draw"] = "CNA_Result (*)(CNA_Handle,  const CNA_GameTime*, void*, CNA_CallbackError*)",
        ["CNA_GameFrameHooks.end_run"] = "CNA_Result (*)(CNA_Handle,  const CNA_GameTime*, void*, CNA_CallbackError*)",
        ["CNA_GameFrameHooks.initialize"] = "CNA_Result (*)(CNA_Handle,  const CNA_GameTime*, void*, CNA_CallbackError*)",
        ["CNA_SpriteFontCreateInfo.glyphs"] = "const CNA_SpriteFontGlyph*",
        ["CNA_StringView.data"] = "const char*",
        ["CNA_UserIndices.index_data"] = "const void*",
        ["CNA_UserPrimitives.vertex_data"] = "const void*",
        ["CNA_VisualizationData.frequencies"] = "",   // a C array member: its address is a pointer-to-array, so offset only
        ["CNA_VisualizationData.samples"] = "",   // a C array member: its address is a pointer-to-array, so offset only

        // The same, in CNB's model structures. C declares these as float arrays and this binding
        // spells them as the vector and matrix they are -- CNA_CnbModelBone.transform is documented
        // as M11..M44, which is CnaMatrix's own field order. Offset only, for the pointer-to-array
        // reason above; the containing struct's total size still has to agree, and does.
        ["CNA_CnbMaterialInfo.base_color_factor"] = "",
        ["CNA_CnbMaterialInfo.emissive_factor"] = "",
        ["CNA_CnbMaterialInfo.specular_color_factor"] = "",
        ["CNA_CnbModelBone.transform"] = "",
    };

    /// <summary>Field names that name a C array of padding rather than a typed member.</summary>
    public static readonly HashSet<string> PaddingLikeFieldNames =
        new(StringComparer.Ordinal) { "reserved", "reserved0", "reserved1", "pressed_key_words" };

    public static bool IsPadding(string managed) =>
        managed.StartsWith('_') &&
        System.Text.RegularExpressions.Regex.IsMatch(managed, "^_[Rr]eserved[0-9]*$");

    /// <summary>
    /// Managed field names that do not follow the case rule, keyed by <c>Struct.Field</c>.
    ///
    /// Every entry is a place where the two sides genuinely chose different words -- CNA says
    /// <c>pressed_buttons</c> and the managed struct says <c>Buttons</c>, CNA says
    /// <c>scroll_wheel</c> and the managed struct says <c>ScrollWheelValue</c>. Derivation cannot
    /// bridge that and should not try: an override is a statement that someone checked the header,
    /// and a wrong one fails to compile rather than silently measuring the wrong field.
    /// </summary>
    private static readonly Dictionary<string, string> FieldOverrides = new(StringComparer.Ordinal)
    {
        ["CnaGamePadCapabilities.GamePadType"] = "gamepad_type",
        ["CnaGamePadCapabilities.HasDPadUpButton"] = "has_dpad_up_button",
        ["CnaGamePadCapabilities.HasDPadDownButton"] = "has_dpad_down_button",
        ["CnaGamePadCapabilities.HasDPadLeftButton"] = "has_dpad_left_button",
        ["CnaGamePadCapabilities.HasDPadRightButton"] = "has_dpad_right_button",
        ["CnaGamePadState.Buttons"] = "pressed_buttons",
        ["CnaGestureSample.FingerId"] = "finger_id_ext",
        ["CnaGestureSample.FingerId2"] = "finger_id2_ext",
        ["CnaMouseState.Buttons"] = "pressed_buttons",
        ["CnaMouseState.ScrollWheelValue"] = "scroll_wheel",
        ["CnaMouseState.HorizontalScrollWheelValue"] = "horizontal_scroll_wheel",
        ["CnaRenderTargetInfo.ContentLost"] = "is_content_lost",
        ["CnaVertexBufferInfo.ContentLost"] = "is_content_lost",

        // The keyboard's pressed-key set is four 64-bit words here and one array in C.
        ["CnaKeyboardState.Bits0"] = "pressed_key_words",

        // C spells these two "format" and "depth_format"; the managed structs are more explicit.
        ["CnaRenderTarget2DCreateInfo.ColorFormat"] = "format",
        ["CnaRenderTarget2DCreateInfo.DepthStencilFormat"] = "depth_format",
        ["CnaRenderTargetInfo.ColorFormat"] = "format",
        ["CnaRenderTargetInfo.DepthStencilFormat"] = "depth_format",

        // Padding that is one array in C and separate bytes here. Measured at the first byte, which
        // is the array's own offset; the rest follow by construction and the total size still has
        // to agree.
        ["CnaBackBufferReadback.Reserved0"] = "reserved",
        ["CnaGraphicsFormatSelection.Reserved0"] = "reserved",
        ["CnaSpriteFontCreateInfo.Reserved0"] = "reserved",
        ["CnaRasterizerState.ReservedTail"] = "reserved",
        ["CnaRenderTargetInfo.ReservedTail"] = "reserved",

        // Two separate padding members in C, named for their position rather than numbered here.
        ["CnaRenderTarget2DCreateInfo.Reserved"] = "reserved0",
        ["CnaRenderTarget2DCreateInfo.Reserved2"] = "reserved1",
    };

    private static readonly HashSet<string> SkippedFields = new(StringComparer.Ordinal)
    {
        // Measured at Bits0, which is the array's own offset; the rest follow by construction.
        "CnaKeyboardState.Bits1",
        "CnaKeyboardState.Bits2",
        "CnaKeyboardState.Bits3",

        // The tail of a padding run measured at its first byte, for the same reason.
        "CnaBackBufferReadback.Reserved1",
        "CnaBackBufferReadback.Reserved2",
        "CnaGraphicsFormatSelection.Reserved1",
        "CnaGraphicsFormatSelection.Reserved2",
        "CnaSpriteFontCreateInfo.Reserved1",
        "CnaSpriteFontCreateInfo.Reserved2",
        "CnaSpriteFontCreateInfo.Reserved3",
        "CnaSpriteFontCreateInfo.Reserved4",
    };

    public static bool Skip(Type type, FieldInfo field) =>
        SkippedFields.Contains($"{type.Name}.{field.Name}");

    public static string FieldName(Type type, FieldInfo field) =>
        FieldOverrides.TryGetValue($"{type.Name}.{field.Name}", out string? mapped)
            ? mapped
            : NativeField(field.Name);
}

