namespace CNA.Content.Xnb;


/// <summary>
/// The generic built-in readers -- <c>ListReader</c>, <c>ArrayReader</c>, <c>DictionaryReader</c>,
/// <c>NullableReader</c>, <c>EnumReader</c> -- resolved from the element type names the
/// <c>.xnb</c> reader table spells.
///
/// <b>Why a resolver instead of more table entries.</b> The reader table used to hold four fully
/// spelled generic names, one per instantiation a <c>SpriteFont</c> happens to need
/// (<c>ListReader`1[[Microsoft.Xna.Framework.Rectangle]]</c> and three more). That works for a
/// closed set of assets and cannot work for a game's own data: a level file holding a
/// <c>List&lt;Vector2&gt;</c> or a <c>Dictionary&lt;string, int&gt;</c> spells an instantiation
/// nobody registered, and the load failed naming a reader that "this project's .xnb reader does not
/// (yet) support". Content-pipeline data is how a large share of XNA games store their levels, so
/// that message was one of the larger practical barriers to running one.
///
/// <b>The value-type rule is the whole difficulty.</b> XNA's collection readers call
/// <c>ReadObject&lt;T&gt;(elementReader)</c>, which reads a value type *inline* using the reader it
/// already holds and a reference type through the polymorphic route, with its own type-index
/// prefix. So a <c>List&lt;int&gt;</c> and a <c>List&lt;string&gt;</c> have different byte layouts
/// per element, and choosing wrong desynchronises everything after the first element rather than
/// failing where the mistake is. Each entry in <see cref="XnbBuiltInReaders"/> therefore carries
/// whether its target is a value type, taken from the decompiled reader's own declaration.
///
/// <b>What is deliberately not resolved.</b> An element type this table does not name fails, by
/// name, at the point the collection is read. It would be easy to fall back to the polymorphic
/// route and hope the element is a reference type; when it is not, the result is a plausible object
/// graph read from misaligned bytes.
/// </summary>
internal static class XnbGenericReaders
{
    private const string Prefix = "Microsoft.Xna.Framework.Content.";

    /// <summary>
    /// Resolves a normalised generic reader name, or returns <see langword="null"/> when the name
    /// is not one of the generic built-ins.
    /// </summary>
    internal static XnbBuiltInReader? TryResolve(string readerName)
    {
        ArgumentNullException.ThrowIfNull(readerName);

        if (!readerName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return null;
        }

        int bracket = readerName.IndexOf('[');
        if (bracket < 0 || !readerName.EndsWith("]", StringComparison.Ordinal))
        {
            return null;
        }

        string open = readerName[Prefix.Length..bracket];
        string[] arguments = SplitTypeArguments(readerName[bracket..]);

        return open switch
        {
            "ListReader`1" when arguments.Length == 1 => List(arguments[0]),
            "ArrayReader`1" when arguments.Length == 1 => Array(arguments[0]),
            "DictionaryReader`2" when arguments.Length == 2 => Dictionary(arguments[0], arguments[1]),
            "NullableReader`1" when arguments.Length == 1 => Nullable(arguments[0]),
            "EnumReader`1" when arguments.Length == 1 => Enum(arguments[0]),
            _ => null,
        };
    }

    /// <summary>
    /// Splits <c>[[A],[B]]</c> into its arguments, tracking bracket depth so a nested generic
    /// argument survives intact.
    /// </summary>
    private static string[] SplitTypeArguments(string bracketed)
    {
        // Strip the outermost pair, then split the remainder on top-level commas.
        string inner = bracketed[1..^1];
        var arguments = new List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < inner.Length; i++)
        {
            switch (inner[i])
            {
                case '[':
                    depth++;
                    break;
                case ']':
                    depth--;
                    break;
                case ',' when depth == 0:
                    arguments.Add(Trim(inner[start..i]));
                    start = i + 1;
                    break;
            }
        }

        arguments.Add(Trim(inner[start..]));
        return [.. arguments];

        static string Trim(string argument)
        {
            argument = argument.Trim();
            return argument.StartsWith('[') && argument.EndsWith(']')
                ? argument[1..^1].Trim()
                : argument;
        }
    }

    private static XnbBuiltInReader? List(string elementType)
    {
        if (!XnbBuiltInReaders.ByTargetType.TryGetValue(elementType, out XnbBuiltInReader element))
        {
            return null;
        }

        return new XnbBuiltInReader(
            reader =>
            {
                int count = reader.ReadCollectionCount("list");
                var items = new List<object?>(Math.Min(count, 1024));
                for (int i = 0; i < count; i++)
                {
                    items.Add(reader.ReadElement(element));
                }

                return items;
            },
            TargetIsValueType: false);
    }

    private static XnbBuiltInReader? Array(string elementType)
    {
        if (!XnbBuiltInReaders.ByTargetType.TryGetValue(elementType, out XnbBuiltInReader element))
        {
            return null;
        }

        return new XnbBuiltInReader(
            reader =>
            {
                int count = reader.ReadCollectionCount("array");
                var items = new object?[count];
                for (int i = 0; i < count; i++)
                {
                    items[i] = reader.ReadElement(element);
                }

                return items;
            },
            TargetIsValueType: false);
    }

    private static XnbBuiltInReader? Dictionary(string keyType, string valueType)
    {
        if (!XnbBuiltInReaders.ByTargetType.TryGetValue(keyType, out XnbBuiltInReader key) ||
            !XnbBuiltInReaders.ByTargetType.TryGetValue(valueType, out XnbBuiltInReader value))
        {
            return null;
        }

        return new XnbBuiltInReader(
            reader =>
            {
                int count = reader.ReadCollectionCount("dictionary");
                var items = new Dictionary<object, object?>(Math.Min(count, 1024));
                for (int i = 0; i < count; i++)
                {
                    object entryKey = reader.ReadElement(key)
                        ?? throw new ContentLoadException("Corrupt .xnb file: a dictionary key was null.");
                    items[entryKey] = reader.ReadElement(value);
                }

                return items;
            },
            TargetIsValueType: false);
    }

    private static XnbBuiltInReader? Nullable(string elementType)
    {
        if (!XnbBuiltInReaders.ByTargetType.TryGetValue(elementType, out XnbBuiltInReader element))
        {
            return null;
        }

        // Always the raw route, never the polymorphic one: XNA's NullableReader is constrained to
        // struct and calls ReadRawObject.
        return new XnbBuiltInReader(
            reader => reader.ReadBoolean() ? element.Read(reader) : null,
            TargetIsValueType: true);
    }

    /// <summary>
    /// An enum reads as its underlying integral type, and the file does not say which one --
    /// XNA takes it from the CLR type. Resolving that here would mean loading the game's own
    /// assembly from a name this reader has already stripped of its assembly qualification.
    ///
    /// So this returns the reader only for enums whose underlying type is the default. That is not
    /// a guess dressed up as a rule: <see cref="XnbContentReader"/> hands the caller's expected type
    /// down when it has one, and <see cref="XnbEnumTypes"/> is where a caller states an exception.
    /// An enum with a non-default underlying type and no stated exception fails by name instead of
    /// reading four bytes where the file wrote one.
    /// </summary>
    private static XnbBuiltInReader? Enum(string enumType)
    {
        string underlying = XnbEnumTypes.UnderlyingTypeName(enumType);
        if (!XnbBuiltInReaders.ByTargetType.TryGetValue(underlying, out XnbBuiltInReader element))
        {
            return null;
        }

        return new XnbBuiltInReader(element.Read, TargetIsValueType: true);
    }
}

/// <summary>
/// Which integral type an enum in a <c>.xnb</c> file is written as.
///
/// The file records the enum's own name and nothing about its storage, so a reader that never sees
/// the CLR type has to be told. Registering an exception is how a game with a <c>byte</c>- or
/// <c>long</c>-backed enum makes its content loadable; everything else is <c>Int32</c>, which is
/// C#'s default and what the content pipeline writes for it.
/// </summary>
public static class XnbEnumTypes
{
    private static readonly Dictionary<string, string> Registered = new(StringComparer.Ordinal);
    private static readonly object Gate = new();

    /// <summary>
    /// States that <paramref name="enumTypeName"/> -- the full name, without assembly
    /// qualification -- is stored as <paramref name="underlyingType"/>, one of the
    /// <c>System.SByte</c>/<c>Byte</c>/<c>Int16</c>/<c>UInt16</c>/<c>Int32</c>/<c>UInt32</c>/
    /// <c>Int64</c>/<c>UInt64</c> names.
    /// </summary>
    public static void Register(string enumTypeName, string underlyingType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enumTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(underlyingType);

        lock (Gate)
        {
            Registered[enumTypeName] = underlyingType;
        }
    }

    /// <summary>Convenience for a caller that has the CLR type in hand.</summary>
    public static void Register(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);
        if (!enumType.IsEnum)
        {
            throw new ArgumentException($"{enumType} is not an enum.", nameof(enumType));
        }

        Register(enumType.FullName!, System.Enum.GetUnderlyingType(enumType).FullName!);
    }

    internal static string UnderlyingTypeName(string enumTypeName)
    {
        lock (Gate)
        {
            return Registered.TryGetValue(enumTypeName, out string? underlying)
                ? underlying
                : "System.Int32";
        }
    }
}
