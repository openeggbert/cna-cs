using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace Microsoft.Xna.Framework.Design;

/// <summary>Shared XNA design-time behavior for the framework's math value types.</summary>
public class MathTypeConverter : ExpandableObjectConverter
{
    protected PropertyDescriptorCollection propertyDescriptions = null!;
    protected bool supportStringConvert = true;

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        (supportStringConvert && sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);

    public override bool GetCreateInstanceSupported(ITypeDescriptorContext? context) => true;

    public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;

    public override PropertyDescriptorCollection GetProperties(
        ITypeDescriptorContext? context,
        object value,
        Attribute[]? attributes) => propertyDescriptions;
}

public class PointConverter : MathTypeConverter
{
    private static readonly string[] Names = ["X", "Y"];

    public PointConverter() => propertyDescriptions = DesignConverterSupport.Properties(typeof(Point), Names);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        DesignConverterSupport.TryConvertFromString<Point>(context, culture, value, Names, [typeof(int), typeof(int)], out object? result)
            ? result
            : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        DesignConverterSupport.TryConvertTo(context, culture, value, destinationType, typeof(Point), Names, [typeof(int), typeof(int)], out object? result)
            ? result
            : base.ConvertTo(context, culture, value, destinationType);

    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) =>
        new Point(DesignConverterSupport.Get<int>(propertyValues, "X"), DesignConverterSupport.Get<int>(propertyValues, "Y"));
}

public class RectangleConverter : MathTypeConverter
{
    private static readonly string[] Names = ["X", "Y", "Width", "Height"];

    public RectangleConverter()
    {
        supportStringConvert = false;
        propertyDescriptions = DesignConverterSupport.Properties(typeof(Rectangle), Names);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        DesignConverterSupport.TryConvertTo(context, culture, value, destinationType, typeof(Rectangle), Names,
            [typeof(int), typeof(int), typeof(int), typeof(int)], out object? result)
            ? result
            : base.ConvertTo(context, culture, value, destinationType);

    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) => new Rectangle(
        DesignConverterSupport.Get<int>(propertyValues, "X"),
        DesignConverterSupport.Get<int>(propertyValues, "Y"),
        DesignConverterSupport.Get<int>(propertyValues, "Width"),
        DesignConverterSupport.Get<int>(propertyValues, "Height"));
}

public class Vector2Converter : MathTypeConverter
{
    private static readonly string[] Names = ["X", "Y"];

    public Vector2Converter() => propertyDescriptions = DesignConverterSupport.Properties(typeof(Vector2), Names);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        DesignConverterSupport.TryConvertFromString<Vector2>(context, culture, value, Names, [typeof(float), typeof(float)], out object? result)
            ? result
            : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        DesignConverterSupport.TryConvertTo(context, culture, value, destinationType, typeof(Vector2), Names,
            [typeof(float), typeof(float)], out object? result)
            ? result
            : base.ConvertTo(context, culture, value, destinationType);

    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) =>
        new Vector2(DesignConverterSupport.Get<float>(propertyValues, "X"), DesignConverterSupport.Get<float>(propertyValues, "Y"));
}

public class Vector3Converter : MathTypeConverter
{
    private static readonly string[] Names = ["X", "Y", "Z"];

    public Vector3Converter() => propertyDescriptions = DesignConverterSupport.Properties(typeof(Vector3), Names);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        DesignConverterSupport.TryConvertFromString<Vector3>(context, culture, value, Names,
            [typeof(float), typeof(float), typeof(float)], out object? result)
            ? result
            : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        DesignConverterSupport.TryConvertTo(context, culture, value, destinationType, typeof(Vector3), Names,
            [typeof(float), typeof(float), typeof(float)], out object? result)
            ? result
            : base.ConvertTo(context, culture, value, destinationType);

    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) => new Vector3(
        DesignConverterSupport.Get<float>(propertyValues, "X"),
        DesignConverterSupport.Get<float>(propertyValues, "Y"),
        DesignConverterSupport.Get<float>(propertyValues, "Z"));
}

public class Vector4Converter : MathTypeConverter
{
    private static readonly string[] Names = ["X", "Y", "Z", "W"];

    public Vector4Converter() => propertyDescriptions = DesignConverterSupport.Properties(typeof(Vector4), Names);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        DesignConverterSupport.TryConvertFromString<Vector4>(context, culture, value, Names,
            [typeof(float), typeof(float), typeof(float), typeof(float)], out object? result)
            ? result
            : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        DesignConverterSupport.TryConvertTo(context, culture, value, destinationType, typeof(Vector4), Names,
            [typeof(float), typeof(float), typeof(float), typeof(float)], out object? result)
            ? result
            : base.ConvertTo(context, culture, value, destinationType);

    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) => new Vector4(
        DesignConverterSupport.Get<float>(propertyValues, "X"),
        DesignConverterSupport.Get<float>(propertyValues, "Y"),
        DesignConverterSupport.Get<float>(propertyValues, "Z"),
        DesignConverterSupport.Get<float>(propertyValues, "W"));
}

public class QuaternionConverter : MathTypeConverter
{
    private static readonly string[] Names = ["X", "Y", "Z", "W"];

    public QuaternionConverter() => propertyDescriptions = DesignConverterSupport.Properties(typeof(Quaternion), Names);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        DesignConverterSupport.TryConvertFromString<Quaternion>(context, culture, value, Names,
            [typeof(float), typeof(float), typeof(float), typeof(float)], out object? result)
            ? result
            : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        DesignConverterSupport.TryConvertTo(context, culture, value, destinationType, typeof(Quaternion), Names,
            [typeof(float), typeof(float), typeof(float), typeof(float)], out object? result)
            ? result
            : base.ConvertTo(context, culture, value, destinationType);

    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) => new Quaternion(
        DesignConverterSupport.Get<float>(propertyValues, "X"),
        DesignConverterSupport.Get<float>(propertyValues, "Y"),
        DesignConverterSupport.Get<float>(propertyValues, "Z"),
        DesignConverterSupport.Get<float>(propertyValues, "W"));
}

public class MatrixConverter : MathTypeConverter
{
    private static readonly string[] Names =
    [
        "M11", "M12", "M13", "M14", "M21", "M22", "M23", "M24",
        "M31", "M32", "M33", "M34", "M41", "M42", "M43", "M44",
    ];

    public MatrixConverter()
    {
        supportStringConvert = false;
        propertyDescriptions = DesignConverterSupport.Properties(typeof(Matrix), Names);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        DesignConverterSupport.TryConvertTo(context, culture, value, destinationType, typeof(Matrix), Names,
            Enumerable.Repeat(typeof(float), 16).ToArray(), out object? result)
            ? result
            : base.ConvertTo(context, culture, value, destinationType);

    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) => new Matrix(
        DesignConverterSupport.Get<float>(propertyValues, "M11"), DesignConverterSupport.Get<float>(propertyValues, "M12"),
        DesignConverterSupport.Get<float>(propertyValues, "M13"), DesignConverterSupport.Get<float>(propertyValues, "M14"),
        DesignConverterSupport.Get<float>(propertyValues, "M21"), DesignConverterSupport.Get<float>(propertyValues, "M22"),
        DesignConverterSupport.Get<float>(propertyValues, "M23"), DesignConverterSupport.Get<float>(propertyValues, "M24"),
        DesignConverterSupport.Get<float>(propertyValues, "M31"), DesignConverterSupport.Get<float>(propertyValues, "M32"),
        DesignConverterSupport.Get<float>(propertyValues, "M33"), DesignConverterSupport.Get<float>(propertyValues, "M34"),
        DesignConverterSupport.Get<float>(propertyValues, "M41"), DesignConverterSupport.Get<float>(propertyValues, "M42"),
        DesignConverterSupport.Get<float>(propertyValues, "M43"), DesignConverterSupport.Get<float>(propertyValues, "M44"));
}

public class BoundingBoxConverter : MathTypeConverter
{
    private static readonly string[] Names = ["Min", "Max"];

    public BoundingBoxConverter() => propertyDescriptions = DesignConverterSupport.Properties(typeof(BoundingBox), Names);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        DesignConverterSupport.TryConvertFromString<BoundingBox>(context, culture, value, Names,
            [typeof(Vector3), typeof(Vector3)], out object? result)
            ? result
            : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        DesignConverterSupport.TryConvertTo(context, culture, value, destinationType, typeof(BoundingBox), Names,
            [typeof(Vector3), typeof(Vector3)], out object? result)
            ? result
            : base.ConvertTo(context, culture, value, destinationType);

    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) => new BoundingBox(
        DesignConverterSupport.Get<Vector3>(propertyValues, "Min"),
        DesignConverterSupport.Get<Vector3>(propertyValues, "Max"));
}

public class BoundingSphereConverter : MathTypeConverter
{
    private static readonly string[] Names = ["Center", "Radius"];

    public BoundingSphereConverter() => propertyDescriptions = DesignConverterSupport.Properties(typeof(BoundingSphere), Names);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        DesignConverterSupport.TryConvertFromString<BoundingSphere>(context, culture, value, Names,
            [typeof(Vector3), typeof(float)], out object? result)
            ? result
            : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        DesignConverterSupport.TryConvertTo(context, culture, value, destinationType, typeof(BoundingSphere), Names,
            [typeof(Vector3), typeof(float)], out object? result)
            ? result
            : base.ConvertTo(context, culture, value, destinationType);

    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) => new BoundingSphere(
        DesignConverterSupport.Get<Vector3>(propertyValues, "Center"),
        DesignConverterSupport.Get<float>(propertyValues, "Radius"));
}

public class PlaneConverter : MathTypeConverter
{
    private static readonly string[] Names = ["Normal", "D"];

    public PlaneConverter()
    {
        supportStringConvert = false;
        propertyDescriptions = DesignConverterSupport.Properties(typeof(Plane), Names);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        DesignConverterSupport.TryConvertTo(context, culture, value, destinationType, typeof(Plane), Names,
            [typeof(Vector3), typeof(float)], out object? result)
            ? result
            : base.ConvertTo(context, culture, value, destinationType);

    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) => new Plane(
        DesignConverterSupport.Get<Vector3>(propertyValues, "Normal"),
        DesignConverterSupport.Get<float>(propertyValues, "D"));
}

public class RayConverter : MathTypeConverter
{
    private static readonly string[] Names = ["Position", "Direction"];

    public RayConverter() => propertyDescriptions = DesignConverterSupport.Properties(typeof(Ray), Names);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        DesignConverterSupport.TryConvertFromString<Ray>(context, culture, value, Names,
            [typeof(Vector3), typeof(Vector3)], out object? result)
            ? result
            : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        DesignConverterSupport.TryConvertTo(context, culture, value, destinationType, typeof(Ray), Names,
            [typeof(Vector3), typeof(Vector3)], out object? result)
            ? result
            : base.ConvertTo(context, culture, value, destinationType);

    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) => new Ray(
        DesignConverterSupport.Get<Vector3>(propertyValues, "Position"),
        DesignConverterSupport.Get<Vector3>(propertyValues, "Direction"));
}

public class ColorConverter : MathTypeConverter
{
    private static readonly string[] Names = ["R", "G", "B", "A"];
    private static readonly Type[] ConstructorTypes = [typeof(int), typeof(int), typeof(int), typeof(int)];

    public ColorConverter() => propertyDescriptions = DesignConverterSupport.Properties(typeof(Color), Names);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        DesignConverterSupport.TryConvertFromString<Color>(context, culture, value, Names,
            [typeof(byte), typeof(byte), typeof(byte), typeof(byte)], out object? result)
            ? result
            : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        DesignConverterSupport.TryConvertTo(context, culture, value, destinationType, typeof(Color), Names,
            ConstructorTypes, out object? result)
            ? result
            : base.ConvertTo(context, culture, value, destinationType);

    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) => new Color(
        DesignConverterSupport.Get<byte>(propertyValues, "R"),
        DesignConverterSupport.Get<byte>(propertyValues, "G"),
        DesignConverterSupport.Get<byte>(propertyValues, "B"),
        DesignConverterSupport.Get<byte>(propertyValues, "A"));
}

internal static class DesignConverterSupport
{
    internal static PropertyDescriptorCollection Properties(Type type, IReadOnlyList<string> names)
    {
        var descriptors = new PropertyDescriptor[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            MemberInfo member = (MemberInfo?)type.GetField(names[i], BindingFlags.Instance | BindingFlags.Public)
                ?? type.GetProperty(names[i], BindingFlags.Instance | BindingFlags.Public)
                ?? throw new InvalidOperationException($"{type.FullName} has no public member '{names[i]}'.");
            descriptors[i] = new ValueMemberPropertyDescriptor(member);
        }

        return new PropertyDescriptorCollection(descriptors, readOnly: true);
    }

    internal static bool TryConvertFromString<T>(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object value,
        IReadOnlyList<string> names,
        Type[] constructorTypes,
        out object? result)
    {
        if (value is not string text)
        {
            result = null;
            return false;
        }

        culture ??= CultureInfo.CurrentCulture;
        string[] parts = text.Trim().Split([culture.TextInfo.ListSeparator], StringSplitOptions.None);
        if (parts.Length != constructorTypes.Length)
        {
            throw new ArgumentException(
                $"Expected {string.Join(culture.TextInfo.ListSeparator, names)}.",
                nameof(value));
        }

        var values = new object?[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            values[i] = TypeDescriptor.GetConverter(constructorTypes[i]).ConvertFromString(context, culture, parts[i].Trim());
        }

        ConstructorInfo constructor = typeof(T).GetConstructor(constructorTypes)
            ?? throw new InvalidOperationException($"{typeof(T).FullName} has no expected design-time constructor.");
        result = constructor.Invoke(values);
        return true;
    }

    internal static bool TryConvertTo(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value,
        Type destinationType,
        Type valueType,
        IReadOnlyList<string> names,
        Type[] constructorTypes,
        out object? result)
    {
        if (value is null || !valueType.IsInstanceOfType(value))
        {
            result = null;
            return false;
        }

        culture ??= CultureInfo.CurrentCulture;
        object?[] values = ReadValues(value, valueType, names, constructorTypes);
        if (destinationType == typeof(string))
        {
            var converted = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                converted[i] = TypeDescriptor.GetConverter(constructorTypes[i])
                    .ConvertToString(context, culture, values[i]) ?? string.Empty;
            }

            result = string.Join(culture.TextInfo.ListSeparator + " ", converted);
            return true;
        }

        if (destinationType == typeof(InstanceDescriptor))
        {
            ConstructorInfo constructor = valueType.GetConstructor(constructorTypes)
                ?? throw new InvalidOperationException($"{valueType.FullName} has no expected design-time constructor.");
            result = new InstanceDescriptor(constructor, values);
            return true;
        }

        result = null;
        return false;
    }

    internal static T Get<T>(IDictionary values, string name)
    {
        object? value = values[name];
        if (value is T exact)
        {
            return exact;
        }

        return (T)Convert.ChangeType(value!, typeof(T), CultureInfo.InvariantCulture);
    }

    private static object?[] ReadValues(
        object value,
        Type valueType,
        IReadOnlyList<string> names,
        IReadOnlyList<Type> constructorTypes)
    {
        var values = new object?[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            MemberInfo member = (MemberInfo?)valueType.GetField(names[i], BindingFlags.Instance | BindingFlags.Public)
                ?? valueType.GetProperty(names[i], BindingFlags.Instance | BindingFlags.Public)
                ?? throw new InvalidOperationException($"{valueType.FullName} has no public member '{names[i]}'.");
            object? memberValue = member is FieldInfo field
                ? field.GetValue(value)
                : ((PropertyInfo)member).GetValue(value);
            values[i] = memberValue is null || constructorTypes[i].IsInstanceOfType(memberValue)
                ? memberValue
                : Convert.ChangeType(memberValue, constructorTypes[i], CultureInfo.InvariantCulture);
        }

        return values;
    }

    private sealed class ValueMemberPropertyDescriptor : PropertyDescriptor
    {
        private readonly FieldInfo? _field;
        private readonly PropertyInfo? _property;

        internal ValueMemberPropertyDescriptor(MemberInfo member)
            : base(member.Name, member.GetCustomAttributes<Attribute>().ToArray())
        {
            _field = member as FieldInfo;
            _property = member as PropertyInfo;
        }

        public override Type ComponentType => (_field?.DeclaringType ?? _property!.DeclaringType)!;

        public override bool IsReadOnly => false;

        public override Type PropertyType => _field?.FieldType ?? _property!.PropertyType;

        public override bool CanResetValue(object component) => false;

        public override object? GetValue(object? component) =>
            _field is not null ? _field.GetValue(component) : _property!.GetValue(component);

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object? component, object? value)
        {
            if (_field is not null)
            {
                _field.SetValue(component, value);
            }
            else
            {
                _property!.SetValue(component, value);
            }

            OnValueChanged(component, EventArgs.Empty);
        }

        public override bool ShouldSerializeValue(object component) => false;
    }
}
