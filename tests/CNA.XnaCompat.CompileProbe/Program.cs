using System.Reflection;

namespace XnaCompatibilityCompileProbe;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        LoadXnaRuntimeAssembliesIfRequested();

        foreach (string observation in MathBehaviorCorpus.Capture())
        {
            Console.WriteLine(observation);
        }

        foreach (string observation in InputBehaviorCorpus.Capture())
        {
            Console.WriteLine(observation);
        }

        foreach (string observation in AudioBehaviorCorpus.Capture())
        {
            Console.WriteLine(observation);
        }

        foreach (string observation in ContentErrorCorpus.Capture())
        {
            Console.WriteLine(observation);
        }
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
