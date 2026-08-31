using System.Text;

namespace CNA.BehaviorProbes;

/// <summary>
/// Where a behavior probe puts its observations.
///
/// <b>Why this is not just <c>Console.WriteLine</c>.</b> A probe shares its standard output with the
/// native library loaded underneath it, and some CNA renderers write to it: the Vulkan backend
/// prints a one-line capability banner at device creation, and SDL_RENDERER prints its logical
/// presentation mode. Capturing a probe by redirecting stdout therefore produces a snapshot whose
/// contents depend on which renderer built the library -- measured, not hypothetical: the same
/// probe emitted 166 lines under OPENGLES3 and 167 under Vulkan, and the corpus validator rejected
/// the second as an observation-count mismatch.
///
/// So a capture names a file and the observations go there, where nothing else can write. Standard
/// output stays the interactive default, because reading a probe's answers in a terminal is what it
/// is for.
///
/// The file is written the way every other corpus artifact is written -- UTF-8 with no BOM, LF line
/// endings, one trailing newline -- so that a captured snapshot is byte-identical to a redirected
/// one on any platform.
/// </summary>
internal static class ProbeOutput
{
    /// <summary>
    /// Writes <paramref name="observations"/> to the path named by <c>--output</c> in
    /// <paramref name="args"/>, or to standard output when there is none.
    /// </summary>
    public static void Write(string[] args, IEnumerable<string> observations)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(observations);

        string? path = OutputPath(args);
        if (path is null)
        {
            foreach (string observation in observations)
            {
                Console.WriteLine(observation);
            }

            return;
        }

        var text = new StringBuilder();
        foreach (string observation in observations)
        {
            text.Append(observation).Append('\n');
        }

        File.WriteAllText(path, text.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string? OutputPath(string[] args)
    {
        for (int index = 0; index < args.Length; index++)
        {
            if (args[index] != "--output")
            {
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException("--output needs a path.", nameof(args));
            }

            return args[index + 1];
        }

        return null;
    }
}
