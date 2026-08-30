namespace Microsoft.Xna.Framework.Content;

using System.Reflection;

/// <summary>
/// XNA's generic built-in content readers -- <c>ListReader</c>, <c>ArrayReader</c>,
/// <c>DictionaryReader</c>, <c>NullableReader</c>, <c>EnumReader</c> -- constructed from the
/// serialized reader name an <c>.xnb</c> file spells.
///
/// <b>Why this matters more than it looks.</b> A large share of XNA games keep their levels,
/// tables and tuning data in content-pipeline assets, and almost any such asset is a
/// <c>List&lt;T&gt;</c>, an array or a dictionary at some level. Until now the reader table
/// resolved those names through <see cref="Type.GetType(string, bool)"/>, which cannot find a type
/// in <c>Microsoft.Xna.Framework</c> because no such assembly is loaded, so the load failed with
/// "could not find ContentTypeReader Type". Scalars and math types were registered by hand; every
/// collection of them was not.
///
/// <b>The type arguments keep their assembly qualification.</b> The outer reader name is matched
/// after stripping it, because the reader lives in an assembly that does not exist here. The
/// arguments must not be, because one of them is routinely the game's own type and its assembly
/// *is* loaded -- that is what makes <c>List&lt;MyGame.Tile&gt;</c> resolvable at all.
/// </summary>
internal static class BuiltinGenericReaders
{
    private const string Prefix = "Microsoft.Xna.Framework.Content.";

    /// <summary>
    /// Builds the reader for a serialized generic reader name, or returns <see langword="null"/>
    /// when the name is not one of the generic built-ins.
    /// </summary>
    internal static ContentTypeReader? TryCreate(string serializedName)
    {
        ArgumentNullException.ThrowIfNull(serializedName);

        int bracket = serializedName.IndexOf('[');
        if (bracket < 0)
        {
            return null;
        }

        string open = serializedName[..bracket];
        if (!open.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return null;
        }

        int closing = MatchingBracket(serializedName, bracket);
        if (closing < 0)
        {
            return null;
        }

        string[] arguments = SplitTypeArguments(serializedName[(bracket + 1)..closing]);

        return open[Prefix.Length..] switch
        {
            "ListReader`1" when arguments.Length == 1 => Close(typeof(ListReader<>), arguments),
            "ArrayReader`1" when arguments.Length == 1 => Close(typeof(ArrayReader<>), arguments),
            "DictionaryReader`2" when arguments.Length == 2 => Close(typeof(DictionaryReader<,>), arguments),
            "NullableReader`1" when arguments.Length == 1 => Close(typeof(NullableReader<>), arguments),
            "EnumReader`1" when arguments.Length == 1 => Close(typeof(EnumReader<>), arguments),
            "ReflectiveReader`1" when arguments.Length == 1 => Reflective(arguments[0]),
            _ => null,
        };
    }

    /// <summary>
    /// <c>ReflectiveReader&lt;T&gt;</c> names the game's own type, and the reader is not generic
    /// here -- it takes the resolved <see cref="Type"/> instead, which is the same reader with one
    /// fewer layer of reflection.
    /// </summary>
    private static ContentTypeReader? Reflective(string targetTypeName)
    {
        Type? target = ContentTypeResolver.Resolve(targetTypeName);
        return target is null ? null : new ReflectiveContentReader(target);
    }

    private static ContentTypeReader? Close(Type openReader, string[] arguments)
    {
        var resolved = new Type[arguments.Length];
        for (int index = 0; index < arguments.Length; index++)
        {
            Type? argument = ContentTypeResolver.Resolve(arguments[index]);
            if (argument is null)
            {
                return null;
            }

            resolved[index] = argument;
        }

        try
        {
            return (ContentTypeReader?)Activator.CreateInstance(
                openReader.MakeGenericType(resolved), nonPublic: true);
        }
        catch (ArgumentException)
        {
            // A type argument that does not satisfy the reader's constraints -- a NullableReader
            // over a reference type, say. Answering null lets the caller report the reader name it
            // could not build, which is more use than an exception from reflection.
            return null;
        }
    }

    private static int MatchingBracket(string text, int open)
    {
        int depth = 0;
        for (int index = open; index < text.Length; index++)
        {
            switch (text[index])
            {
                case '[':
                    depth++;
                    break;
                case ']':
                    depth--;
                    if (depth == 0)
                    {
                        return index;
                    }

                    break;
            }
        }

        return -1;
    }

    /// <summary>Splits <c>[A,...],[B,...]</c> into its bracketed, still assembly-qualified
    /// arguments.</summary>
    private static string[] SplitTypeArguments(string inner)
    {
        var arguments = new List<string>();
        int depth = 0;
        int start = 0;

        for (int index = 0; index < inner.Length; index++)
        {
            switch (inner[index])
            {
                case '[':
                    depth++;
                    break;
                case ']':
                    depth--;
                    break;
                case ',' when depth == 0:
                    arguments.Add(Unwrap(inner[start..index]));
                    start = index + 1;
                    break;
            }
        }

        arguments.Add(Unwrap(inner[start..]));
        return [.. arguments];

        static string Unwrap(string argument)
        {
            argument = argument.Trim();
            return argument.StartsWith('[') && argument.EndsWith(']') ? argument[1..^1].Trim() : argument;
        }
    }
}

/// <summary>
/// Resolves a content type name from an <c>.xnb</c> file to a loaded CLR type.
///
/// Four steps, because one is not enough. The name in the file is assembly-qualified against the
/// assemblies XNA shipped -- <c>System.Int32, mscorlib, Version=2.0.0.0</c>,
/// <c>Microsoft.Xna.Framework.Vector2, Microsoft.Xna.Framework, Version=4.0.0.0</c> -- and neither
/// assembly exists here. So the qualified name is tried first (it is the one that works for a
/// game's own types), then the bare name against this assembly and the running application's.
/// </summary>
internal static class ContentTypeResolver
{
    private static readonly Dictionary<string, Type> WellKnown = new(StringComparer.Ordinal)
    {
        ["System.Boolean"] = typeof(bool),
        ["System.Byte"] = typeof(byte),
        ["System.SByte"] = typeof(sbyte),
        ["System.Char"] = typeof(char),
        ["System.Int16"] = typeof(short),
        ["System.UInt16"] = typeof(ushort),
        ["System.Int32"] = typeof(int),
        ["System.UInt32"] = typeof(uint),
        ["System.Int64"] = typeof(long),
        ["System.UInt64"] = typeof(ulong),
        ["System.Single"] = typeof(float),
        ["System.Double"] = typeof(double),
        ["System.Decimal"] = typeof(decimal),
        ["System.String"] = typeof(string),
        ["System.DateTime"] = typeof(DateTime),
        ["System.TimeSpan"] = typeof(TimeSpan),
        ["System.Object"] = typeof(object),
    };

    internal static Type? Resolve(string assemblyQualifiedName)
    {
        ArgumentNullException.ThrowIfNull(assemblyQualifiedName);

        Type? direct = Type.GetType(assemblyQualifiedName, throwOnError: false);
        if (direct is not null)
        {
            return direct;
        }

        string full = StripAssemblyQualification(assemblyQualifiedName);
        if (WellKnown.TryGetValue(full, out Type? wellKnown))
        {
            return wellKnown;
        }

        // The XNA types a game names live in this assembly, under the same full names.
        Type? compat = typeof(ContentTypeResolver).Assembly.GetType(full, throwOnError: false);
        if (compat is not null)
        {
            return compat;
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type? found = assembly.GetType(full, throwOnError: false);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static string StripAssemblyQualification(string name)
    {
        int depth = 0;
        for (int index = 0; index < name.Length; index++)
        {
            switch (name[index])
            {
                case '[':
                    depth++;
                    break;
                case ']':
                    depth--;
                    break;
                case ',' when depth == 0:
                    return name[..index].Trim();
            }
        }

        return name.Trim();
    }
}
