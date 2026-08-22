namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>Matrix</c>. Fields are duplicated (real XNA code reads/writes
/// <c>matrix.M11</c> etc. directly), but every formula -- construction, multiplication,
/// inversion, transpose -- delegates to <see cref="CNA.Matrix"/> via internal
/// conversion methods, so there is exactly one implementation of the actual math. See
/// docs/architecture.md and Matrix.cs in CNA for what is and isn't implemented.
/// </summary>
[System.ComponentModel.TypeConverter(typeof(Design.MatrixConverter))]
public struct Matrix : IEquatable<Matrix>
{
    private struct CanonicalBasis
    {
        public Vector3 Row0;
        public Vector3 Row1;
        public Vector3 Row2;
    }

    private unsafe struct VectorBasis
    {
        public Vector3* Element0;
        public Vector3* Element1;
        public Vector3* Element2;
    }

    public float M11, M12, M13, M14;
    public float M21, M22, M23, M24;
    public float M31, M32, M33, M34;
    public float M41, M42, M43, M44;

    public Matrix(
        float m11, float m12, float m13, float m14,
        float m21, float m22, float m23, float m24,
        float m31, float m32, float m33, float m34,
        float m41, float m42, float m43, float m44)
    {
        M11 = m11; M12 = m12; M13 = m13; M14 = m14;
        M21 = m21; M22 = m22; M23 = m23; M24 = m24;
        M31 = m31; M32 = m32; M33 = m33; M34 = m34;
        M41 = m41; M42 = m42; M43 = m43; M44 = m44;
    }

    public static Matrix Identity => FromFramework(CNA.Matrix.Identity);

    public Vector3 Translation
    {
        readonly get => new(M41, M42, M43);
        set { M41 = value.X; M42 = value.Y; M43 = value.Z; }
    }

    public Vector3 Right
    {
        readonly get => new(M11, M12, M13);
        set { M11 = value.X; M12 = value.Y; M13 = value.Z; }
    }

    public Vector3 Left
    {
        readonly get => new(-M11, -M12, -M13);
        set { M11 = -value.X; M12 = -value.Y; M13 = -value.Z; }
    }

    public Vector3 Up
    {
        readonly get => new(M21, M22, M23);
        set { M21 = value.X; M22 = value.Y; M23 = value.Z; }
    }

    public Vector3 Down
    {
        readonly get => new(-M21, -M22, -M23);
        set { M21 = -value.X; M22 = -value.Y; M23 = -value.Z; }
    }

    public Vector3 Forward
    {
        readonly get => new(-M31, -M32, -M33);
        set { M31 = -value.X; M32 = -value.Y; M33 = -value.Z; }
    }

    public Vector3 Backward
    {
        readonly get => new(M31, M32, M33);
        set { M31 = value.X; M32 = value.Y; M33 = value.Z; }
    }

    public static Matrix CreateTranslation(Vector3 position) =>
        FromFramework(CNA.Matrix.CreateTranslation(position.ToFramework()));

    public static Matrix CreateTranslation(float xPosition, float yPosition, float zPosition) =>
        FromFramework(CNA.Matrix.CreateTranslation(xPosition, yPosition, zPosition));

    public static Matrix CreateScale(float scale) => FromFramework(CNA.Matrix.CreateScale(scale));

    public static Matrix CreateScale(Vector3 scales) =>
        FromFramework(CNA.Matrix.CreateScale(scales.ToFramework()));

    public static Matrix CreateScale(float xScale, float yScale, float zScale) =>
        FromFramework(CNA.Matrix.CreateScale(xScale, yScale, zScale));

    public static Matrix CreateRotationX(float radians)
    {
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        return new Matrix(
            1f, 0f, 0f, 0f,
            0f, cos, sin, 0f,
            0f, -sin, cos, 0f,
            0f, 0f, 0f, 1f);
    }

    public static Matrix CreateRotationY(float radians)
    {
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        return new Matrix(
            cos, 0f, -sin, 0f,
            0f, 1f, 0f, 0f,
            sin, 0f, cos, 0f,
            0f, 0f, 0f, 1f);
    }

    public static Matrix CreateRotationZ(float radians)
    {
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        return new Matrix(
            cos, sin, 0f, 0f,
            -sin, cos, 0f, 0f,
            0f, 0f, 1f, 0f,
            0f, 0f, 0f, 1f);
    }

    public static Matrix CreateFromAxisAngle(Vector3 axis, float angle)
    {
        float x = axis.X;
        float y = axis.Y;
        float z = axis.Z;
        float sin = (float)Math.Sin(angle);
        float cos = (float)Math.Cos(angle);
        float xx = x * x;
        float yy = y * y;
        float zz = z * z;
        float xy = x * y;
        float xz = x * z;
        float yz = y * z;

        return new Matrix(
            xx + (cos * (1f - xx)), xy - (cos * xy) + (sin * z), xz - (cos * xz) - (sin * y), 0f,
            xy - (cos * xy) - (sin * z), yy + (cos * (1f - yy)), yz - (cos * yz) + (sin * x), 0f,
            xz - (cos * xz) + (sin * y), yz - (cos * yz) - (sin * x), zz + (cos * (1f - zz)), 0f,
            0f, 0f, 0f, 1f);
    }

    public static Matrix CreateFromQuaternion(Quaternion quaternion)
    {
        float xx = quaternion.X * quaternion.X;
        float yy = quaternion.Y * quaternion.Y;
        float zz = quaternion.Z * quaternion.Z;
        float xy = quaternion.X * quaternion.Y;
        float zw = quaternion.Z * quaternion.W;
        float zx = quaternion.Z * quaternion.X;
        float yw = quaternion.Y * quaternion.W;
        float yz = quaternion.Y * quaternion.Z;
        float xw = quaternion.X * quaternion.W;
        return new Matrix(
            1f - (2f * (yy + zz)), 2f * (xy + zw), 2f * (zx - yw), 0f,
            2f * (xy - zw), 1f - (2f * (zz + xx)), 2f * (yz + xw), 0f,
            2f * (zx + yw), 2f * (yz - xw), 1f - (2f * (yy + xx)), 0f,
            0f, 0f, 0f, 1f);
    }

    public static Matrix CreateFromYawPitchRoll(float yaw, float pitch, float roll) =>
        CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll));

    public static Matrix CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
    {
        Vector3 backward = Vector3.Normalize(cameraPosition - cameraTarget);
        Vector3 right = Vector3.Normalize(Vector3.Cross(cameraUpVector, backward));
        Vector3 up = Vector3.Cross(backward, right);
        return new Matrix(
            right.X, up.X, backward.X, 0f,
            right.Y, up.Y, backward.Y, 0f,
            right.Z, up.Z, backward.Z, 0f,
            -Vector3.Dot(right, cameraPosition),
            -Vector3.Dot(up, cameraPosition),
            -Vector3.Dot(backward, cameraPosition),
            1f);
    }

    public static Matrix CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
    {
        Vector3 backward = Vector3.Normalize(-forward);
        Vector3 right = Vector3.Normalize(Vector3.Cross(up, backward));
        Vector3 correctedUp = Vector3.Cross(backward, right);
        return new Matrix(
            right.X, right.Y, right.Z, 0f,
            correctedUp.X, correctedUp.Y, correctedUp.Z, 0f,
            backward.X, backward.Y, backward.Z, 0f,
            position.X, position.Y, position.Z, 1f);
    }

    public static Matrix CreatePerspectiveFieldOfView(
        float fieldOfView,
        float aspectRatio,
        float nearPlaneDistance,
        float farPlaneDistance)
    {
        if (fieldOfView <= 0f || fieldOfView >= MathHelper.Pi)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldOfView));
        }

        ValidatePerspectivePlanes(nearPlaneDistance, farPlaneDistance);
        float yScale = 1f / (float)Math.Tan(fieldOfView * 0.5f);
        float xScale = yScale / aspectRatio;
        float depth = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
        return new Matrix(
            xScale, 0f, 0f, 0f,
            0f, yScale, 0f, 0f,
            0f, 0f, depth, -1f,
            0f, 0f,
            (nearPlaneDistance * farPlaneDistance) / (nearPlaneDistance - farPlaneDistance),
            0f);
    }

    public static Matrix CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane) =>
        FromFramework(CNA.Matrix.CreateOrthographic(width, height, zNearPlane, zFarPlane));

    public static Matrix CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane) =>
        FromFramework(CNA.Matrix.CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane));

    public static Matrix CreatePerspective(
        float width,
        float height,
        float nearPlaneDistance,
        float farPlaneDistance)
    {
        ValidatePerspectivePlanes(nearPlaneDistance, farPlaneDistance);
        return new Matrix(
            (2f * nearPlaneDistance) / width, 0f, 0f, 0f,
            0f, (2f * nearPlaneDistance) / height, 0f, 0f,
            0f, 0f, farPlaneDistance / (nearPlaneDistance - farPlaneDistance), -1f,
            0f, 0f,
            (nearPlaneDistance * farPlaneDistance) / (nearPlaneDistance - farPlaneDistance),
            0f);
    }

    public static Matrix CreatePerspectiveOffCenter(
        float left,
        float right,
        float bottom,
        float top,
        float nearPlaneDistance,
        float farPlaneDistance)
    {
        ValidatePerspectivePlanes(nearPlaneDistance, farPlaneDistance);
        return new Matrix(
            (2f * nearPlaneDistance) / (right - left), 0f, 0f, 0f,
            0f, (2f * nearPlaneDistance) / (top - bottom), 0f, 0f,
            (left + right) / (right - left),
            (top + bottom) / (top - bottom),
            farPlaneDistance / (nearPlaneDistance - farPlaneDistance),
            -1f,
            0f, 0f,
            (nearPlaneDistance * farPlaneDistance) / (nearPlaneDistance - farPlaneDistance),
            0f);
    }

    public static Matrix CreateBillboard(
        Vector3 objectPosition,
        Vector3 cameraPosition,
        Vector3 cameraUpVector,
        Vector3? cameraForwardVector)
    {
        Vector3 facing = objectPosition - cameraPosition;
        float lengthSquared = facing.LengthSquared();
        if (lengthSquared < 0.0001f)
        {
            facing = cameraForwardVector.HasValue ? -cameraForwardVector.Value : Vector3.Forward;
        }
        else
        {
            facing *= 1f / (float)Math.Sqrt(lengthSquared);
        }

        Vector3 right = Vector3.Cross(cameraUpVector, facing);
        right.Normalize();
        Vector3 up = Vector3.Cross(facing, right);
        return new Matrix(
            right.X, right.Y, right.Z, 0f,
            up.X, up.Y, up.Z, 0f,
            facing.X, facing.Y, facing.Z, 0f,
            objectPosition.X, objectPosition.Y, objectPosition.Z, 1f);
    }

    public static Matrix CreateConstrainedBillboard(
        Vector3 objectPosition,
        Vector3 cameraPosition,
        Vector3 rotateAxis,
        Vector3? cameraForwardVector,
        Vector3? objectForwardVector)
    {
        Vector3 facing = objectPosition - cameraPosition;
        float lengthSquared = facing.LengthSquared();
        if (lengthSquared < 0.0001f)
        {
            facing = cameraForwardVector.HasValue ? -cameraForwardVector.Value : Vector3.Forward;
        }
        else
        {
            facing *= 1f / (float)Math.Sqrt(lengthSquared);
        }

        Vector3 up = rotateAxis;
        float alignment = Vector3.Dot(rotateAxis, facing);
        Vector3 forward;
        Vector3 right;
        if (Math.Abs(alignment) > 0.99825466f)
        {
            if (objectForwardVector.HasValue)
            {
                forward = objectForwardVector.Value;
                alignment = Vector3.Dot(rotateAxis, forward);
                if (Math.Abs(alignment) > 0.99825466f)
                {
                    alignment = (rotateAxis.X * Vector3.Forward.X) +
                        (rotateAxis.Y * Vector3.Forward.Y) +
                        (rotateAxis.Z * Vector3.Forward.Z);
                    forward = Math.Abs(alignment) > 0.99825466f ? Vector3.Right : Vector3.Forward;
                }
            }
            else
            {
                alignment = (rotateAxis.X * Vector3.Forward.X) +
                    (rotateAxis.Y * Vector3.Forward.Y) +
                    (rotateAxis.Z * Vector3.Forward.Z);
                forward = Math.Abs(alignment) > 0.99825466f ? Vector3.Right : Vector3.Forward;
            }

            right = Vector3.Cross(rotateAxis, forward);
            right.Normalize();
            forward = Vector3.Cross(right, rotateAxis);
            forward.Normalize();
        }
        else
        {
            right = Vector3.Cross(rotateAxis, facing);
            right.Normalize();
            forward = Vector3.Cross(right, up);
            forward.Normalize();
        }

        return new Matrix(
            right.X, right.Y, right.Z, 0f,
            up.X, up.Y, up.Z, 0f,
            forward.X, forward.Y, forward.Z, 0f,
            objectPosition.X, objectPosition.Y, objectPosition.Z, 1f);
    }

    public static Matrix CreateShadow(Vector3 lightDirection, Plane plane)
    {
        Plane normalized = Plane.Normalize(plane);
        float dot = (normalized.Normal.X * lightDirection.X) +
            (normalized.Normal.Y * lightDirection.Y) +
            (normalized.Normal.Z * lightDirection.Z);
        float x = -normalized.Normal.X;
        float y = -normalized.Normal.Y;
        float z = -normalized.Normal.Z;
        float d = -normalized.D;
        return new Matrix(
            (x * lightDirection.X) + dot, x * lightDirection.Y, x * lightDirection.Z, 0f,
            y * lightDirection.X, (y * lightDirection.Y) + dot, y * lightDirection.Z, 0f,
            z * lightDirection.X, z * lightDirection.Y, (z * lightDirection.Z) + dot, 0f,
            d * lightDirection.X, d * lightDirection.Y, d * lightDirection.Z, dot);
    }

    public static Matrix CreateReflection(Plane value)
    {
        value.Normalize();
        return CreateReflectionFromNormalized(value);
    }

    private static Matrix CreateReflectionFromNormalized(Plane value)
    {
        float x = value.Normal.X;
        float y = value.Normal.Y;
        float z = value.Normal.Z;
        float doubledX = -2f * x;
        float doubledY = -2f * y;
        float doubledZ = -2f * z;
        return new Matrix(
            (doubledX * x) + 1f, doubledY * x, doubledZ * x, 0f,
            doubledX * y, (doubledY * y) + 1f, doubledZ * y, 0f,
            doubledX * z, doubledY * z, (doubledZ * z) + 1f, 0f,
            doubledX * value.D, doubledY * value.D, doubledZ * value.D, 1f);
    }

    public readonly unsafe bool Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation)
    {
        bool result = true;
        scale = default;
        translation = default;
        fixed (float* scaleValues = &scale.X)
        {
            VectorBasis vectorBasis = default;
            Vector3** basis = (Vector3**)(&vectorBasis);
            Matrix rotationMatrix = Identity;
            CanonicalBasis canonicalBasis = default;
            Vector3* canonical = &canonicalBasis.Row0;
            canonicalBasis.Row0 = new Vector3(1f, 0f, 0f);
            canonicalBasis.Row1 = new Vector3(0f, 1f, 0f);
            canonicalBasis.Row2 = new Vector3(0f, 0f, 1f);

            translation.X = M41;
            translation.Y = M42;
            translation.Z = M43;
            basis[0] = (Vector3*)(&rotationMatrix.M11);
            basis[1] = (Vector3*)(&rotationMatrix.M21);
            basis[2] = (Vector3*)(&rotationMatrix.M31);
            *basis[0] = new Vector3(M11, M12, M13);
            *basis[1] = new Vector3(M21, M22, M23);
            *basis[2] = new Vector3(M31, M32, M33);
            scale.X = basis[0]->Length();
            scale.Y = basis[1]->Length();
            scale.Z = basis[2]->Length();

            float scaleX = scaleValues[0];
            float scaleY = scaleValues[1];
            float scaleZ = scaleValues[2];
            uint largest;
            uint middle;
            uint smallest;
            if (scaleX < scaleY)
            {
                if (scaleY < scaleZ)
                {
                    largest = 2;
                    middle = 1;
                    smallest = 0;
                }
                else
                {
                    largest = 1;
                    if (scaleX < scaleZ)
                    {
                        middle = 2;
                        smallest = 0;
                    }
                    else
                    {
                        middle = 0;
                        smallest = 2;
                    }
                }
            }
            else if (scaleX < scaleZ)
            {
                largest = 2;
                middle = 0;
                smallest = 1;
            }
            else
            {
                largest = 0;
                if (scaleY < scaleZ)
                {
                    middle = 2;
                    smallest = 1;
                }
                else
                {
                    middle = 1;
                    smallest = 2;
                }
            }

            if (scaleValues[largest] < 0.0001f)
            {
                *basis[largest] = canonical[largest];
            }

            basis[largest]->Normalize();
            if (scaleValues[middle] < 0.0001f)
            {
                float absX = Math.Abs(basis[largest]->X);
                float absY = Math.Abs(basis[largest]->Y);
                float absZ = Math.Abs(basis[largest]->Z);
                uint leastAligned = absX < absY
                    ? (absY < absZ ? 0u : (absX < absZ ? 0u : 2u))
                    : (absX < absZ ? 1u : (absY < absZ ? 1u : 2u));
                Vector3.Cross(ref *basis[middle], ref *basis[largest], out canonical[leastAligned]);
            }

            basis[middle]->Normalize();
            if (scaleValues[smallest] < 0.0001f)
            {
                Vector3.Cross(ref *basis[smallest], ref *basis[largest], out *basis[middle]);
            }

            basis[smallest]->Normalize();
            float determinant = rotationMatrix.Determinant();
            if (determinant < 0f)
            {
                scaleValues[largest] = -scaleValues[largest];
                *basis[largest] = -*basis[largest];
                determinant = -determinant;
            }

            determinant -= 1f;
            determinant *= determinant;
            if (0.0001f < determinant)
            {
                rotation = Quaternion.Identity;
                result = false;
            }
            else
            {
                Quaternion.CreateFromRotationMatrix(ref rotationMatrix, out rotation);
            }
        }

        return result;
    }

    public readonly float Determinant()
    {
        float subFactor1 = (M33 * M44) - (M34 * M43);
        float subFactor2 = (M32 * M44) - (M34 * M42);
        float subFactor3 = (M32 * M43) - (M33 * M42);
        float subFactor4 = (M31 * M44) - (M34 * M41);
        float subFactor5 = (M31 * M43) - (M33 * M41);
        float subFactor6 = (M31 * M42) - (M32 * M41);

        return (M11 * (((M22 * subFactor1) - (M23 * subFactor2)) + (M24 * subFactor3)))
            - (M12 * (((M21 * subFactor1) - (M23 * subFactor4)) + (M24 * subFactor5)))
            + (M13 * (((M21 * subFactor2) - (M22 * subFactor4)) + (M24 * subFactor6)))
            - (M14 * (((M21 * subFactor3) - (M22 * subFactor5)) + (M23 * subFactor6)));
    }

    public static Matrix Transpose(Matrix matrix) =>
        FromFramework(CNA.Matrix.Transpose(matrix.ToFramework()));

    public static Matrix Invert(Matrix matrix)
    {
        Invert(ref matrix, out Matrix result);
        return result;
    }

    public static Matrix Add(Matrix matrix1, Matrix matrix2) => matrix1 + matrix2;

    public static void Add(ref Matrix matrix1, ref Matrix matrix2, out Matrix result) => result = matrix1 + matrix2;

    public static void CreateBillboard(
        ref Vector3 objectPosition,
        ref Vector3 cameraPosition,
        ref Vector3 cameraUpVector,
        Vector3? cameraForwardVector,
        out Matrix result) =>
        result = CreateBillboard(objectPosition, cameraPosition, cameraUpVector, cameraForwardVector);

    public static void CreateConstrainedBillboard(
        ref Vector3 objectPosition,
        ref Vector3 cameraPosition,
        ref Vector3 rotateAxis,
        Vector3? cameraForwardVector,
        Vector3? objectForwardVector,
        out Matrix result) => result = CreateConstrainedBillboard(
            objectPosition, cameraPosition, rotateAxis, cameraForwardVector, objectForwardVector);

    public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Matrix result) =>
        result = CreateFromAxisAngle(axis, angle);

    public static void CreateFromQuaternion(ref Quaternion quaternion, out Matrix result) =>
        result = CreateFromQuaternion(quaternion);

    public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Matrix result) =>
        result = CreateFromYawPitchRoll(yaw, pitch, roll);

    public static void CreateLookAt(
        ref Vector3 cameraPosition,
        ref Vector3 cameraTarget,
        ref Vector3 cameraUpVector,
        out Matrix result) => result = CreateLookAt(cameraPosition, cameraTarget, cameraUpVector);

    public static void CreateOrthographic(
        float width,
        float height,
        float zNearPlane,
        float zFarPlane,
        out Matrix result) => result = CreateOrthographic(width, height, zNearPlane, zFarPlane);

    public static void CreateOrthographicOffCenter(
        float left,
        float right,
        float bottom,
        float top,
        float zNearPlane,
        float zFarPlane,
        out Matrix result) => result = CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane);

    public static void CreatePerspective(
        float width,
        float height,
        float nearPlaneDistance,
        float farPlaneDistance,
        out Matrix result) => result = CreatePerspective(width, height, nearPlaneDistance, farPlaneDistance);

    public static void CreatePerspectiveFieldOfView(
        float fieldOfView,
        float aspectRatio,
        float nearPlaneDistance,
        float farPlaneDistance,
        out Matrix result) => result = CreatePerspectiveFieldOfView(
            fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance);

    public static void CreatePerspectiveOffCenter(
        float left,
        float right,
        float bottom,
        float top,
        float nearPlaneDistance,
        float farPlaneDistance,
        out Matrix result) => result = CreatePerspectiveOffCenter(
            left, right, bottom, top, nearPlaneDistance, farPlaneDistance);

    public static void CreateReflection(ref Plane value, out Matrix result)
    {
        Plane normalized = Plane.Normalize(value);
        // XNA 4.0 observably normalizes the ref argument as well as the copy used for the result.
        value.Normalize();
        result = CreateReflectionFromNormalized(normalized);
    }

    public static void CreateRotationX(float radians, out Matrix result) => result = CreateRotationX(radians);

    public static void CreateRotationY(float radians, out Matrix result) => result = CreateRotationY(radians);

    public static void CreateRotationZ(float radians, out Matrix result) => result = CreateRotationZ(radians);

    public static void CreateScale(float scale, out Matrix result) => result = CreateScale(scale);

    public static void CreateScale(float xScale, float yScale, float zScale, out Matrix result) =>
        result = CreateScale(xScale, yScale, zScale);

    public static void CreateScale(ref Vector3 scales, out Matrix result) => result = CreateScale(scales);

    public static void CreateShadow(ref Vector3 lightDirection, ref Plane plane, out Matrix result) =>
        result = CreateShadow(lightDirection, plane);

    public static void CreateTranslation(
        float xPosition,
        float yPosition,
        float zPosition,
        out Matrix result) => result = CreateTranslation(xPosition, yPosition, zPosition);

    public static void CreateTranslation(ref Vector3 position, out Matrix result) =>
        result = CreateTranslation(position);

    public static void CreateWorld(
        ref Vector3 position,
        ref Vector3 forward,
        ref Vector3 up,
        out Matrix result) => result = CreateWorld(position, forward, up);

    public static Matrix Divide(Matrix matrix1, Matrix matrix2) => matrix1 / matrix2;

    public static Matrix Divide(Matrix matrix1, float divider) => matrix1 / divider;

    public static void Divide(ref Matrix matrix1, float divider, out Matrix result) => result = matrix1 / divider;

    public static void Divide(ref Matrix matrix1, ref Matrix matrix2, out Matrix result) => result = matrix1 / matrix2;

    public static void Invert(ref Matrix matrix, out Matrix result)
    {
        // XNA uses a fixed Laplace/adjugate expansion and returns NaN components for a singular
        // matrix. Keep its operation ordering here instead of delegating to CNA.Matrix's
        // deliberately different pivoting implementation, which throws for singular input.
        float num1 = matrix.M11;
        float num2 = matrix.M12;
        float num3 = matrix.M13;
        float num4 = matrix.M14;
        float num5 = matrix.M21;
        float num6 = matrix.M22;
        float num7 = matrix.M23;
        float num8 = matrix.M24;
        float num9 = matrix.M31;
        float num10 = matrix.M32;
        float num11 = matrix.M33;
        float num12 = matrix.M34;
        float num13 = matrix.M41;
        float num14 = matrix.M42;
        float num15 = matrix.M43;
        float num16 = matrix.M44;
        float num17 = (num11 * num16) - (num12 * num15);
        float num18 = (num10 * num16) - (num12 * num14);
        float num19 = (num10 * num15) - (num11 * num14);
        float num20 = (num9 * num16) - (num12 * num13);
        float num21 = (num9 * num15) - (num11 * num13);
        float num22 = (num9 * num14) - (num10 * num13);
        float num23 = (num6 * num17) - (num7 * num18) + (num8 * num19);
        float num24 = -((num5 * num17) - (num7 * num20) + (num8 * num21));
        float num25 = (num5 * num18) - (num6 * num20) + (num8 * num22);
        float num26 = -((num5 * num19) - (num6 * num21) + (num7 * num22));
        float num27 = 1f / ((num1 * num23) + (num2 * num24) + (num3 * num25) + (num4 * num26));

        result.M11 = num23 * num27;
        result.M21 = num24 * num27;
        result.M31 = num25 * num27;
        result.M41 = num26 * num27;
        result.M12 = -((num2 * num17) - (num3 * num18) + (num4 * num19)) * num27;
        result.M22 = ((num1 * num17) - (num3 * num20) + (num4 * num21)) * num27;
        result.M32 = -((num1 * num18) - (num2 * num20) + (num4 * num22)) * num27;
        result.M42 = ((num1 * num19) - (num2 * num21) + (num3 * num22)) * num27;

        float num28 = (num7 * num16) - (num8 * num15);
        float num29 = (num6 * num16) - (num8 * num14);
        float num30 = (num6 * num15) - (num7 * num14);
        float num31 = (num5 * num16) - (num8 * num13);
        float num32 = (num5 * num15) - (num7 * num13);
        float num33 = (num5 * num14) - (num6 * num13);
        result.M13 = ((num2 * num28) - (num3 * num29) + (num4 * num30)) * num27;
        result.M23 = -((num1 * num28) - (num3 * num31) + (num4 * num32)) * num27;
        result.M33 = ((num1 * num29) - (num2 * num31) + (num4 * num33)) * num27;
        result.M43 = -((num1 * num30) - (num2 * num32) + (num3 * num33)) * num27;

        float num34 = (num7 * num12) - (num8 * num11);
        float num35 = (num6 * num12) - (num8 * num10);
        float num36 = (num6 * num11) - (num7 * num10);
        float num37 = (num5 * num12) - (num8 * num9);
        float num38 = (num5 * num11) - (num7 * num9);
        float num39 = (num5 * num10) - (num6 * num9);
        result.M14 = -((num2 * num34) - (num3 * num35) + (num4 * num36)) * num27;
        result.M24 = ((num1 * num34) - (num3 * num37) + (num4 * num38)) * num27;
        result.M34 = -((num1 * num35) - (num2 * num37) + (num4 * num39)) * num27;
        result.M44 = ((num1 * num36) - (num2 * num38) + (num3 * num39)) * num27;
    }

    public static Matrix Lerp(Matrix matrix1, Matrix matrix2, float amount) => new(
        matrix1.M11 + ((matrix2.M11 - matrix1.M11) * amount),
        matrix1.M12 + ((matrix2.M12 - matrix1.M12) * amount),
        matrix1.M13 + ((matrix2.M13 - matrix1.M13) * amount),
        matrix1.M14 + ((matrix2.M14 - matrix1.M14) * amount),
        matrix1.M21 + ((matrix2.M21 - matrix1.M21) * amount),
        matrix1.M22 + ((matrix2.M22 - matrix1.M22) * amount),
        matrix1.M23 + ((matrix2.M23 - matrix1.M23) * amount),
        matrix1.M24 + ((matrix2.M24 - matrix1.M24) * amount),
        matrix1.M31 + ((matrix2.M31 - matrix1.M31) * amount),
        matrix1.M32 + ((matrix2.M32 - matrix1.M32) * amount),
        matrix1.M33 + ((matrix2.M33 - matrix1.M33) * amount),
        matrix1.M34 + ((matrix2.M34 - matrix1.M34) * amount),
        matrix1.M41 + ((matrix2.M41 - matrix1.M41) * amount),
        matrix1.M42 + ((matrix2.M42 - matrix1.M42) * amount),
        matrix1.M43 + ((matrix2.M43 - matrix1.M43) * amount),
        matrix1.M44 + ((matrix2.M44 - matrix1.M44) * amount));

    public static void Lerp(ref Matrix matrix1, ref Matrix matrix2, float amount, out Matrix result) =>
        result = Lerp(matrix1, matrix2, amount);

    public static Matrix Multiply(Matrix matrix1, Matrix matrix2) => matrix1 * matrix2;

    public static Matrix Multiply(Matrix matrix1, float scaleFactor) => matrix1 * scaleFactor;

    public static void Multiply(ref Matrix matrix1, float scaleFactor, out Matrix result) =>
        result = matrix1 * scaleFactor;

    public static void Multiply(ref Matrix matrix1, ref Matrix matrix2, out Matrix result) =>
        result = matrix1 * matrix2;

    public static Matrix Negate(Matrix matrix) => -matrix;

    public static void Negate(ref Matrix matrix, out Matrix result) => result = -matrix;

    public static Matrix Subtract(Matrix matrix1, Matrix matrix2) => matrix1 - matrix2;

    public static void Subtract(ref Matrix matrix1, ref Matrix matrix2, out Matrix result) =>
        result = matrix1 - matrix2;

    public static Matrix Transform(Matrix value, Quaternion rotation)
    {
        Transform(ref value, ref rotation, out Matrix result);
        return result;
    }

    public static void Transform(ref Matrix value, ref Quaternion rotation, out Matrix result)
    {
        float x2 = rotation.X + rotation.X;
        float y2 = rotation.Y + rotation.Y;
        float z2 = rotation.Z + rotation.Z;
        float wx2 = rotation.W * x2;
        float wy2 = rotation.W * y2;
        float wz2 = rotation.W * z2;
        float xx2 = rotation.X * x2;
        float xy2 = rotation.X * y2;
        float xz2 = rotation.X * z2;
        float yy2 = rotation.Y * y2;
        float yz2 = rotation.Y * z2;
        float zz2 = rotation.Z * z2;
        float m11 = 1f - yy2 - zz2;
        float m12 = xy2 - wz2;
        float m13 = xz2 + wy2;
        float m21 = xy2 + wz2;
        float m22 = 1f - xx2 - zz2;
        float m23 = yz2 - wx2;
        float m31 = xz2 - wy2;
        float m32 = yz2 + wx2;
        float m33 = 1f - xx2 - yy2;

        float resultM11 = (value.M11 * m11) + (value.M12 * m12) + (value.M13 * m13);
        float resultM12 = (value.M11 * m21) + (value.M12 * m22) + (value.M13 * m23);
        float resultM13 = (value.M11 * m31) + (value.M12 * m32) + (value.M13 * m33);
        float resultM14 = value.M14;
        float resultM21 = (value.M21 * m11) + (value.M22 * m12) + (value.M23 * m13);
        float resultM22 = (value.M21 * m21) + (value.M22 * m22) + (value.M23 * m23);
        float resultM23 = (value.M21 * m31) + (value.M22 * m32) + (value.M23 * m33);
        float resultM24 = value.M24;
        float resultM31 = (value.M31 * m11) + (value.M32 * m12) + (value.M33 * m13);
        float resultM32 = (value.M31 * m21) + (value.M32 * m22) + (value.M33 * m23);
        float resultM33 = (value.M31 * m31) + (value.M32 * m32) + (value.M33 * m33);
        float resultM34 = value.M34;
        float resultM41 = (value.M41 * m11) + (value.M42 * m12) + (value.M43 * m13);
        float resultM42 = (value.M41 * m21) + (value.M42 * m22) + (value.M43 * m23);
        float resultM43 = (value.M41 * m31) + (value.M42 * m32) + (value.M43 * m33);
        float resultM44 = value.M44;

        result = new Matrix(
            resultM11, resultM12, resultM13, resultM14,
            resultM21, resultM22, resultM23, resultM24,
            resultM31, resultM32, resultM33, resultM34,
            resultM41, resultM42, resultM43, resultM44);
    }

    public static void Transpose(ref Matrix matrix, out Matrix result) => result = Transpose(matrix);

    public static Matrix operator *(Matrix matrix1, Matrix matrix2) =>
        FromFramework(matrix1.ToFramework() * matrix2.ToFramework());
    public static Matrix operator *(Matrix matrix, float scaleFactor) => new(
        matrix.M11 * scaleFactor, matrix.M12 * scaleFactor, matrix.M13 * scaleFactor, matrix.M14 * scaleFactor,
        matrix.M21 * scaleFactor, matrix.M22 * scaleFactor, matrix.M23 * scaleFactor, matrix.M24 * scaleFactor,
        matrix.M31 * scaleFactor, matrix.M32 * scaleFactor, matrix.M33 * scaleFactor, matrix.M34 * scaleFactor,
        matrix.M41 * scaleFactor, matrix.M42 * scaleFactor, matrix.M43 * scaleFactor, matrix.M44 * scaleFactor);
    public static Matrix operator *(float scaleFactor, Matrix matrix) => matrix * scaleFactor;
    public static Matrix operator /(Matrix matrix1, Matrix matrix2) => new(
        matrix1.M11 / matrix2.M11, matrix1.M12 / matrix2.M12, matrix1.M13 / matrix2.M13, matrix1.M14 / matrix2.M14,
        matrix1.M21 / matrix2.M21, matrix1.M22 / matrix2.M22, matrix1.M23 / matrix2.M23, matrix1.M24 / matrix2.M24,
        matrix1.M31 / matrix2.M31, matrix1.M32 / matrix2.M32, matrix1.M33 / matrix2.M33, matrix1.M34 / matrix2.M34,
        matrix1.M41 / matrix2.M41, matrix1.M42 / matrix2.M42, matrix1.M43 / matrix2.M43, matrix1.M44 / matrix2.M44);
    public static Matrix operator /(Matrix matrix1, float divider) => matrix1 * (1f / divider);
    public static Matrix operator +(Matrix matrix1, Matrix matrix2) =>
        FromFramework(matrix1.ToFramework() + matrix2.ToFramework());
    public static Matrix operator -(Matrix matrix1, Matrix matrix2) =>
        FromFramework(matrix1.ToFramework() - matrix2.ToFramework());
    public static Matrix operator -(Matrix matrix1) => new(
        -matrix1.M11, -matrix1.M12, -matrix1.M13, -matrix1.M14,
        -matrix1.M21, -matrix1.M22, -matrix1.M23, -matrix1.M24,
        -matrix1.M31, -matrix1.M32, -matrix1.M33, -matrix1.M34,
        -matrix1.M41, -matrix1.M42, -matrix1.M43, -matrix1.M44);

    public static bool operator ==(Matrix matrix1, Matrix matrix2) => matrix1.Equals(matrix2);
    public static bool operator !=(Matrix matrix1, Matrix matrix2) => !matrix1.Equals(matrix2);

    public readonly bool Equals(Matrix other) =>
        M11 == other.M11 && M12 == other.M12 && M13 == other.M13 && M14 == other.M14 &&
        M21 == other.M21 && M22 == other.M22 && M23 == other.M23 && M24 == other.M24 &&
        M31 == other.M31 && M32 == other.M32 && M33 == other.M33 && M34 == other.M34 &&
        M41 == other.M41 && M42 == other.M42 && M43 == other.M43 && M44 == other.M44;

    public override readonly bool Equals(object? obj) => obj is Matrix other && Equals(other);

    public override readonly int GetHashCode()
    {
        return M11.GetHashCode() + M12.GetHashCode() + M13.GetHashCode() + M14.GetHashCode() +
            M21.GetHashCode() + M22.GetHashCode() + M23.GetHashCode() + M24.GetHashCode() +
            M31.GetHashCode() + M32.GetHashCode() + M33.GetHashCode() + M34.GetHashCode() +
            M41.GetHashCode() + M42.GetHashCode() + M43.GetHashCode() + M44.GetHashCode();
    }

    public override readonly string ToString() =>
        $"{{ {{M11:{M11} M12:{M12} M13:{M13} M14:{M14}}} " +
        $"{{M21:{M21} M22:{M22} M23:{M23} M24:{M24}}} " +
        $"{{M31:{M31} M32:{M32} M33:{M33} M34:{M34}}} " +
        $"{{M41:{M41} M42:{M42} M43:{M43} M44:{M44}}} }}";

    internal static Matrix FromFramework(CNA.Matrix value) => new(
        value.M11, value.M12, value.M13, value.M14,
        value.M21, value.M22, value.M23, value.M24,
        value.M31, value.M32, value.M33, value.M34,
        value.M41, value.M42, value.M43, value.M44);

    internal readonly CNA.Matrix ToFramework() => new(
        M11, M12, M13, M14,
        M21, M22, M23, M24,
        M31, M32, M33, M34,
        M41, M42, M43, M44);

    private static void ValidatePerspectivePlanes(float nearPlaneDistance, float farPlaneDistance)
    {
        if (nearPlaneDistance <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));
        }

        if (farPlaneDistance <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(farPlaneDistance));
        }

        if (nearPlaneDistance >= farPlaneDistance)
        {
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));
        }
    }

    private static CNA.Vector3? ToFramework(Vector3? value) => value?.ToFramework();
}
