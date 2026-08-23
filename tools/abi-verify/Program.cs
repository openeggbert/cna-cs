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

compiler = ResolveCompiler(compiler);
string sourceDirectory = AppContext.BaseDirectory;
string layoutSource = FindSource("native_layout_probe.c");
string prototypeSource = FindSource("native_prototype_probe.c");
string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"cna-abi-{Guid.NewGuid():N}");
Directory.CreateDirectory(temporaryDirectory);

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

static string FindSource(string name)
{
    string candidate = Path.Combine(AppContext.BaseDirectory, name);
    if (File.Exists(candidate))
    {
        return candidate;
    }

    candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", name));
    if (File.Exists(candidate))
    {
        return candidate;
    }

    throw new FileNotFoundException($"ABI probe source was not copied or found: {name}");
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
    string standardOutput = process.StandardOutput.ReadToEnd();
    string standardError = process.StandardError.ReadToEnd();
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
        ["sizeof.CNA_Handle"] = Unsafe.SizeOf<CnaHandle>(),
        ["alignof.CNA_Handle"] = AlignmentOf<CnaHandle>(),
        ["sizeof.CNA_GraphicsDeviceEvent"] = Unsafe.SizeOf<CnaGraphicsDeviceEvent>(),
        ["sizeof.CNA_GraphicsProfile"] = Unsafe.SizeOf<CnaGraphicsProfile>(),
    };

    AddStruct<CnaStringView>(values, "CNA_StringView", ("data", nameof(CnaStringView.Data)), ("byte_length", nameof(CnaStringView.ByteLength)));
    AddStruct<CnaSoundEffectCreateInfo>(values, "CNA_SoundEffectCreateInfo",
        ("struct_size", nameof(CnaSoundEffectCreateInfo.StructSize)), ("struct_version", nameof(CnaSoundEffectCreateInfo.StructVersion)),
        ("sample_rate", nameof(CnaSoundEffectCreateInfo.SampleRate)), ("channels", nameof(CnaSoundEffectCreateInfo.Channels)),
        ("reserved", nameof(CnaSoundEffectCreateInfo.Reserved)));
    AddStruct<CnaSoundEffectInstanceInfo>(values, "CNA_SoundEffectInstanceInfo",
        ("struct_size", nameof(CnaSoundEffectInstanceInfo.StructSize)), ("struct_version", nameof(CnaSoundEffectInstanceInfo.StructVersion)),
        ("state", nameof(CnaSoundEffectInstanceInfo.State)), ("is_looped", nameof(CnaSoundEffectInstanceInfo.IsLooped)),
        ("reserved0", nameof(CnaSoundEffectInstanceInfo.Reserved0)), ("volume", nameof(CnaSoundEffectInstanceInfo.Volume)),
        ("pitch", nameof(CnaSoundEffectInstanceInfo.Pitch)), ("pan", nameof(CnaSoundEffectInstanceInfo.Pan)),
        ("reserved1", nameof(CnaSoundEffectInstanceInfo.Reserved1)));
    AddStruct<CnaAudioListener>(values, "CNA_AudioListener",
        ("struct_size", nameof(CnaAudioListener.StructSize)), ("struct_version", nameof(CnaAudioListener.StructVersion)),
        ("forward", nameof(CnaAudioListener.Forward)), ("position", nameof(CnaAudioListener.Position)),
        ("up", nameof(CnaAudioListener.Up)), ("velocity", nameof(CnaAudioListener.Velocity)));
    AddStruct<CnaAudioEmitter>(values, "CNA_AudioEmitter",
        ("struct_size", nameof(CnaAudioEmitter.StructSize)), ("struct_version", nameof(CnaAudioEmitter.StructVersion)),
        ("doppler_scale", nameof(CnaAudioEmitter.DopplerScale)), ("forward", nameof(CnaAudioEmitter.Forward)),
        ("position", nameof(CnaAudioEmitter.Position)), ("up", nameof(CnaAudioEmitter.Up)),
        ("velocity", nameof(CnaAudioEmitter.Velocity)));
    AddStruct<CnaCueInfo>(values, "CNA_CueInfo",
        ("struct_size", nameof(CnaCueInfo.StructSize)), ("struct_version", nameof(CnaCueInfo.StructVersion)),
        ("is_created", nameof(CnaCueInfo.IsCreated)), ("is_disposed", nameof(CnaCueInfo.IsDisposed)),
        ("is_paused", nameof(CnaCueInfo.IsPaused)), ("is_playing", nameof(CnaCueInfo.IsPlaying)),
        ("is_prepared", nameof(CnaCueInfo.IsPrepared)), ("is_preparing", nameof(CnaCueInfo.IsPreparing)),
        ("is_stopped", nameof(CnaCueInfo.IsStopped)), ("is_stopping", nameof(CnaCueInfo.IsStopping)));
    AddStruct<CnaVisualizationData>(values, "CNA_VisualizationData",
        ("struct_size", nameof(CnaVisualizationData.StructSize)), ("struct_version", nameof(CnaVisualizationData.StructVersion)),
        ("frequencies", nameof(CnaVisualizationData.Frequencies)), ("samples", nameof(CnaVisualizationData.Samples)));
    AddStruct<CnaManagedGameCallbacks>(values, "CNA_GameCallbacks",
        ("struct_size", nameof(CnaManagedGameCallbacks.StructSize)), ("struct_version", nameof(CnaManagedGameCallbacks.StructVersion)),
        ("load_content", nameof(CnaManagedGameCallbacks.LoadContent)), ("update", nameof(CnaManagedGameCallbacks.Update)),
        ("draw", nameof(CnaManagedGameCallbacks.Draw)), ("unload_content", nameof(CnaManagedGameCallbacks.UnloadContent)),
        ("exiting", nameof(CnaManagedGameCallbacks.Exiting)), ("context", nameof(CnaManagedGameCallbacks.Context)));
    AddStruct<CnaGameCreateInfo>(values, "CNA_GameCreateInfo",
        ("struct_size", nameof(CnaGameCreateInfo.StructSize)), ("struct_version", nameof(CnaGameCreateInfo.StructVersion)),
        ("is_fixed_time_step", nameof(CnaGameCreateInfo.IsFixedTimeStep)), ("reserved", nameof(CnaGameCreateInfo.Reserved)),
        ("target_elapsed_time_ticks", nameof(CnaGameCreateInfo.TargetElapsedTimeTicks)), ("window_title", nameof(CnaGameCreateInfo.WindowTitle)),
        ("callbacks", nameof(CnaGameCreateInfo.Callbacks)));
    return values;
}

static void AddStruct<T>(Dictionary<string, long> values, string nativeName, params (string Native, string Managed)[] fields)
    where T : unmanaged
{
    values[$"sizeof.{nativeName}"] = Unsafe.SizeOf<T>();
    values[$"alignof.{nativeName}"] = AlignmentOf<T>();
    foreach ((string native, string managed) in fields)
    {
        values[$"offsetof.{nativeName}.{native}"] = Marshal.OffsetOf<T>(managed).ToInt64();
    }
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
