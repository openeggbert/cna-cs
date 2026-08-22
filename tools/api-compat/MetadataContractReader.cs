using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace CNA.ApiCompat;

internal sealed class MetadataContractReader
{
    private static readonly HashSet<string> RelevantAttributes = new(StringComparer.Ordinal)
    {
        "System.CLSCompliantAttribute",
        "System.ComponentModel.DefaultValueAttribute",
        "System.ComponentModel.EditorBrowsableAttribute",
        "System.FlagsAttribute",
        "System.ObsoleteAttribute",
        "System.ParamArrayAttribute",
        "System.Runtime.CompilerServices.ExtensionAttribute",
    };

    private readonly IReadOnlyList<string> _namespacePrefixes;

    public MetadataContractReader(IReadOnlyList<string> namespacePrefixes)
    {
        _namespacePrefixes = namespacePrefixes;
    }

    public ApiContract Read(IEnumerable<string> assemblyPaths)
    {
        var contract = new ApiContract();

        foreach (string assemblyPath in assemblyPaths)
        {
            using FileStream stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);
            if (!peReader.HasMetadata)
            {
                throw new InvalidDataException($"'{assemblyPath}' is not a managed assembly.");
            }

            MetadataReader reader = peReader.GetMetadataReader();
            string assemblyName = reader.IsAssembly
                ? reader.GetString(reader.GetAssemblyDefinition().Name)
                : Path.GetFileNameWithoutExtension(assemblyPath);
            var names = new MetadataNames(reader);
            var provider = new ContractSignatureProvider(names);

            foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
            {
                TypeDefinition definition = reader.GetTypeDefinition(handle);
                string name = names.GetTypeDefinitionName(handle);
                if (!IsContractType(name) || !IsEffectivelyVisibleType(reader, definition))
                {
                    continue;
                }

                TypeContract type = ReadType(reader, provider, names, handle, definition, name, assemblyName);
                if (!contract.Types.TryAdd(name, type))
                {
                    throw new InvalidDataException(
                        $"Public type '{name}' is defined by more than one selected assembly " +
                        $"('{contract.Types[name].AssemblyName}' and '{assemblyName}').");
                }
            }
        }

        // Public XNA classes implement several internal bookkeeping interfaces (for example
        // IGraphicsResource). Those names occur in metadata but cannot be named by game source and
        // are not part of the public contract. Keep external/BCL interfaces and selected visible
        // contract interfaces; discard inaccessible interfaces from the selected namespace.
        foreach ((string name, TypeContract type) in contract.Types.ToArray())
        {
            string[] visibleInterfaces = type.Interfaces
                .Where(@interface => !IsContractType(@interface) || contract.Types.ContainsKey(@interface))
                .ToArray();
            contract.Types[name] = type with { Interfaces = visibleInterfaces };
        }

        return contract;
    }

    private TypeContract ReadType(
        MetadataReader reader,
        ContractSignatureProvider provider,
        MetadataNames names,
        TypeDefinitionHandle handle,
        TypeDefinition definition,
        string name,
        string assemblyName)
    {
        var context = new GenericContext(definition.GetGenericParameters().Count, 0);
        string? baseType = definition.BaseType.IsNil
            ? null
            : names.DecodeType(definition.BaseType, provider, context);

        string kind = definition.Attributes.HasFlag(TypeAttributes.Interface)
            ? "interface"
            : baseType switch
            {
                "System.Enum" => "enum",
                "System.ValueType" => "struct",
                "System.MulticastDelegate" => "delegate",
                _ => "class",
            };

        string[] interfaces = definition.GetInterfaceImplementations()
            .Select(interfaceHandle => reader.GetInterfaceImplementation(interfaceHandle))
            .Select(implementation => names.DecodeType(implementation.Interface, provider, context))
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] genericParameters = ReadGenericParameters(
            reader,
            provider,
            names,
            definition.GetGenericParameters(),
            context);

        IReadOnlyList<MemberContract> members = ReadMembers(reader, provider, names, definition, context);

        return new TypeContract(
            name,
            GetTypeAccessibility(definition.Attributes),
            kind,
            definition.Attributes.HasFlag(TypeAttributes.Abstract),
            definition.Attributes.HasFlag(TypeAttributes.Sealed),
            baseType,
            interfaces,
            genericParameters,
            GetLayout(definition),
            ReadRelevantAttributes(reader, names, definition.GetCustomAttributes()),
            members,
            assemblyName);
    }

    private static IReadOnlyList<MemberContract> ReadMembers(
        MetadataReader reader,
        ContractSignatureProvider provider,
        MetadataNames names,
        TypeDefinition definition,
        GenericContext typeContext)
    {
        var accessorHandles = new HashSet<MethodDefinitionHandle>();
        foreach (PropertyDefinitionHandle handle in definition.GetProperties())
        {
            PropertyAccessors accessors = reader.GetPropertyDefinition(handle).GetAccessors();
            AddIfPresent(accessorHandles, accessors.Getter);
            AddIfPresent(accessorHandles, accessors.Setter);
            foreach (MethodDefinitionHandle other in accessors.Others)
            {
                AddIfPresent(accessorHandles, other);
            }
        }

        foreach (EventDefinitionHandle handle in definition.GetEvents())
        {
            EventAccessors accessors = reader.GetEventDefinition(handle).GetAccessors();
            AddIfPresent(accessorHandles, accessors.Adder);
            AddIfPresent(accessorHandles, accessors.Remover);
            AddIfPresent(accessorHandles, accessors.Raiser);
            foreach (MethodDefinitionHandle other in accessors.Others)
            {
                AddIfPresent(accessorHandles, other);
            }
        }

        var result = new List<MemberContract>();

        foreach (MethodDefinitionHandle handle in definition.GetMethods())
        {
            MethodDefinition method = reader.GetMethodDefinition(handle);
            if (accessorHandles.Contains(handle) || !IsVisibleMethod(method.Attributes) ||
                reader.GetString(method.Name) == ".cctor")
            {
                continue;
            }

            result.Add(ReadMethod(reader, provider, names, method, typeContext));
        }

        foreach (PropertyDefinitionHandle handle in definition.GetProperties())
        {
            PropertyDefinition property = reader.GetPropertyDefinition(handle);
            PropertyAccessors accessors = property.GetAccessors();
            if (!IsVisibleAccessor(reader, accessors.Getter) && !IsVisibleAccessor(reader, accessors.Setter))
            {
                continue;
            }

            result.Add(ReadProperty(reader, provider, names, property, accessors, typeContext));
        }

        foreach (EventDefinitionHandle handle in definition.GetEvents())
        {
            EventDefinition @event = reader.GetEventDefinition(handle);
            EventAccessors accessors = @event.GetAccessors();
            if (!IsVisibleAccessor(reader, accessors.Adder) && !IsVisibleAccessor(reader, accessors.Remover))
            {
                continue;
            }

            result.Add(ReadEvent(reader, provider, names, @event, accessors, typeContext));
        }

        foreach (FieldDefinitionHandle handle in definition.GetFields())
        {
            FieldDefinition field = reader.GetFieldDefinition(handle);
            if (!IsVisibleField(field.Attributes))
            {
                continue;
            }

            result.Add(ReadField(reader, provider, names, field, typeContext));
        }

        return result
            .OrderBy(member => member.FamilyKey, StringComparer.Ordinal)
            .ThenBy(member => member.SignatureKey, StringComparer.Ordinal)
            .ToArray();
    }

    private static MemberContract ReadMethod(
        MetadataReader reader,
        ContractSignatureProvider provider,
        MetadataNames names,
        MethodDefinition method,
        GenericContext typeContext)
    {
        int genericArity = method.GetGenericParameters().Count;
        var context = typeContext with { MethodArity = genericArity };
        MethodSignature<string> signature = method.DecodeSignature(provider, context);
        IReadOnlyList<ParameterContract> parameters = ReadParameters(reader, method, signature.ParameterTypes);
        string methodName = reader.GetString(method.Name);

        return new MemberContract(
            methodName == ".ctor" ? "constructor" : "method",
            methodName,
            genericArity,
            GetMethodAccessibility(method.Attributes),
            method.Attributes.HasFlag(MethodAttributes.Static),
            method.Attributes.HasFlag(MethodAttributes.Abstract),
            method.Attributes.HasFlag(MethodAttributes.Virtual),
            method.Attributes.HasFlag(MethodAttributes.Final),
            signature.ReturnType,
            parameters,
            ReadGenericParameters(reader, provider, names, method.GetGenericParameters(), context),
            null,
            null,
            null,
            null,
            false,
            false,
            null,
            ReadRelevantAttributes(reader, names, method.GetCustomAttributes()));
    }

    private static MemberContract ReadProperty(
        MetadataReader reader,
        ContractSignatureProvider provider,
        MetadataNames names,
        PropertyDefinition property,
        PropertyAccessors accessors,
        GenericContext context)
    {
        MethodSignature<string> signature = property.DecodeSignature(provider, context);
        MethodDefinitionHandle representative = !accessors.Getter.IsNil ? accessors.Getter : accessors.Setter;
        MethodDefinition representativeMethod = reader.GetMethodDefinition(representative);
        IReadOnlyList<ParameterContract> parameters = ReadPropertyParameters(reader, representativeMethod, signature.ParameterTypes);

        return new MemberContract(
            "property",
            reader.GetString(property.Name),
            0,
            MostVisibleAccessibility(
                GetAccessorAccessibility(reader, accessors.Getter),
                GetAccessorAccessibility(reader, accessors.Setter)),
            representativeMethod.Attributes.HasFlag(MethodAttributes.Static),
            representativeMethod.Attributes.HasFlag(MethodAttributes.Abstract),
            representativeMethod.Attributes.HasFlag(MethodAttributes.Virtual),
            representativeMethod.Attributes.HasFlag(MethodAttributes.Final),
            signature.ReturnType,
            parameters,
            [],
            GetAccessorAccessibility(reader, accessors.Getter),
            GetAccessorAccessibility(reader, accessors.Setter),
            null,
            null,
            false,
            false,
            null,
            ReadRelevantAttributes(reader, names, property.GetCustomAttributes()));
    }

    private static MemberContract ReadEvent(
        MetadataReader reader,
        ContractSignatureProvider provider,
        MetadataNames names,
        EventDefinition @event,
        EventAccessors accessors,
        GenericContext context)
    {
        MethodDefinitionHandle representative = !accessors.Adder.IsNil ? accessors.Adder : accessors.Remover;
        MethodDefinition representativeMethod = reader.GetMethodDefinition(representative);

        return new MemberContract(
            "event",
            reader.GetString(@event.Name),
            0,
            MostVisibleAccessibility(
                GetAccessorAccessibility(reader, accessors.Adder),
                GetAccessorAccessibility(reader, accessors.Remover)),
            representativeMethod.Attributes.HasFlag(MethodAttributes.Static),
            representativeMethod.Attributes.HasFlag(MethodAttributes.Abstract),
            representativeMethod.Attributes.HasFlag(MethodAttributes.Virtual),
            representativeMethod.Attributes.HasFlag(MethodAttributes.Final),
            names.DecodeType(@event.Type, provider, context),
            [],
            [],
            null,
            null,
            GetAccessorAccessibility(reader, accessors.Adder),
            GetAccessorAccessibility(reader, accessors.Remover),
            false,
            false,
            null,
            ReadRelevantAttributes(reader, names, @event.GetCustomAttributes()));
    }

    private static MemberContract ReadField(
        MetadataReader reader,
        ContractSignatureProvider provider,
        MetadataNames names,
        FieldDefinition field,
        GenericContext context)
    {
        bool literal = field.Attributes.HasFlag(FieldAttributes.Literal);
        string? constant = literal && !field.GetDefaultValue().IsNil
            ? ReadConstant(reader, field.GetDefaultValue())
            : null;

        return new MemberContract(
            "field",
            reader.GetString(field.Name),
            0,
            GetFieldAccessibility(field.Attributes),
            field.Attributes.HasFlag(FieldAttributes.Static),
            false,
            false,
            false,
            field.DecodeSignature(provider, context),
            [],
            [],
            null,
            null,
            null,
            null,
            field.Attributes.HasFlag(FieldAttributes.InitOnly),
            literal,
            constant,
            ReadRelevantAttributes(reader, names, field.GetCustomAttributes()));
    }

    private static IReadOnlyList<ParameterContract> ReadParameters(
        MetadataReader reader,
        MethodDefinition method,
        ImmutableArray<string> signatureTypes)
    {
        var metadata = method.GetParameters()
            .Select(handle => reader.GetParameter(handle))
            .Where(parameter => parameter.SequenceNumber > 0)
            .ToDictionary(parameter => parameter.SequenceNumber - 1);

        var result = new ParameterContract[signatureTypes.Length];
        for (int index = 0; index < signatureTypes.Length; index++)
        {
            metadata.TryGetValue(index, out Parameter parameter);
            result[index] = CreateParameter(reader, index, signatureTypes[index], parameter);
        }

        return result;
    }

    private static IReadOnlyList<ParameterContract> ReadPropertyParameters(
        MetadataReader reader,
        MethodDefinition representative,
        ImmutableArray<string> signatureTypes)
    {
        // A setter has the assigned value as its final parameter; property signatures do not.
        IReadOnlyList<ParameterContract> methodParameters = ReadParameters(
            reader,
            representative,
            representative.DecodeSignature(new ContractSignatureProvider(new MetadataNames(reader)), default).ParameterTypes);
        return methodParameters.Take(signatureTypes.Length).ToArray();
    }

    private static ParameterContract CreateParameter(
        MetadataReader reader,
        int index,
        string signatureType,
        Parameter parameter)
    {
        bool byReference = signatureType.EndsWith('&');
        string type = byReference ? signatureType[..^1] : signatureType;
        string modifier = !byReference
            ? "value"
            : parameter.Attributes.HasFlag(ParameterAttributes.Out)
                ? "out"
                : parameter.Attributes.HasFlag(ParameterAttributes.In)
                    ? "in"
                    : "ref";
        bool hasDefault = !parameter.GetDefaultValue().IsNil;

        return new ParameterContract(
            index,
            parameter.Name.IsNil ? $"arg{index}" : reader.GetString(parameter.Name),
            type,
            modifier,
            parameter.Attributes.HasFlag(ParameterAttributes.Optional),
            hasDefault,
            hasDefault ? ReadConstant(reader, parameter.GetDefaultValue()) : null);
    }

    private static string[] ReadGenericParameters(
        MetadataReader reader,
        ContractSignatureProvider provider,
        MetadataNames names,
        GenericParameterHandleCollection handles,
        GenericContext context)
    {
        return handles
            .Select(handle => reader.GetGenericParameter(handle))
            .OrderBy(parameter => parameter.Index)
            .Select(parameter =>
            {
                GenericParameterAttributes attributes = parameter.Attributes;
                string variance = (attributes & GenericParameterAttributes.VarianceMask) switch
                {
                    GenericParameterAttributes.Covariant => "out",
                    GenericParameterAttributes.Contravariant => "in",
                    _ => "none",
                };
                var special = new List<string>();
                if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)) special.Add("class");
                if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)) special.Add("struct");
                if (attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint)) special.Add("new()");
                bool hasValueTypeConstraint =
                    attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint);
                string[] constraints = parameter.GetConstraints()
                    .Select(handle => reader.GetGenericParameterConstraint(handle))
                    .Select(constraint => names.DecodeType(constraint.Type, provider, context))
                    // C# emits System.ValueType alongside the CLI struct flag, while
                    // the C++/CLI XNA assemblies encode only the flag. They are the
                    // same public constraint. A modreq-decorated ValueType (used by
                    // `unmanaged`) remains visible and therefore still differs.
                    .Where(constraint => !(hasValueTypeConstraint &&
                        string.Equals(constraint, "System.ValueType", StringComparison.Ordinal)))
                    .Order(StringComparer.Ordinal)
                    .ToArray();
                return $"{parameter.Index}:{variance}:{string.Join("&", special)}:{string.Join("&", constraints)}";
            })
            .ToArray();
    }

    private static string GetLayout(TypeDefinition definition)
    {
        string kind = (definition.Attributes & TypeAttributes.LayoutMask) switch
        {
            TypeAttributes.SequentialLayout => "sequential",
            TypeAttributes.ExplicitLayout => "explicit",
            _ => "auto",
        };
        TypeLayout layout = definition.GetLayout();
        return $"{kind};pack={layout.PackingSize};size={layout.Size}";
    }

    private static string ReadConstant(MetadataReader reader, ConstantHandle handle)
    {
        Constant constant = reader.GetConstant(handle);
        BlobReader value = reader.GetBlobReader(constant.Value);
        return constant.TypeCode switch
        {
            ConstantTypeCode.Boolean => value.ReadBoolean() ? "true" : "false",
            ConstantTypeCode.Char => $"U+{value.ReadUInt16():X4}",
            ConstantTypeCode.SByte => value.ReadSByte().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Byte => value.ReadByte().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Int16 => value.ReadInt16().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt16 => value.ReadUInt16().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Int32 => value.ReadInt32().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt32 => value.ReadUInt32().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Int64 => value.ReadInt64().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt64 => value.ReadUInt64().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Single => value.ReadSingle().ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.Double => value.ReadDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            ConstantTypeCode.String => $"\"{value.ReadUTF16(value.Length)}\"",
            ConstantTypeCode.NullReference => "null",
            _ => $"<{constant.TypeCode}:{Convert.ToHexString(reader.GetBlobBytes(constant.Value))}>",
        };
    }

    private static IReadOnlyList<string> ReadRelevantAttributes(
        MetadataReader reader,
        MetadataNames names,
        CustomAttributeHandleCollection handles) =>
        handles
            .Select(handle => names.GetAttributeTypeName(reader.GetCustomAttribute(handle)))
            .Where(RelevantAttributes.Contains)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private bool IsContractType(string name) => _namespacePrefixes.Any(prefix =>
        name.Equals(prefix, StringComparison.Ordinal) ||
        name.StartsWith(prefix + ".", StringComparison.Ordinal) ||
        name.StartsWith(prefix + "+", StringComparison.Ordinal));

    private static bool IsEffectivelyVisibleType(MetadataReader reader, TypeDefinition definition)
    {
        TypeAttributes visibility = definition.Attributes & TypeAttributes.VisibilityMask;
        if (visibility == TypeAttributes.Public)
        {
            return true;
        }

        if (visibility is not (TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem))
        {
            return false;
        }

        TypeDefinitionHandle declaring = definition.GetDeclaringType();
        return !declaring.IsNil && IsEffectivelyVisibleType(reader, reader.GetTypeDefinition(declaring));
    }

    private static bool IsVisibleMethod(MethodAttributes attributes) =>
        (attributes & MethodAttributes.MemberAccessMask) is MethodAttributes.Public or
        MethodAttributes.Family or MethodAttributes.FamORAssem;

    private static bool IsVisibleField(FieldAttributes attributes) =>
        (attributes & FieldAttributes.FieldAccessMask) is FieldAttributes.Public or
        FieldAttributes.Family or FieldAttributes.FamORAssem;

    private static bool IsVisibleAccessor(MetadataReader reader, MethodDefinitionHandle handle) =>
        !handle.IsNil && IsVisibleMethod(reader.GetMethodDefinition(handle).Attributes);

    private static void AddIfPresent(HashSet<MethodDefinitionHandle> handles, MethodDefinitionHandle handle)
    {
        if (!handle.IsNil)
        {
            handles.Add(handle);
        }
    }

    private static string GetTypeAccessibility(TypeAttributes attributes) =>
        (attributes & TypeAttributes.VisibilityMask) switch
        {
            TypeAttributes.Public or TypeAttributes.NestedPublic => "public",
            TypeAttributes.NestedFamily => "protected",
            TypeAttributes.NestedFamORAssem => "protected internal",
            TypeAttributes.NestedFamANDAssem => "private protected",
            TypeAttributes.NestedPrivate => "private",
            _ => "internal",
        };

    private static string GetMethodAccessibility(MethodAttributes attributes) =>
        (attributes & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Public => "public",
            MethodAttributes.Family => "protected",
            MethodAttributes.FamORAssem => "protected internal",
            MethodAttributes.FamANDAssem => "private protected",
            MethodAttributes.Assembly => "internal",
            _ => "private",
        };

    private static string GetFieldAccessibility(FieldAttributes attributes) =>
        (attributes & FieldAttributes.FieldAccessMask) switch
        {
            FieldAttributes.Public => "public",
            FieldAttributes.Family => "protected",
            FieldAttributes.FamORAssem => "protected internal",
            FieldAttributes.FamANDAssem => "private protected",
            FieldAttributes.Assembly => "internal",
            _ => "private",
        };

    private static string? GetAccessorAccessibility(MetadataReader reader, MethodDefinitionHandle handle) =>
        handle.IsNil ? null : GetMethodAccessibility(reader.GetMethodDefinition(handle).Attributes);

    private static string MostVisibleAccessibility(string? first, string? second)
    {
        string[] order = ["public", "protected internal", "protected", "private protected", "internal", "private"];
        return order.First(value => first == value || second == value);
    }
}

internal readonly record struct GenericContext(int TypeArity, int MethodArity);

internal sealed class MetadataNames
{
    private readonly MetadataReader _reader;
    private readonly Dictionary<TypeDefinitionHandle, string> _definitionNames = [];
    private readonly Dictionary<TypeReferenceHandle, string> _referenceNames = [];

    public MetadataNames(MetadataReader reader)
    {
        _reader = reader;
    }

    public string GetTypeDefinitionName(TypeDefinitionHandle handle)
    {
        if (_definitionNames.TryGetValue(handle, out string? cached))
        {
            return cached;
        }

        TypeDefinition definition = _reader.GetTypeDefinition(handle);
        string simpleName = _reader.GetString(definition.Name);
        TypeDefinitionHandle declaring = definition.GetDeclaringType();
        string name = !declaring.IsNil
            ? GetTypeDefinitionName(declaring) + "+" + simpleName
            : JoinNamespace(_reader.GetString(definition.Namespace), simpleName);
        _definitionNames.Add(handle, name);
        return name;
    }

    public string GetTypeReferenceName(TypeReferenceHandle handle)
    {
        if (_referenceNames.TryGetValue(handle, out string? cached))
        {
            return cached;
        }

        TypeReference reference = _reader.GetTypeReference(handle);
        string simpleName = _reader.GetString(reference.Name);
        string name = reference.ResolutionScope.Kind == HandleKind.TypeReference
            ? GetTypeReferenceName((TypeReferenceHandle)reference.ResolutionScope) + "+" + simpleName
            : JoinNamespace(_reader.GetString(reference.Namespace), simpleName);
        _referenceNames.Add(handle, name);
        return name;
    }

    public string DecodeType(EntityHandle handle, ContractSignatureProvider provider, GenericContext context) =>
        handle.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeDefinitionName((TypeDefinitionHandle)handle),
            HandleKind.TypeReference => GetTypeReferenceName((TypeReferenceHandle)handle),
            HandleKind.TypeSpecification => _reader.GetTypeSpecification((TypeSpecificationHandle)handle)
                .DecodeSignature(provider, context),
            _ => $"<{handle.Kind}:{MetadataTokens.GetToken(handle):X8}>",
        };

    public string GetAttributeTypeName(CustomAttribute attribute)
    {
        EntityHandle parent = attribute.Constructor.Kind switch
        {
            HandleKind.MethodDefinition => _reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor)
                .GetDeclaringType(),
            HandleKind.MemberReference => _reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
            _ => default,
        };

        return parent.IsNil ? string.Empty : parent.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeDefinitionName((TypeDefinitionHandle)parent),
            HandleKind.TypeReference => GetTypeReferenceName((TypeReferenceHandle)parent),
            _ => string.Empty,
        };
    }

    private static string JoinNamespace(string @namespace, string name) =>
        string.IsNullOrEmpty(@namespace) ? name : @namespace + "." + name;
}

internal sealed class ContractSignatureProvider : ISignatureTypeProvider<string, GenericContext>
{
    private readonly MetadataNames _names;

    public ContractSignatureProvider(MetadataNames names)
    {
        _names = names;
    }

    public string GetArrayType(string elementType, ArrayShape shape) =>
        elementType + "[" + new string(',', Math.Max(0, shape.Rank - 1)) + "]";

    public string GetByReferenceType(string elementType) => elementType + "&";

    public string GetFunctionPointerType(MethodSignature<string> signature) =>
        $"methodptr({string.Join(",", signature.ParameterTypes)})->{signature.ReturnType}";

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
        genericType + "<" + string.Join(",", typeArguments) + ">";

    public string GetGenericMethodParameter(GenericContext genericContext, int index) => "``" + index;

    public string GetGenericTypeParameter(GenericContext genericContext, int index) => "`" + index;

    public string GetModifiedType(string modifierType, string unmodifiedType, bool isRequired) =>
        $"{unmodifiedType} {(isRequired ? "modreq" : "modopt")}({modifierType})";

    public string GetPinnedType(string elementType) => elementType + " pinned";

    public string GetPointerType(string elementType) => elementType + "*";

    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Boolean => "System.Boolean",
        PrimitiveTypeCode.Byte => "System.Byte",
        PrimitiveTypeCode.Char => "System.Char",
        PrimitiveTypeCode.Double => "System.Double",
        PrimitiveTypeCode.Int16 => "System.Int16",
        PrimitiveTypeCode.Int32 => "System.Int32",
        PrimitiveTypeCode.Int64 => "System.Int64",
        PrimitiveTypeCode.IntPtr => "System.IntPtr",
        PrimitiveTypeCode.Object => "System.Object",
        PrimitiveTypeCode.SByte => "System.SByte",
        PrimitiveTypeCode.Single => "System.Single",
        PrimitiveTypeCode.String => "System.String",
        PrimitiveTypeCode.TypedReference => "System.TypedReference",
        PrimitiveTypeCode.UInt16 => "System.UInt16",
        PrimitiveTypeCode.UInt32 => "System.UInt32",
        PrimitiveTypeCode.UInt64 => "System.UInt64",
        PrimitiveTypeCode.UIntPtr => "System.UIntPtr",
        PrimitiveTypeCode.Void => "System.Void",
        _ => $"<{typeCode}>",
    };

    public string GetSZArrayType(string elementType) => elementType + "[]";

    public string GetTypeFromDefinition(
        MetadataReader reader,
        TypeDefinitionHandle handle,
        byte rawTypeKind) => _names.GetTypeDefinitionName(handle);

    public string GetTypeFromReference(
        MetadataReader reader,
        TypeReferenceHandle handle,
        byte rawTypeKind) => _names.GetTypeReferenceName(handle);

    public string GetTypeFromSpecification(
        MetadataReader reader,
        GenericContext genericContext,
        TypeSpecificationHandle handle,
        byte rawTypeKind) => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
}
