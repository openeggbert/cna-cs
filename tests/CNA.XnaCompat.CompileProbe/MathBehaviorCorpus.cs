using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace XnaCompatibilityCompileProbe;

/// <summary>
/// Deterministic, engine-neutral math observations. The same source compiles against XNA, CNA,
/// FNA, MonoGame, and Kni through this project's <c>CompatibilityTarget</c> switch, so snapshots
/// can be diffed without maintaining engine-specific test implementations.
/// </summary>
public static class MathBehaviorCorpus
{
    public static IReadOnlyList<string> Capture()
    {
        var observations = new List<string>();

        Add(observations, "v2.normalize.zero", Vector2.Normalize(Vector2.Zero));
        Add(observations, "v3.normalize.zero", Vector3.Normalize(Vector3.Zero));
        Add(observations, "v4.normalize.zero", Vector4.Normalize(Vector4.Zero));
        Add(observations, "vector.divide.scalar", new Vector4(
            (new Vector2(3f) / 7f).X,
            (new Vector3(7f) / 3f).X,
            (new Vector4(12345.67f) / 3f).X,
            (Matrix.Identity / 3f).M11));
        Add(observations, "q.normalize.zero", Quaternion.Normalize(default));
        Add(observations, "q.inverse.zero", Quaternion.Inverse(default));

        Quaternion yaw = Quaternion.CreateFromAxisAngle(Vector3.Up, 0.7f);
        Quaternion pitch = Quaternion.CreateFromAxisAngle(Vector3.Right, -0.4f);
        Add(observations, "q.multiply", yaw * pitch);
        Add(
            observations,
            "q.multiply.grouped",
            new Quaternion(45889.05859375f, -42412.4453125f, 96034.96875f, -76386.84375f) *
            new Quaternion(-16375.435546875f, 51428.1875f, -69603.09375f, -2207.3798828125f));
        Add(observations, "q.concatenate", Quaternion.Concatenate(yaw, pitch));
        Add(observations, "v3.qtransform", Vector3.Transform(new Vector3(1.25f, -2.5f, 3.75f), yaw * pitch));

        Matrix matrix = Matrix.CreateScale(2f, 3f, 4f)
            * Matrix.CreateRotationY(0.25f)
            * Matrix.CreateTranslation(5f, 6f, 7f);
        Add(observations, "v2.transform", Vector2.Transform(new Vector2(1.5f, -2f), matrix));
        Add(observations, "v3.transform", Vector3.Transform(new Vector3(1.5f, -2f, 0.25f), matrix));
        Add(observations, "v4.transform", Vector4.Transform(new Vector4(1.5f, -2f, 0.25f, 1f), matrix));
        Add(observations, "matrix.inverse.product", matrix * Matrix.Invert(matrix));
        Add(observations, "matrix.inverse.singular", Matrix.Invert(default));

        var viewport = new Viewport(11, 13, 640, 360)
        {
            MinDepth = 0.2f,
            MaxDepth = 0.9f,
        };
        Matrix viewportWorld = Matrix.CreateScale(1.5f, 0.75f, 2f) *
            Matrix.CreateRotationY(0.31f) *
            Matrix.CreateTranslation(2f, -1f, 0.5f);
        Matrix viewportView = Matrix.CreateLookAt(
            new Vector3(4f, 3f, 8f),
            new Vector3(0f, 0f, 0f),
            Vector3.Up);
        Matrix viewportProjection = Matrix.CreatePerspectiveFieldOfView(0.9f, 16f / 9f, 0.1f, 100f);
        Vector3 viewportProjected = viewport.Project(
            new Vector3(0.25f, -0.5f, 1.25f),
            viewportProjection,
            viewportView,
            viewportWorld);
        Add(observations, "viewport.project", viewportProjected);
        Add(observations, "viewport.unproject", viewport.Unproject(
            viewportProjected,
            viewportProjection,
            viewportView,
            viewportWorld));
        Add(observations, "viewport.unproject.singular", viewport.Unproject(
            new Vector3(100f, 50f, 0.5f),
            Matrix.Identity,
            Matrix.Identity,
            default));

        Color packed = new(0.5f, float.NaN, float.PositiveInfinity, float.NegativeInfinity);
        observations.Add("color.pack=" + packed.PackedValue.ToString("X8", CultureInfo.InvariantCulture));
        Color midpoint = Color.Lerp(new Color(0, 0, 0, 0), new Color(255, 255, 255, 255), 0.5f);
        observations.Add("color.lerp=" + midpoint.PackedValue.ToString("X8", CultureInfo.InvariantCulture));
        observations.Add("color.nonpremultiplied.extreme=" + Color.FromNonPremultiplied(
            int.MaxValue,
            int.MaxValue,
            int.MaxValue,
            int.MaxValue).PackedValue.ToString("X8", CultureInfo.InvariantCulture));

        Plane transformedPlane = Plane.Transform(
            new Plane(Vector3.Up, -2f),
            Matrix.CreateTranslation(0f, 5f, 0f));
        Add(observations, "plane.transform", new Vector4(transformedPlane.Normal, transformedPlane.D));

        var box = new BoundingBox(new Vector3(-1f), new Vector3(1f));
        observations.Add("box.contains.edge=" + ((int)box.Contains(new Vector3(1f, 0f, 0f))).ToString(CultureInfo.InvariantCulture));
        var nanBox = new BoundingBox(
            new Vector3(float.NaN, -1f, -1f),
            new Vector3(float.NaN, 1f, 1f));
        observations.Add(
            $"box.nan={((int)box.Contains(new Vector3(float.NaN, 0f, 0f))).ToString(CultureInfo.InvariantCulture)}," +
            Flag(box.Intersects(nanBox)));
        var sphere = new BoundingSphere(Vector3.Zero, 1f);
        observations.Add("sphere.contains.edge=" + ((int)sphere.Contains(Vector3.UnitX)).ToString(CultureInfo.InvariantCulture));
        BoundingSphere pointsSphere = BoundingSphere.CreateFromPoints(
        [
            new Vector3(-4f, 1f, 0f),
            new Vector3(6f, -2f, 3f),
            new Vector3(0f, 8f, -5f),
            new Vector3(2f, 0f, 9f),
        ]);
        Add(observations, "sphere.points", new Vector4(pointsSphere.Center, pointsSphere.Radius));
        observations.Add("ray.sphere=" + NullableBits(
            new Ray(new Vector3(-5f, 0.25f, 0f), Vector3.UnitX).Intersects(sphere)));

        var nanVector = new Vector2(float.NaN, 0f);
        Vector2 nanVectorCopy = nanVector;
        observations.Add($"v2.equals.nan={Flag(nanVector.Equals(nanVectorCopy))},{Flag(nanVector == nanVectorCopy)}");
        Matrix nanMatrix = Matrix.Identity;
        nanMatrix.M11 = float.NaN;
        Matrix nanMatrixCopy = nanMatrix;
        observations.Add($"matrix.equals.nan={Flag(nanMatrix.Equals(nanMatrixCopy))},{Flag(nanMatrix == nanMatrixCopy)}");
        observations.Add("v3.hash=" + new Vector3(1f, 2f, 3f).GetHashCode().ToString(CultureInfo.InvariantCulture));
        observations.Add("matrix.identity.hash=" + Matrix.Identity.GetHashCode().ToString(CultureInfo.InvariantCulture));
        observations.Add(
            $"integer.hash={new Point(1, 2).GetHashCode()},{new Rectangle(1, 2, 3, 4).GetHashCode()}");
        observations.Add("sphere.negative=" + ExceptionName(
            () => _ = new BoundingSphere(Vector3.Zero, -1f)));
        observations.Add("math.clamp.reversed=" + Bits(MathHelper.Clamp(0f, 2f, 1f)));
        observations.Add("math.wrap.large=" + Bits(MathHelper.WrapAngle(123456.789f)));
        observations.Add(
            $"math.splines={Bits(MathHelper.CatmullRom(-10f, -10f, -10f, -7f, 0.3f))}," +
            Bits(MathHelper.Hermite(-10f, -10f, -10f, -10f, 1.1f)));
        observations.Add("math.hermite.endpoint.nan=" + Flag(float.IsNaN(
            MathHelper.Hermite(1f, float.PositiveInfinity, 2f, 0f, 0f))));

        var tangentSphere = new BoundingSphere(new Vector3(2f, 0f, 0f), 1f);
        observations.Add("sphere.intersects.tangent=" + Flag(sphere.Intersects(tangentSphere)));

        var rayBox = new BoundingBox(new Vector3(-1f), new Vector3(1f));
        var nearParallelRay = new Ray(new Vector3(2f, 0f, 0f), new Vector3(-5e-7f, 0f, 0f));
        observations.Add("box.ray.nearparallel=" + NullableBits(nearParallelRay.Intersects(rayBox)));

        var nearParallelPlaneRay = new Ray(Vector3.Zero, new Vector3(5e-6f, 1f, 0f));
        observations.Add("ray.plane.nearparallel=" + NullableBits(nearParallelPlaneRay.Intersects(new Plane(Vector3.UnitX, -1f))));

        var justBehindPlaneRay = new Ray(new Vector3(5e-6f, 0f, 0f), Vector3.UnitX);
        var originPlane = new Plane(Vector3.UnitX, 0f);
        float? valueDistance = justBehindPlaneRay.Intersects(originPlane);
        justBehindPlaneRay.Intersects(ref originPlane, out float? refDistance);
        observations.Add($"ray.plane.overloads={NullableBits(valueDistance)},{NullableBits(refDistance)}");
        observations.Add("v3.transform.negative.length=" + ExceptionName(TransformWithNegativeLength));
        observations.Add("v3.transform.negative.index=" + ExceptionName(TransformWithNegativeIndex));
        Add(observations, "v3.min.nan", Vector3.Min(
            new Vector3(float.NaN, 1f, float.NaN),
            new Vector3(7f, float.NaN, float.NaN)));
        Add(observations, "v3.clamp.reversed", Vector3.Clamp(Vector3.Zero, new Vector3(2f), new Vector3(1f)));
        Add(observations, "q.slerp", Quaternion.Slerp(yaw, pitch, 0.37f));
        Add(observations, "q.axis.large", Quaternion.CreateFromAxisAngle(Vector3.Up, 123456.789f));
        Add(observations, "q.from.matrix", Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationY(0.7f)));

        Matrix largeRotation = Matrix.CreateRotationY(123456.789f);
        Add(observations, "matrix.rotation.large", new Vector2(largeRotation.M11, largeRotation.M31));
        Matrix infinitePerspective = Matrix.CreatePerspective(4f, 3f, 0.1f, float.PositiveInfinity);
        observations.Add(
            $"matrix.perspective.infinity={Bits(infinitePerspective.M33)},{Bits(infinitePerspective.M43)}");
        observations.Add("matrix.fov.invalid=" + ExceptionName(
            () => _ = Matrix.CreatePerspectiveFieldOfView(0f, 1f, 0.1f, 100f)));
        Matrix mirroredMatrix = Matrix.CreateScale(-2f, 3f, 4f)
            * Matrix.CreateRotationY(0.25f)
            * Matrix.CreateTranslation(5f, 6f, 7f);
        bool decomposed = mirroredMatrix.Decompose(
            out Vector3 mirroredScale,
            out Quaternion mirroredRotation,
            out Vector3 mirroredTranslation);
        observations.Add(
            $"matrix.decompose.mirror={Flag(decomposed)}," +
            $"{Bits(mirroredScale.X)},{Bits(mirroredScale.Y)},{Bits(mirroredScale.Z)}," +
            $"{Bits(mirroredRotation.X)},{Bits(mirroredRotation.Y)}," +
            $"{Bits(mirroredRotation.Z)},{Bits(mirroredRotation.W)}," +
            $"{Bits(mirroredTranslation.X)},{Bits(mirroredTranslation.Y)},{Bits(mirroredTranslation.Z)}");
        Matrix constrainedBillboard = Matrix.CreateConstrainedBillboard(
            new Vector3(0f, 10f, 0f),
            Vector3.Zero,
            new Vector3(0f, 2f, 0f),
            null,
            null);
        observations.Add(
            $"matrix.billboard.axis={Bits(constrainedBillboard.M11)}," +
            $"{Bits(constrainedBillboard.M22)},{Bits(constrainedBillboard.M33)}");
        Matrix zeroPlaneShadow = Matrix.CreateShadow(Vector3.Forward, new Plane(Vector3.Zero, 0f));
        observations.Add(
            $"matrix.shadow.zero.nan={Flag(float.IsNaN(zeroPlaneShadow.M11))}," +
            Flag(float.IsNaN(zeroPlaneShadow.M44)));
        Plane reflectionPlane = new(new Vector3(2f, 0f, 0f), 4f);
        Matrix.CreateReflection(ref reflectionPlane, out Matrix reflectionMatrix);
        observations.Add(
            $"matrix.reflection.ref={Bits(reflectionPlane.Normal.X)},{Bits(reflectionPlane.D)}," +
            $"{Bits(reflectionMatrix.M11)},{Bits(reflectionMatrix.M41)}");
        Add(observations, "matrix.lookat.degenerate", Matrix.CreateLookAt(Vector3.Zero, Vector3.Zero, Vector3.Up));
        observations.Add(CaptureMatrixTransformInfinity());
        observations.Add(
            $"negate.signedzero={Bits((-Vector4.Zero).X)},{Bits((-default(Quaternion)).X)}," +
            Bits((-default(Matrix)).M11));
        observations.Add("matrix.tostring=" + Matrix.Identity);
        Plane degeneratePlane = new(Vector3.Zero, Vector3.Zero, Vector3.Zero);
        Add(observations, "plane.points.degenerate", new Vector4(degeneratePlane.Normal, degeneratePlane.D));
        Plane nearUnitPlane = Plane.Normalize(new Plane(new Vector3(0.6f, 0.79999995f, 0f), 2f));
        Add(observations, "plane.normalize.nearunit", new Vector4(nearUnitPlane.Normal, nearUnitPlane.D));
        observations.Add("plane.box.coplanar=" + ((int)new Plane(Vector3.Zero, 0f).Intersects(box))
            .ToString(CultureInfo.InvariantCulture));

        var curveKey = new CurveKey(1f, 2f, 3f, 4f, CurveContinuity.Step);
        observations.Add("curve.key.hash=" + curveKey.GetHashCode().ToString(CultureInfo.InvariantCulture));
        var nanCurveKey = new CurveKey(float.NaN, 0f);
        var finiteCurveKey = new CurveKey(0f, 0f);
        observations.Add(
            $"curve.key.compare={nanCurveKey.CompareTo(finiteCurveKey)},{finiteCurveKey.CompareTo(nanCurveKey)}," +
            ExceptionName(() => _ = finiteCurveKey.CompareTo(null!)));

        var curveKeys = new CurveKeyCollection();
        curveKeys.Add(new CurveKey(0f, 1f));
        curveKeys.Add(new CurveKey(5e-8f, 2f));
        var replacementKey = new CurveKey(1e-7f, 3f);
        curveKeys[0] = replacementKey;
        observations.Add($"curve.collection.reposition={Bits(curveKeys[0].Value)},{Bits(curveKeys[1].Value)}");
        observations.Add(
            $"curve.collection.oob={ExceptionName(() => curveKeys[-1] = replacementKey)}," +
            ExceptionName(() => curveKeys[curveKeys.Count] = replacementKey));

        var tangentCurve = new Curve();
        tangentCurve.Keys.Add(new CurveKey(0f, 0f));
        tangentCurve.Keys.Add(new CurveKey(1f, 5e-9f));
        tangentCurve.Keys.Add(new CurveKey(2f, 1e-8f));
        tangentCurve.ComputeTangent(1, CurveTangent.Smooth);
        observations.Add(
            $"curve.tangent.epsilon={Bits(tangentCurve.Keys[1].TangentIn)}," +
            Bits(tangentCurve.Keys[1].TangentOut));

        var loopCurve = new Curve { PreLoop = CurveLoopType.Cycle };
        loopCurve.Keys.Add(new CurveKey(0f, 10f, 0f, 0f, CurveContinuity.Step));
        loopCurve.Keys.Add(new CurveKey(1f, 20f, 0f, 0f, CurveContinuity.Step));
        observations.Add("curve.cycle.preboundary=" + Bits(loopCurve.Evaluate(-1f)));
        observations.Add("curve.step.nan=" + Bits(loopCurve.Evaluate(float.NaN)));

        var alphaMidpoint = new Alpha8(0.5f / 255f);
        var oneBitAlphaMidpoint = new Bgra5551(0f, 0f, 0f, 0.5f);
        observations.Add(
            $"packed.unorm.midpoint={alphaMidpoint.PackedValue:X2}," +
            $"{oneBitAlphaMidpoint.PackedValue:X4}");
        observations.Add("packed.unsigned.rounding=" +
            new Byte4(0.5f, 1.5f, 2.5f, 3.5f).PackedValue.ToString("X8", CultureInfo.InvariantCulture));
        observations.Add("packed.snorm.rounding=" +
            new NormalizedByte2(0.5f / 127f, -0.5f / 127f).PackedValue
                .ToString("X4", CultureInfo.InvariantCulture));
        var minimumSNorm = new NormalizedByte2 { PackedValue = 0x8080 };
        Add(observations, "packed.snorm.minimum", minimumSNorm.ToVector2());
        observations.Add("packed.signed.rounding=" +
            new Short2(0.5f, 1.5f).PackedValue.ToString("X8", CultureInfo.InvariantCulture));
        var exponent31Half = new HalfSingle { PackedValue = 0x7C00 };
        observations.Add(
            $"packed.half.saturation={new HalfSingle(float.PositiveInfinity).PackedValue:X4}," +
            $"{new HalfSingle(FloatFromBits(0x7FC00000)).PackedValue:X4}," +
            Bits(exponent31Half.ToSingle()));
        observations.Add(
            $"packed.tostring={new Alpha8 { PackedValue = 0x0A }}," +
            $"{new Bgra5551 { PackedValue = 0x000A }}," +
            $"{new Byte4 { PackedValue = 0x0000000A }}");

        Matrix frustumProjection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4,
            4f / 3f,
            1f,
            10f);
        Matrix frustumMatrix = Matrix.CreateLookAt(
            new Vector3(0f, 0f, 5f),
            Vector3.Zero,
            Vector3.Up) * frustumProjection;
        var frustum = new BoundingFrustum(frustumMatrix);
        Add(observations, "frustum.near", new Vector4(frustum.Near.Normal, frustum.Near.D));
        Add(observations, "frustum.top", new Vector4(frustum.Top.Normal, frustum.Top.D));
        Vector3[] frustumCorners = frustum.GetCorners();
        Add(observations, "frustum.corner0", frustumCorners[0]);
        Add(observations, "frustum.corner6", frustumCorners[6]);
        observations.Add(
            $"frustum.contains={((int)frustum.Contains(Vector3.Zero))}," +
            $"{((int)frustum.Contains(new Vector3(0f, 0f, 6f)))}," +
            $"{((int)frustum.Contains(new BoundingBox(new Vector3(-0.5f), new Vector3(0.5f))))}," +
            $"{((int)frustum.Contains(new BoundingSphere(Vector3.Zero, 0.5f)))}");
        var distantFrustum = new BoundingFrustum(
            Matrix.CreateLookAt(
                new Vector3(100f, 0f, 5f),
                new Vector3(100f, 0f, 0f),
                Vector3.Up) * frustumProjection);
        observations.Add(
            $"frustum.gjk={Flag(frustum.Intersects(new BoundingBox(new Vector3(-0.5f), new Vector3(0.5f))))}," +
            $"{Flag(frustum.Intersects(new BoundingBox(new Vector3(100f), new Vector3(101f))))}," +
            $"{Flag(frustum.Intersects(new BoundingSphere(Vector3.Zero, 0.5f)))}," +
            $"{Flag(frustum.Intersects(new BoundingSphere(new Vector3(100f), 0.5f)))}," +
            Flag(frustum.Intersects(distantFrustum)));
        observations.Add("frustum.ray=" + NullableBits(
            frustum.Intersects(new Ray(new Vector3(0f, 0f, 20f), Vector3.Forward))));

        return observations;
    }

    private static void Add(ICollection<string> output, string name, Vector2 value) =>
        output.Add($"{name}={Bits(value.X)},{Bits(value.Y)}");

    private static void Add(ICollection<string> output, string name, Vector3 value) =>
        output.Add($"{name}={Bits(value.X)},{Bits(value.Y)},{Bits(value.Z)}");

    private static void Add(ICollection<string> output, string name, Vector4 value) =>
        output.Add($"{name}={Bits(value.X)},{Bits(value.Y)},{Bits(value.Z)},{Bits(value.W)}");

    private static void Add(ICollection<string> output, string name, Quaternion value) =>
        output.Add($"{name}={Bits(value.X)},{Bits(value.Y)},{Bits(value.Z)},{Bits(value.W)}");

    private static void Add(ICollection<string> output, string name, Matrix value) => output.Add(
        $"{name}={Bits(value.M11)},{Bits(value.M12)},{Bits(value.M13)},{Bits(value.M14)}," +
        $"{Bits(value.M21)},{Bits(value.M22)},{Bits(value.M23)},{Bits(value.M24)}," +
        $"{Bits(value.M31)},{Bits(value.M32)},{Bits(value.M33)},{Bits(value.M34)}," +
        $"{Bits(value.M41)},{Bits(value.M42)},{Bits(value.M43)},{Bits(value.M44)}");

    private static string Bits(float value) =>
        unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(value), 0))
            .ToString("X8", CultureInfo.InvariantCulture);

    private static float FloatFromBits(uint bits) =>
        BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);

    private static string NullableBits(float? value) => value.HasValue ? Bits(value.Value) : "none";

    private static string CaptureMatrixTransformInfinity()
    {
        // MonoGame 3.8 omits this XNA method. Reflection keeps the shared corpus source
        // buildable there while still recording the missing API instead of silently replacing
        // the operation with matrix multiplication, whose IEEE behavior is observably different.
        System.Reflection.MethodInfo? transform = typeof(Matrix).GetMethod(
            "Transform",
            new[] { typeof(Matrix), typeof(Quaternion) });
        if (transform is null)
        {
            return "matrix.transform.infinity=missing";
        }

        Matrix value = Matrix.Identity;
        value.M14 = float.PositiveInfinity;
        Matrix result = (Matrix)transform.Invoke(null, new object[] { value, Quaternion.Identity })!;
        return $"matrix.transform.infinity={Bits(result.M11)},{Bits(result.M14)}," +
            Flag(float.IsNaN(result.M11));
    }

    private static int Flag(bool value) => value ? 1 : 0;

    private static void TransformWithNegativeLength()
    {
        Matrix matrix = Matrix.Identity;
        Vector3.Transform([Vector3.Zero], 0, ref matrix, [Vector3.Zero], 0, -1);
    }

    private static void TransformWithNegativeIndex()
    {
        Matrix matrix = Matrix.Identity;
        Vector3.Transform([Vector3.Zero], -1, ref matrix, [Vector3.Zero], 0, 1);
    }

    private static string ExceptionName(Action action)
    {
        try
        {
            action();
            return "none";
        }
        catch (Exception exception)
        {
            return exception.GetType().Name;
        }
    }
}
