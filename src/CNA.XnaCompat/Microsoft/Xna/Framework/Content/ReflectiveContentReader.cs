namespace Microsoft.Xna.Framework.Content;

using System.Reflection;

/// <summary>
/// XNA's <c>ReflectiveReader</c>: the reader the content pipeline emits for a type that has no
/// reader of its own, which is every plain data class a game serializes from XML.
///
/// <b>Why it is worth porting exactly.</b> A game's level file, tuning table or entity template is
/// usually one of these, and the format is not "the fields, in some order" -- it is the fields and
/// properties the pipeline decided to serialize, in the order reflection returns them, with the
/// base type's members read first. Every rule below changes the byte layout, and getting one wrong
/// misreads the rest of the asset rather than failing at the mistake:
///
/// <list type="bullet">
/// <item>properties are read before fields, each declared-only, in reflection order;</item>
/// <item>a non-public member is included only if it carries <c>ContentSerializerAttribute</c>;</item>
/// <item>a read-only member is included only if its type can deserialize into an existing instance
/// -- so a read-only <c>List&lt;T&gt;</c> is read into, and a read-only <c>int</c> is skipped;</item>
/// <item>a member marked <c>SharedResource</c> reads a deferred reference rather than a value;</item>
/// <item>a skipped member that carried <c>ContentSerializerAttribute</c> is an error, not a
/// silent omission -- the author asked for it and the pipeline could not provide it.</item>
/// </list>
///
/// <b>What this does not do.</b> XNA's own reader is generic over the target type and this one is
/// not, because it is constructed from a name at run time; the difference is invisible from the
/// outside. <c>ContentSerializerRuntimeTypeAttribute</c> is not honoured: the pipeline uses it to
/// substitute a different runtime type, and substituting one here would need the writer's view of
/// the type graph.
/// </summary>
internal sealed class ReflectiveContentReader : ContentTypeReader
{
    private readonly ConstructorInfo? _instanceConstructor;
    private readonly int _typeVersion;
    private readonly List<ReflectiveMember> _members = [];
    private ContentTypeReader? _baseReader;

    internal ReflectiveContentReader(Type targetType)
        : base(targetType)
    {
        _instanceConstructor = targetType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        if (targetType.GetCustomAttribute<ContentSerializerTypeVersionAttribute>(inherit: false) is { } version)
        {
            _typeVersion = version.TypeVersion;
        }
    }

    public override bool CanDeserializeIntoExistingObject => TargetType.IsClass;

    public override int TypeVersion => _typeVersion;

    protected internal override void Initialize(ContentTypeReaderManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        Type? baseType = TargetType.BaseType;
        if (baseType is not null && baseType != typeof(object) && baseType != typeof(ValueType))
        {
            _baseReader = manager.GetTypeReader(baseType);
        }

        const BindingFlags flags =
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // Properties first, then fields. That is reflection's own grouping in XNA's reader, and the
        // pipeline writes members in the same order it reads them.
        foreach (PropertyInfo property in TargetType.GetProperties(flags))
        {
            if (ReflectiveMember.TryCreate(manager, TargetType, property) is { } member)
            {
                _members.Add(member);
            }
        }

        foreach (FieldInfo field in TargetType.GetFields(flags))
        {
            if (ReflectiveMember.TryCreate(manager, TargetType, field) is { } member)
            {
                _members.Add(member);
            }
        }
    }

    protected internal override object Read(ContentReader input, object? existingInstance)
    {
        ArgumentNullException.ThrowIfNull(input);

        object instance = existingInstance ?? Construct();

        if (_baseReader is not null)
        {
            object fromBase = _baseReader.Read(input, instance);
            if (!ReferenceEquals(fromBase, instance))
            {
                throw new InvalidOperationException(
                    $"Content type reader {_baseReader.GetType()} constructed a new instance while reading " +
                    $"the base of {TargetType}, so the derived members would be written to the wrong object.");
            }
        }

        foreach (ReflectiveMember member in _members)
        {
            member.Read(input, instance);
        }

        return instance;
    }

    private object Construct()
    {
        if (_instanceConstructor is not null)
        {
            return _instanceConstructor.Invoke(null);
        }

        return TargetType.IsValueType
            ? Activator.CreateInstance(TargetType)!
            : throw new InvalidOperationException(
                $"{TargetType} has no parameterless constructor, so content cannot construct one.");
    }
}

/// <summary>One serialized member of a reflectively read type.</summary>
internal sealed class ReflectiveMember
{
    private readonly ContentTypeReader? _typeReader;
    private readonly FieldInfo? _field;
    private readonly PropertyInfo? _property;
    private readonly bool _canWrite;
    private readonly bool _sharedResource;

    private ReflectiveMember(
        ContentTypeReaderManager manager,
        FieldInfo? field,
        PropertyInfo? property,
        Type memberType,
        bool canWrite)
    {
        _typeReader = manager.GetTypeReader(memberType) ?? BuiltinReaders.TryCreateForTargetType(memberType);
        _field = field;
        _property = property;
        _canWrite = canWrite;
        _sharedResource = IsSharedResource((MemberInfo?)field ?? property!);
    }

    internal static ReflectiveMember? TryCreate(
        ContentTypeReaderManager manager, Type declaringType, FieldInfo field)
    {
        bool canWrite = !field.IsInitOnly && !field.IsLiteral;
        if (!ShouldSerialize(manager, declaringType, field, field.FieldType, field.IsPublic, canRead: true, canWrite))
        {
            ValidateSkipped(field);
            return null;
        }

        return new ReflectiveMember(manager, field, property: null, field.FieldType, canWrite);
    }

    internal static ReflectiveMember? TryCreate(
        ContentTypeReaderManager manager, Type declaringType, PropertyInfo property)
    {
        if (property.GetIndexParameters().Length > 0)
        {
            return null;
        }

        bool isPublic = true;
        foreach (MethodInfo accessor in property.GetAccessors(nonPublic: true))
        {
            // An override is serialized by the type that declares it, not by this one.
            if (accessor.GetBaseDefinition() != accessor)
            {
                return null;
            }

            if (!accessor.IsPublic)
            {
                isPublic = false;
            }
        }

        if (!ShouldSerialize(
                manager, declaringType, property, property.PropertyType, isPublic, property.CanRead, property.CanWrite))
        {
            ValidateSkipped(property);
            return null;
        }

        return new ReflectiveMember(manager, field: null, property, property.PropertyType, property.CanWrite);
    }

    private static bool ShouldSerialize(
        ContentTypeReaderManager manager,
        Type declaringType,
        MemberInfo member,
        Type memberType,
        bool isPublic,
        bool canRead,
        bool canWrite)
    {
        if (!canRead || member.IsDefined(typeof(ContentSerializerIgnoreAttribute), inherit: false))
        {
            return false;
        }

        if (!isPublic && member.GetCustomAttribute<ContentSerializerAttribute>() is null)
        {
            return false;
        }

        if (!canWrite)
        {
            // A read-only member is only serializable if its value can be read *into*: a read-only
            // List<T> is filled, a read-only int cannot be.
            ContentTypeReader? reader =
                manager.GetTypeReader(memberType) ?? BuiltinReaders.TryCreateForTargetType(memberType);
            if (reader is null || !reader.CanDeserializeIntoExistingObject)
            {
                return false;
            }
        }

        return !declaringType.IsValueType || !IsSharedResource(member);
    }

    private static bool IsSharedResource(MemberInfo member) =>
        member.GetCustomAttribute<ContentSerializerAttribute>()?.SharedResource ?? false;

    /// <summary>A member the author explicitly asked to serialize, which the rules above rejected,
    /// is a mistake worth naming rather than a member quietly missing from the asset.</summary>
    private static void ValidateSkipped(MemberInfo member)
    {
        if (member.GetCustomAttribute<ContentSerializerAttribute>() is not null)
        {
            throw new InvalidOperationException(
                $"{member.DeclaringType}.{member.Name} is marked with ContentSerializerAttribute but cannot " +
                "be serialized.");
        }
    }

    internal void Read(ContentReader input, object parentInstance)
    {
        if (_typeReader is null)
        {
            throw new ContentLoadException(
                $"Content asset '{input.AssetName}' needs a content type reader for " +
                $"{_field?.FieldType ?? _property!.PropertyType} and its type-reader table does not declare one.");
        }

        if (_sharedResource)
        {
            if (!_canWrite)
            {
                throw new InvalidOperationException(
                    "A read-only member cannot hold a shared resource, because the reference is resolved after " +
                    "the object is constructed.");
            }

            input.ReadSharedResource<object>(value => Assign(parentInstance, value));
            return;
        }

        if (_canWrite)
        {
            Assign(parentInstance, input.ReadObject<object>(_typeReader, null!));
            return;
        }

        object? existing = _property is not null
            ? _property.GetValue(parentInstance, null)
            : _field!.GetValue(parentInstance);

        if (existing is null)
        {
            MemberInfo member = (MemberInfo?)_property ?? _field!;
            throw new InvalidOperationException(
                $"{member.DeclaringType}.{member.Name} is read-only and null, so content has nothing to read into.");
        }

        input.ReadObject(_typeReader, existing);
    }

    private void Assign(object parentInstance, object? value)
    {
        if (_property is not null)
        {
            _property.SetValue(parentInstance, value, null);
        }
        else
        {
            _field!.SetValue(parentInstance, value);
        }
    }
}
