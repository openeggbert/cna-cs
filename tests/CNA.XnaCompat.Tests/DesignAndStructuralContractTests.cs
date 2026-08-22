using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Design;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input.Touch;
using XnaCompatibilityCompileProbe;
using Xunit;

// Keep this test namespace outside `CNA.*`: unqualified framework value-type names must resolve
// to the imported Microsoft.Xna.Framework facade, never to its parallel CNA implementation type.
namespace XnaCompat.Tests;

public class DesignAndStructuralContractTests
{
    [Fact]
    public void MathValueTypes_ExposeTheirXnaDesignConverters()
    {
        (Type ValueType, Type ConverterType)[] contracts =
        [
            (typeof(Point), typeof(PointConverter)),
            (typeof(Rectangle), typeof(RectangleConverter)),
            (typeof(Vector2), typeof(Vector2Converter)),
            (typeof(Vector3), typeof(Vector3Converter)),
            (typeof(Vector4), typeof(Vector4Converter)),
            (typeof(Quaternion), typeof(QuaternionConverter)),
            (typeof(Matrix), typeof(MatrixConverter)),
            (typeof(BoundingBox), typeof(BoundingBoxConverter)),
            (typeof(BoundingSphere), typeof(BoundingSphereConverter)),
            (typeof(Plane), typeof(PlaneConverter)),
            (typeof(Ray), typeof(RayConverter)),
            (typeof(Color), typeof(ColorConverter)),
        ];

        foreach ((Type valueType, Type converterType) in contracts)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(valueType);
            Assert.Equal(converterType, converter.GetType());
            Assert.True(converter.GetCreateInstanceSupported());
            Assert.True(converter.GetPropertiesSupported());
        }
    }

    [Fact]
    public void Vector3Converter_RoundTripsCultureAwareStringsAndInstanceDescriptors()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(Vector3));
        var source = new Vector3(1.25f, -2.5f, 3.75f);

        string text = Assert.IsType<string>(converter.ConvertTo(null, CultureInfo.InvariantCulture, source, typeof(string)));
        var parsed = Assert.IsType<Vector3>(converter.ConvertFrom(null, CultureInfo.InvariantCulture, text));
        Assert.Equal(source, parsed);

        var descriptor = Assert.IsType<InstanceDescriptor>(
            converter.ConvertTo(null, CultureInfo.InvariantCulture, source, typeof(InstanceDescriptor)));
        Assert.Equal(source, Assert.IsType<Vector3>(descriptor.Invoke()));

        PropertyDescriptorCollection properties = converter.GetProperties(source)!;
        Assert.Equal(["X", "Y", "Z"], properties.Cast<PropertyDescriptor>().Select(property => property.Name));
    }

    [Fact]
    public void NonStringDesignConverters_StillProduceConstructorDescriptors()
    {
        TypeConverter rectangleConverter = TypeDescriptor.GetConverter(typeof(Rectangle));
        Assert.False(rectangleConverter.CanConvertFrom(typeof(string)));
        var rectangle = new Rectangle(1, 2, 3, 4);
        var rectangleDescriptor = Assert.IsType<InstanceDescriptor>(
            rectangleConverter.ConvertTo(rectangle, typeof(InstanceDescriptor)));
        Assert.Equal(rectangle, Assert.IsType<Rectangle>(rectangleDescriptor.Invoke()));

        TypeConverter matrixConverter = TypeDescriptor.GetConverter(typeof(Matrix));
        Assert.False(matrixConverter.CanConvertFrom(typeof(string)));
        var matrixDescriptor = Assert.IsType<InstanceDescriptor>(
            matrixConverter.ConvertTo(Matrix.Identity, typeof(InstanceDescriptor)));
        Assert.Equal(Matrix.Identity, Assert.IsType<Matrix>(matrixDescriptor.Invoke()));
    }

    [Fact]
    public void ColorConverter_RoundTripsByteComponents()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(Color));
        var source = new Color(10, 20, 30, 40);

        string text = Assert.IsType<string>(converter.ConvertTo(null, CultureInfo.InvariantCulture, source, typeof(string)));
        Assert.Equal(source, Assert.IsType<Color>(converter.ConvertFrom(null, CultureInfo.InvariantCulture, text)));

        var descriptor = Assert.IsType<InstanceDescriptor>(
            converter.ConvertTo(null, CultureInfo.InvariantCulture, source, typeof(InstanceDescriptor)));
        Assert.Equal(source, Assert.IsType<Color>(descriptor.Invoke()));
    }

    [Fact]
    public void TouchCollection_UsesTheXnaListAndEnumeratorShape()
    {
        Type collectionType = typeof(TouchCollection);
        Assert.True(typeof(IList<TouchLocation>).IsAssignableFrom(collectionType));

        Type? enumeratorType = collectionType.GetNestedType("Enumerator", BindingFlags.Public);
        Assert.NotNull(enumeratorType);
        Assert.True(enumeratorType.IsValueType);
        Assert.True(typeof(IEnumerator<TouchLocation>).IsAssignableFrom(enumeratorType));

        var first = new TouchLocation(7, TouchLocationState.Pressed, new Vector2(1f, 2f));
        var second = new TouchLocation(8, TouchLocationState.Moved, new Vector2(3f, 4f));
        var collection = new TouchCollection([first, second]);

        Assert.True(collection.IsConnected);
        Assert.False(default(TouchCollection).IsConnected);
        Assert.Equal(2, collection.Count);
        Assert.True(collection.IsReadOnly);
        Assert.True(collection.FindById(8, out TouchLocation found));
        Assert.Equal(second, found);
        Assert.Equal([first, second], collection.ToArray());

        Assert.Throws<NotSupportedException>(() => ((IList<TouchLocation>)collection).Add(first));
        Assert.Throws<NotSupportedException>(() => ((IList<TouchLocation>)collection)[0] = second);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TouchCollection(new TouchLocation[9]));
    }

    [Fact]
    public void Color_PacksAndRoundsLikeXna()
    {
        var bytes = new Color(1, 2, 3, 4);
        Assert.Equal(0x04030201u, bytes.PackedValue);

        bytes.G = 200;
        Assert.Equal(200, bytes.G);
        Assert.Equal(0x0403C801u, bytes.PackedValue);

        var floats = new Color(0.5f, float.NaN, float.PositiveInfinity, float.NegativeInfinity);
        Assert.Equal(128, floats.R);
        Assert.Equal(0, floats.G);
        Assert.Equal(255, floats.B);
        Assert.Equal(0, floats.A);

        Color midpoint = Color.Lerp(new Color(0, 0, 0, 0), new Color(255, 255, 255, 255), 0.5f);
        Assert.Equal(new Color(127, 127, 127, 127), midpoint);
        Assert.Equal(new Color(127, 127, 127, 127), Color.Multiply(Color.White, 0.5f));
    }

    [Fact]
    public void GamerServicesComponent_HasTheXnaLifecycleSurface()
    {
        Type type = typeof(GamerServicesComponent);
        Assert.Equal(typeof(GameComponent), type.BaseType);
        Assert.NotNull(type.GetConstructor([typeof(Game)]));
        Assert.Equal(type, type.GetMethod(nameof(GamerServicesComponent.Initialize))!.DeclaringType);
        Assert.Equal(type, type.GetMethod(nameof(GamerServicesComponent.Update), [typeof(GameTime)])!.DeclaringType);
    }

    [Fact]
    public void MathBehaviorCorpus_IsDeterministicAndCoversCriticalEdges()
    {
        IReadOnlyList<string> first = MathBehaviorCorpus.Capture();
        IReadOnlyList<string> second = MathBehaviorCorpus.Capture();

        Assert.Equal(first, second);
        Assert.Equal(83, first.Count);
        Assert.Contains("v2.normalize.zero=FFC00000,FFC00000", first);
        Assert.Contains("vector.divide.scalar=3EDB6DB8,40155556,458099CA,3EAAAAAB", first);
        Assert.Contains("q.inverse.zero=FFC00000,FFC00000,FFC00000,FFC00000", first);
        Assert.Contains("q.multiply.grouped=CE47A05E,CF03EDF7,4FC9C4DD,5011D115", first);
        Assert.Contains(
            "matrix.inverse.product=3F800000,00000000,B2000000,00000000," +
            "00000000,3F800000,00000000,00000000,33000000,00000000,3F800000,00000000," +
            "34000000,00000000,00000000,3F800000",
            first);
        Assert.Contains("viewport.project=43D42808,43AC9F3C,3F63AFF4", first);
        Assert.Contains("viewport.unproject=3E7FFE10,BEFFF906,3FA00111", first);
        Assert.Contains("viewport.unproject.singular=FFC00000,FFC00000,FFC00000", first);
        Assert.Contains("color.pack=00FF0080", first);
        Assert.Contains("color.lerp=7F7F7F7F", first);
        Assert.Contains("color.nonpremultiplied.extreme=FFFFFFFF", first);
        Assert.Contains("box.contains.edge=1", first);
        Assert.Contains("box.nan=0,1", first);
        Assert.Contains("sphere.contains.edge=0", first);
        Assert.Contains("sphere.points=3F800000,40800000,40000000,4101FC10", first);
        Assert.Contains("ray.sphere=40810421", first);
        Assert.Contains("v2.equals.nan=0,0", first);
        Assert.Contains("matrix.equals.nan=0,0", first);
        Assert.Contains("v3.hash=-1077936128", first);
        Assert.Contains("matrix.identity.hash=-33554432", first);
        Assert.Contains("integer.hash=3,10", first);
        Assert.Contains("sphere.negative=ArgumentException", first);
        Assert.Contains("math.clamp.reversed=40000000", first);
        Assert.Contains("math.wrap.large=BFC2E06C", first);
        Assert.Contains("math.splines=C1218313,C1351EBA", first);
        Assert.Contains("math.hermite.endpoint.nan=1", first);
        Assert.Contains("sphere.intersects.tangent=0", first);
        Assert.Contains("box.ray.nearparallel=none", first);
        Assert.Contains("ray.plane.nearparallel=none", first);
        Assert.Contains("ray.plane.overloads=00000000,00000000", first);
        Assert.Contains("v3.transform.negative.length=none", first);
        Assert.Contains("v3.transform.negative.index=IndexOutOfRangeException", first);
        Assert.Contains("v3.min.nan=40E00000,FFC00000,FFC00000", first);
        Assert.Contains("v3.clamp.reversed=40000000,40000000,40000000", first);
        Assert.Contains("q.slerp=BD9A16EC,3E60D7E7,00000000,3F79023D", first);
        Assert.Contains("q.axis.large=00000000,3F30464F,00000000,BF39A48F", first);
        Assert.Contains("q.from.matrix=00000000,3EAF904C,00000000,3F707ABB", first);
        Assert.Contains("matrix.rotation.large=3D53E807,BF7FA83D", first);
        Assert.Contains("matrix.perspective.infinity=FFC00000,FFC00000", first);
        Assert.Contains("matrix.fov.invalid=ArgumentOutOfRangeException", first);
        Assert.Contains(
            "matrix.decompose.mirror=1,40000000,40400000,C0800000," +
            "00000000,3F7E00AA,00000000,BDFF5579,40A00000,40C00000,40E00000",
            first);
        Assert.Contains("matrix.billboard.axis=BF800000,40000000,BF800000", first);
        Assert.Contains("matrix.shadow.zero.nan=1,1", first);
        Assert.Contains("matrix.reflection.ref=3F800000,40000000,BF800000,C0800000", first);
        Assert.Contains(
            "matrix.lookat.degenerate=FFC00000,FFC00000,FFC00000,00000000," +
            "FFC00000,FFC00000,FFC00000,00000000,FFC00000,FFC00000,FFC00000,00000000," +
            "7FC00000,7FC00000,7FC00000,3F800000",
            first);
        Assert.Contains("matrix.transform.infinity=3F800000,7F800000,0", first);
        Assert.Contains("negate.signedzero=80000000,80000000,80000000", first);
        Assert.Contains(
            "matrix.tostring={ {M11:1 M12:0 M13:0 M14:0} " +
            "{M21:0 M22:1 M23:0 M24:0} {M31:0 M32:0 M33:1 M34:0} " +
            "{M41:0 M42:0 M43:0 M44:1} }",
            first);
        Assert.Contains("plane.points.degenerate=FFC00000,FFC00000,FFC00000,7FC00000", first);
        Assert.Contains("plane.normalize.nearunit=3F19999A,3F4CCCCC,00000000,40000000", first);
        Assert.Contains("plane.box.coplanar=2", first);
        Assert.Contains("curve.key.hash=4194305", first);
        Assert.Contains("curve.key.compare=1,1,NullReferenceException", first);
        Assert.Contains("curve.collection.reposition=40000000,40400000", first);
        Assert.Contains(
            "curve.collection.oob=ArgumentOutOfRangeException,ArgumentOutOfRangeException",
            first);
        Assert.Contains("curve.tangent.epsilon=00000000,00000000", first);
        Assert.Contains("curve.cycle.preboundary=41A00000", first);
        Assert.Contains("curve.step.nan=41A00000", first);
        Assert.Contains("packed.unorm.midpoint=00,0000", first);
        Assert.Contains("packed.unsigned.rounding=04020200", first);
        Assert.Contains("packed.snorm.rounding=0000", first);
        Assert.Contains("packed.snorm.minimum=BF800000,BF800000", first);
        Assert.Contains("packed.signed.rounding=00020000", first);
        Assert.Contains("packed.half.saturation=7FFF,7FFF,47800000", first);
        Assert.Contains("packed.tostring=0A,000A,0000000A", first);
        Assert.Contains("frustum.near=80000000,80000000,3F800000,C0800000", first);
        Assert.Contains("frustum.top=00000000,3F6C835F,3EC3EF16,BFF4EADB", first);
        Assert.Contains("frustum.corner0=BF0D6289,3ED413CB,40800000", first);
        Assert.Contains("frustum.corner6=40B0BB28,C0848C5D,C09FFFF8", first);
        Assert.Contains("frustum.contains=1,0,1,1", first);
        Assert.Contains("frustum.gjk=1,0,1,0,0", first);
        Assert.Contains("frustum.ray=41800000", first);

        Matrix singular = Matrix.Invert(default);
        Assert.True(float.IsNaN(singular.M11));
        Assert.True(float.IsNaN(singular.M22));
        Assert.True(float.IsNaN(singular.M33));
        Assert.True(float.IsNaN(singular.M44));
    }

    [Fact]
    public void InputBehaviorCorpus_MatchesXnaValueSemantics()
    {
        IReadOnlyList<string> first = InputBehaviorCorpus.Capture();
        IReadOnlyList<string> second = InputBehaviorCorpus.Capture();

        Assert.Equal(first, second);
        Assert.Equal(23, first.Count);
        Assert.Contains("keyboard.null.count=0", first);
        Assert.Contains("keyboard.pressed=65,90", first);
        Assert.Contains("keyboard.invalid=0,0", first);
        Assert.Contains("keyboard.hash=67108866", first);
        Assert.Contains("mouse.string={X:12 Y:-3 Buttons:Left Right XButton1 Wheel:120}", first);
        Assert.Contains("mouse.hash=-120", first);
        Assert.Contains("thumbs.clamp=3F800000,BF800000,3E800000,BF000000", first);
        Assert.Contains("triggers.clamp=00000000,3F800000", first);
        Assert.Contains("gamepad.null=none", first);
        Assert.Contains("gamepad.virtual=0,1,1,1,0,1", first);
        Assert.Contains("gamepad.filtered=1,0,0,0", first);
        Assert.Contains("gamepad.string={IsConnected:True}", first);
        Assert.Contains("buttons.string={Buttons:A Y Back}", first);
        Assert.Contains("buttons.hash=1", first);
        Assert.Contains("dpad.string={DPad:Up Right}", first);
        Assert.Contains("dpad.hash=2147483647", first);
        Assert.Contains("touch.previous.none=0,-1,0", first);
        Assert.Contains("touch.equals=1,0", first);
        Assert.Contains("touch.hash=2139095045", first);
        Assert.Contains("touch.string={Position:{X:1 Y:2}}", first);
        Assert.Contains("touch.collection.clone=5", first);
        Assert.Contains("touch.collection.contains=0", first);
        Assert.Contains("touch.collection.oob=ArgumentOutOfRangeException", first);
    }
}
