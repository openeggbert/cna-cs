using System.Reflection;
using CNA.BehaviorProbes;

namespace XnaCompatibilityCompileProbe;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        LoadXnaRuntimeAssembliesIfRequested();

        ProbeOutput.Write(
            args,
            [
                .. MathBehaviorCorpus.Capture(),
                .. InputBehaviorCorpus.Capture(),
                .. AudioBehaviorCorpus.Capture(),
                .. ContentErrorCorpus.Capture(),
            ]);
    }

    private static void LoadXnaRuntimeAssembliesIfRequested()
    {
        string? directory = Environment.GetEnvironmentVariable("XNA_RUNTIME_PATH");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        string[] assemblyNames =
        {
            "Microsoft.Xna.Framework.dll",
            "Microsoft.Xna.Framework.Game.dll",
            "Microsoft.Xna.Framework.Graphics.dll",
            "Microsoft.Xna.Framework.Storage.dll",
            "Microsoft.Xna.Framework.Video.dll",
            "Microsoft.Xna.Framework.Input.Touch.dll",
            "Microsoft.Xna.Framework.Xact.dll",
        };

        foreach (string assemblyName in assemblyNames)
        {
            string assemblyPath = Path.Combine(directory, assemblyName);
            if (File.Exists(assemblyPath))
            {
                Assembly.LoadFrom(assemblyPath);
            }
        }
    }
}
