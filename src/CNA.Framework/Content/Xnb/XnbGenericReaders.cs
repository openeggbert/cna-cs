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
    /// Splits a reader name's bracketed argument list into its arguments.
    ///
    /// <b>The separator is not a comma.</b> C# spells a two-argument generic as
    /// <c>[[A],[B]]</c>, and that is what this used to split on -- but the name reaching here has
    /// already been through <see cref="XnbContentReader.NormalizeTypeReaderName"/>, and the comma
    /// between two arguments is *also* the comma that begins the first argument's assembly
    /// qualification. Normalisation eats it along with the qualification, so a real file's
    /// <c>DictionaryReader`2</c> arrives spelled <c>[[System.String][System.Object]]</c>.
    ///
    /// Splitting that on top-level commas finds none, yields one argument, and the arity guard then
    /// rejects the reader -- which is why every real <c>Dictionary</c> asset failed with "does not
    /// (yet) support" while the hand-written comma spelling resolved. Arguments are therefore taken
    /// as the balanced bracket groups at depth zero, with the comma retained only as a fallback for
    /// a name that carried no qualification to strip.
    /// </summary>
    private static string[] SplitTypeArguments(string bracketed)
    {
        // Strip the outermost pair, then take each balanced group inside it.
        string inner = bracketed[1..^1];
        var arguments = new List<string>();
        int depth = 0;
        int groupStart = -1;

        for (int i = 0; i < inner.Length; i++)
        {
            switch (inner[i])
            {
                case '[':
                    if (depth == 0)
                    {
                        groupStart = i + 1;
                    }

                    depth++;
                    break;
                case ']':
                    depth--;
                    if (depth == 0 && groupStart >= 0)
                    {
                        arguments.Add(inner[groupStart..i].Trim());
                        groupStart = -1;
                    }

                    break;
            }
        }

        if (arguments.Count > 0)
        {
            return [.. arguments];
        }

        // No bracket groups at all: a single unbracketed argument, or an unqualified
        // comma-separated list. Both are spellings this project writes rather than reads, and both
        // are cheap to keep working.
        return [.. inner.Split(',').Select(argument => argument.Trim())];
    }

    /// <summary>
    /// <c>List&lt;T&gt;</c>: a 32-bit count followed by that many elements.
    ///
    /// The list is constructed as the real <c>List&lt;T&gt;</c> rather than as a
    /// <c>List&lt;object&gt;</c> of boxed elements, because the result is handed to game code --
    /// a <c>Model.Tag</c>, an <c>EffectMaterial</c> parameter -- which casts it to the type the
    /// content pipeline declared. A <c>List&lt;object&gt;</c> loads without complaint and fails
    /// that cast, which moves the failure from the loader (where it can be reported) into the
    /// game (where it looks like the game's own bug).
    /// </summary>
    private static XnbBuiltInReader? List(string elementType)
    {
        if (TryResolveTargetType(elementType) is not { } element)
        {
            return null;
        }

        Type listType = typeof(List<>).MakeGenericType(element.TargetType);

        return new XnbBuiltInReader(
            reader =>
            {
                int count = reader.ReadCollectionCount("list");
                var items = (System.Collections.IList)Activator.CreateInstance(listType, Math.Min(count, 1024))!;
                for (int i = 0; i < count; i++)
                {
                    items.Add(Coerce(reader.ReadElement(element), element.TargetType, "list element"));
                }

                return items;
            },
            listType);
    }

    /// <summary>An array, laid out exactly as <see cref="List"/> is, and typed for the same
    /// reason.</summary>
    private static XnbBuiltInReader? Array(string elementType)
    {
        if (TryResolveTargetType(elementType) is not { } element)
        {
            return null;
        }

        return new XnbBuiltInReader(
            reader =>
            {
                int count = reader.ReadCollectionCount("array");
                var items = System.Array.CreateInstance(element.TargetType, count);
                for (int i = 0; i < count; i++)
                {
                    items.SetValue(Coerce(reader.ReadElement(element), element.TargetType, "array element"), i);
                }

                return items;
            },
            element.TargetType.MakeArrayType());
    }

    /// <summary>A dictionary: a 32-bit count, then that many key/value pairs. Typed for the same
    /// reason as <see cref="List"/>; <c>Dictionary&lt;string, object&gt;</c> is the shape XNA's own
    /// <c>EffectMaterial</c> parameters and most model tags use.</summary>
    private static XnbBuiltInReader? Dictionary(string keyType, string valueType)
    {
        if (TryResolveTargetType(keyType) is not { } key || TryResolveTargetType(valueType) is not { } value)
        {
            return null;
        }

        Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(key.TargetType, value.TargetType);

        return new XnbBuiltInReader(
            reader =>
            {
                int count = reader.ReadCollectionCount("dictionary");
                var items = (System.Collections.IDictionary)Activator.CreateInstance(dictionaryType, Math.Min(count, 1024))!;
                for (int i = 0; i < count; i++)
                {
                    object entryKey = Coerce(reader.ReadElement(key), key.TargetType, "dictionary key")
                        ?? throw new ContentLoadException("Corrupt .xnb file: a dictionary key was null.");
                    items[entryKey] = Coerce(reader.ReadElement(value), value.TargetType, "dictionary value");
                }

                return items;
            },
            dictionaryType);
    }

    /// <summary>
    /// Checks that a polymorphically-read element really is what the collection declared before it
    /// is stored, so a mismatch is a <see cref="ContentLoadException"/> naming the collection rather
    /// than an <see cref="ArgumentException"/> from inside <c>List&lt;T&gt;.Add</c>.
    ///
    /// A value type's slot accepts null in neither XNA nor here; a reference type's does, and
    /// <c>null</c> is how the format spells an absent element.
    /// </summary>
    private static object? Coerce(object? value, Type targetType, string what)
    {
        if (value is null)
        {
            return targetType.IsValueType
                ? throw new ContentLoadException(
                    $"Corrupt .xnb file: a {what} was null where {targetType.Name} was declared.")
                : null;
        }

        return targetType.IsInstanceOfType(value)
            ? value
            : throw new ContentLoadException(
                $"Corrupt .xnb file: a {what} read as {value.GetType().Name} where {targetType.Name} was declared.");
    }

    private static XnbBuiltInReader? Nullable(string elementType)
    {
        if (TryResolveTargetType(elementType) is not { } element)
        {
            return null;
        }

        // Always the raw route, never the polymorphic one: XNA's NullableReader is constrained to
        // struct and calls ReadRawObject.
        return new XnbBuiltInReader(
            reader => reader.ReadBoolean() ? element.Read(reader) : null,
            typeof(Nullable<>).MakeGenericType(element.TargetType));
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

        return new XnbBuiltInReader(element.Read, element.TargetType);
    }

    /// <summary>
    /// Resolves an element's reader from the *content type* name a collection reader names, rather
    /// than from a reader name.
    ///
    /// <see cref="XnbBuiltInReaders.ByTargetType"/> answers for the closed built-ins. What it
    /// cannot answer for is a collection whose element is itself a collection --
    /// <c>Dictionary&lt;string, List&lt;Vector3&gt;&gt;</c> names its value type
    /// <c>System.Collections.Generic.List`1[[Microsoft.Xna.Framework.Vector3]]</c>, which is a type
    /// name and not a reader name. The mapping between the two is mechanical, so it is derived here
    /// rather than tabulated: XNA's reader for <c>List&lt;T&gt;</c> is <c>ListReader`1[[T]]</c>, and
    /// the same holds for arrays, dictionaries and nullables.
    /// </summary>
    internal static XnbBuiltInReader? TryResolveTargetType(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName);

        if (XnbBuiltInReaders.ByTargetType.TryGetValue(typeName, out XnbBuiltInReader builtIn))
        {
            return builtIn;
        }

        if (typeName.EndsWith("[]", StringComparison.Ordinal))
        {
            return TryResolve($"{Prefix}ArrayReader`1[[{typeName[..^2]}]]");
        }

        int bracket = typeName.IndexOf('[');
        if (bracket < 0 || !typeName.EndsWith("]", StringComparison.Ordinal))
        {
            return null;
        }

        string open = typeName[..bracket];
        string argumentList = typeName[bracket..];

        return open switch
        {
            "System.Collections.Generic.List`1" => TryResolve($"{Prefix}ListReader`1{argumentList}"),
            "System.Collections.Generic.Dictionary`2" => TryResolve($"{Prefix}DictionaryReader`2{argumentList}"),
            "System.Nullable`1" => TryResolve($"{Prefix}NullableReader`1{argumentList}"),
            _ => null,
        };
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
