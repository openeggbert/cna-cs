using CNA;
using CNA.Interop;

try
{
    CnaAbi.EnsureCompatible();
    (int major, int minor, int patch) = CnaAbi.Decode(CnaAbi.NativeVersion);
    Console.WriteLine($"CNA_ABI_PROBE_STATUS=accepted");
    Console.WriteLine($"CNA_ABI_PROBE_VERSION={major}.{minor}.{patch}");
    Console.WriteLine($"CNA_ABI_PROBE_LIBRARY={NativeLibraryResolver.LoadedLibraryPath}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"CNA_ABI_PROBE_STATUS=rejected");
    Console.Error.WriteLine(exception);
    return 1;
}
