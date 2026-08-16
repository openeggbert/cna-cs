namespace CNA;

/// <summary>
/// Local, managed row-major 4x4 matrix, row-vector convention (a point transforms as
/// <c>v' = v * M</c>), matching real XNA's <c>Matrix</c> exactly. See the rationale in Vector2.cs.
/// Not implemented: <c>Decompose</c>, <c>CreateBillboard</c>, <c>CreateConstrainedBillboard</c>,
/// <c>CreateShadow</c>, <c>CreateReflection</c>, the non-FOV <c>CreatePerspective</c> overload,
/// and <c>CreatePerspectiveOffCenter</c> -- all rare enough in typical 2D/3D game code to defer;
/// see plan.md Phase 4.
/// </summary>
public struct Matrix : IEquatable<Matrix>
{
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

    public static Matrix Identity => new(
        1f, 0f, 0f, 0f,
        0f, 1f, 0f, 0f,
        0f, 0f, 1f, 0f,
        0f, 0f, 0f, 1f);

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

    public static Matrix CreateTranslation(Vector3 position) => CreateTranslation(position.X, position.Y, position.Z);

    public static Matrix CreateTranslation(float x, float y, float z)
    {
        Matrix result = Identity;
        result.M41 = x;
        result.M42 = y;
        result.M43 = z;
        return result;
    }

    public static Matrix CreateScale(float scale) => CreateScale(scale, scale, scale);

    public static Matrix CreateScale(Vector3 scale) => CreateScale(scale.X, scale.Y, scale.Z);

    public static Matrix CreateScale(float xScale, float yScale, float zScale)
    {
        Matrix result = Identity;
        result.M11 = xScale;
        result.M22 = yScale;
        result.M33 = zScale;
        return result;
    }

    public static Matrix CreateRotationX(float radians)
    {
        Matrix result = Identity;
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        result.M22 = cos;
        result.M23 = sin;
        result.M32 = -sin;
        result.M33 = cos;
        return result;
    }

    public static Matrix CreateRotationY(float radians)
    {
        Matrix result = Identity;
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        result.M11 = cos;
        result.M13 = -sin;
        result.M31 = sin;
        result.M33 = cos;
        return result;
    }

    public static Matrix CreateRotationZ(float radians)
    {
        Matrix result = Identity;
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        result.M11 = cos;
        result.M12 = sin;
        result.M21 = -sin;
        result.M22 = cos;
        return result;
    }

    public static Matrix CreateFromAxisAngle(Vector3 axis, float angle)
    {
        float x = axis.X, y = axis.Y, z = axis.Z;
        float sin = MathF.Sin(angle), cos = MathF.Cos(angle);
        float xx = x * x, yy = y * y, zz = z * z;
        float xy = x * y, xz = x * z, yz = y * z;

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

    public static Matrix CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
    {
        Vector3 forward = Vector3.Normalize(cameraPosition - cameraTarget);
        Vector3 right = Vector3.Normalize(Vector3.Cross(cameraUpVector, forward));
        Vector3 up = Vector3.Cross(forward, right);

        return new Matrix(
            right.X, up.X, forward.X, 0f,
            right.Y, up.Y, forward.Y, 0f,
            right.Z, up.Z, forward.Z, 0f,
            -Vector3.Dot(right, cameraPosition), -Vector3.Dot(up, cameraPosition), -Vector3.Dot(forward, cameraPosition), 1f);
    }

    public static Matrix CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
    {
        Vector3 z = Vector3.Normalize(-forward);
        Vector3 x = Vector3.Normalize(Vector3.Cross(up, z));
        Vector3 y = Vector3.Cross(z, x);

        return new Matrix(
            x.X, x.Y, x.Z, 0f,
            y.X, y.Y, y.Z, 0f,
            z.X, z.Y, z.Z, 0f,
            position.X, position.Y, position.Z, 1f);
    }

    public static Matrix CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
    {
        float yScale = 1f / MathF.Tan(fieldOfView * 0.5f);
        float xScale = yScale / aspectRatio;
        float negFarRange = float.IsPositiveInfinity(farPlaneDistance)
            ? -1f
            : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);

        return new Matrix(
            xScale, 0f, 0f, 0f,
            0f, yScale, 0f, 0f,
            0f, 0f, negFarRange, -1f,
            0f, 0f, nearPlaneDistance * negFarRange, 0f);
    }

    public static Matrix CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
    {
        Matrix result = Identity;
        result.M11 = 2f / width;
        result.M22 = 2f / height;
        result.M33 = 1f / (zNearPlane - zFarPlane);
        result.M43 = zNearPlane / (zNearPlane - zFarPlane);
        return result;
    }

    public static Matrix CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
    {
        Matrix result = Identity;
        result.M11 = 2f / (right - left);
        result.M22 = 2f / (top - bottom);
        result.M33 = 1f / (zNearPlane - zFarPlane);
        result.M41 = (left + right) / (left - right);
        result.M42 = (top + bottom) / (bottom - top);
        result.M43 = zNearPlane / (zNearPlane - zFarPlane);
        return result;
    }

    public readonly float Determinant()
    {
        float num1 = (M33 * M44) - (M34 * M43);
        float num2 = (M32 * M44) - (M34 * M42);
        float num3 = (M32 * M43) - (M33 * M42);
        float num4 = (M31 * M44) - (M34 * M41);
        float num5 = (M31 * M43) - (M33 * M41);
        float num6 = (M31 * M42) - (M32 * M41);

        return (M11 * ((M22 * num1) - (M23 * num2) + (M24 * num3)))
             - (M12 * ((M21 * num1) - (M23 * num4) + (M24 * num5)))
             + (M13 * ((M21 * num2) - (M22 * num4) + (M24 * num6)))
             - (M14 * ((M21 * num3) - (M22 * num5) + (M23 * num6)));
    }

    public static Matrix Transpose(Matrix matrix) => new(
        matrix.M11, matrix.M21, matrix.M31, matrix.M41,
        matrix.M12, matrix.M22, matrix.M32, matrix.M42,
        matrix.M13, matrix.M23, matrix.M33, matrix.M43,
        matrix.M14, matrix.M24, matrix.M34, matrix.M44);

    /// <summary>
    /// Gauss-Jordan elimination with partial pivoting. Not the hand-optimized cofactor expansion
    /// real XNA uses, but a standard, independently-verifiable algorithm -- see MatrixTests for
    /// the round-trip checks this is validated against. Throws if the matrix is singular.
    /// </summary>
    public static Matrix Invert(Matrix matrix)
    {
        Span<double> augmented = stackalloc double[4 * 8];

        for (int row = 0; row < 4; row++)
        {
            augmented[(row * 8) + 0] = row == 0 ? matrix.M11 : row == 1 ? matrix.M21 : row == 2 ? matrix.M31 : matrix.M41;
            augmented[(row * 8) + 1] = row == 0 ? matrix.M12 : row == 1 ? matrix.M22 : row == 2 ? matrix.M32 : matrix.M42;
            augmented[(row * 8) + 2] = row == 0 ? matrix.M13 : row == 1 ? matrix.M23 : row == 2 ? matrix.M33 : matrix.M43;
            augmented[(row * 8) + 3] = row == 0 ? matrix.M14 : row == 1 ? matrix.M24 : row == 2 ? matrix.M34 : matrix.M44;
            augmented[(row * 8) + 4 + row] = 1.0;
        }

        for (int pivot = 0; pivot < 4; pivot++)
        {
            int pivotRow = pivot;
            double bestMagnitude = Math.Abs(augmented[(pivot * 8) + pivot]);
            for (int row = pivot + 1; row < 4; row++)
            {
                double magnitude = Math.Abs(augmented[(row * 8) + pivot]);
                if (magnitude > bestMagnitude)
                {
                    bestMagnitude = magnitude;
                    pivotRow = row;
                }
            }

            if (pivotRow != pivot)
            {
                for (int col = 0; col < 8; col++)
                {
                    (augmented[(pivot * 8) + col], augmented[(pivotRow * 8) + col]) =
                        (augmented[(pivotRow * 8) + col], augmented[(pivot * 8) + col]);
                }
            }

            double pivotValue = augmented[(pivot * 8) + pivot];
            if (Math.Abs(pivotValue) < 1e-12)
            {
                throw new InvalidOperationException("Matrix is not invertible.");
            }

            for (int col = 0; col < 8; col++)
            {
                augmented[(pivot * 8) + col] /= pivotValue;
            }

            for (int row = 0; row < 4; row++)
            {
                if (row == pivot)
                {
                    continue;
                }

                double factor = augmented[(row * 8) + pivot];
                if (factor == 0.0)
                {
                    continue;
                }

                for (int col = 0; col < 8; col++)
                {
                    augmented[(row * 8) + col] -= factor * augmented[(pivot * 8) + col];
                }
            }
        }

        return new Matrix(
            (float)augmented[(0 * 8) + 4], (float)augmented[(0 * 8) + 5], (float)augmented[(0 * 8) + 6], (float)augmented[(0 * 8) + 7],
            (float)augmented[(1 * 8) + 4], (float)augmented[(1 * 8) + 5], (float)augmented[(1 * 8) + 6], (float)augmented[(1 * 8) + 7],
            (float)augmented[(2 * 8) + 4], (float)augmented[(2 * 8) + 5], (float)augmented[(2 * 8) + 6], (float)augmented[(2 * 8) + 7],
            (float)augmented[(3 * 8) + 4], (float)augmented[(3 * 8) + 5], (float)augmented[(3 * 8) + 6], (float)augmented[(3 * 8) + 7]);
    }

    public static Matrix Multiply(Matrix a, Matrix b) => a * b;

    public static Matrix operator *(Matrix a, Matrix b) => new(
        (a.M11 * b.M11) + (a.M12 * b.M21) + (a.M13 * b.M31) + (a.M14 * b.M41),
        (a.M11 * b.M12) + (a.M12 * b.M22) + (a.M13 * b.M32) + (a.M14 * b.M42),
        (a.M11 * b.M13) + (a.M12 * b.M23) + (a.M13 * b.M33) + (a.M14 * b.M43),
        (a.M11 * b.M14) + (a.M12 * b.M24) + (a.M13 * b.M34) + (a.M14 * b.M44),

        (a.M21 * b.M11) + (a.M22 * b.M21) + (a.M23 * b.M31) + (a.M24 * b.M41),
        (a.M21 * b.M12) + (a.M22 * b.M22) + (a.M23 * b.M32) + (a.M24 * b.M42),
        (a.M21 * b.M13) + (a.M22 * b.M23) + (a.M23 * b.M33) + (a.M24 * b.M43),
        (a.M21 * b.M14) + (a.M22 * b.M24) + (a.M23 * b.M34) + (a.M24 * b.M44),

        (a.M31 * b.M11) + (a.M32 * b.M21) + (a.M33 * b.M31) + (a.M34 * b.M41),
        (a.M31 * b.M12) + (a.M32 * b.M22) + (a.M33 * b.M32) + (a.M34 * b.M42),
        (a.M31 * b.M13) + (a.M32 * b.M23) + (a.M33 * b.M33) + (a.M34 * b.M43),
        (a.M31 * b.M14) + (a.M32 * b.M24) + (a.M33 * b.M34) + (a.M34 * b.M44),

        (a.M41 * b.M11) + (a.M42 * b.M21) + (a.M43 * b.M31) + (a.M44 * b.M41),
        (a.M41 * b.M12) + (a.M42 * b.M22) + (a.M43 * b.M32) + (a.M44 * b.M42),
        (a.M41 * b.M13) + (a.M42 * b.M23) + (a.M43 * b.M33) + (a.M44 * b.M43),
        (a.M41 * b.M14) + (a.M42 * b.M24) + (a.M43 * b.M34) + (a.M44 * b.M44));

    public static Matrix operator +(Matrix a, Matrix b) => new(
        a.M11 + b.M11, a.M12 + b.M12, a.M13 + b.M13, a.M14 + b.M14,
        a.M21 + b.M21, a.M22 + b.M22, a.M23 + b.M23, a.M24 + b.M24,
        a.M31 + b.M31, a.M32 + b.M32, a.M33 + b.M33, a.M34 + b.M34,
        a.M41 + b.M41, a.M42 + b.M42, a.M43 + b.M43, a.M44 + b.M44);

    public static Matrix operator -(Matrix a, Matrix b) => new(
        a.M11 - b.M11, a.M12 - b.M12, a.M13 - b.M13, a.M14 - b.M14,
        a.M21 - b.M21, a.M22 - b.M22, a.M23 - b.M23, a.M24 - b.M24,
        a.M31 - b.M31, a.M32 - b.M32, a.M33 - b.M33, a.M34 - b.M34,
        a.M41 - b.M41, a.M42 - b.M42, a.M43 - b.M43, a.M44 - b.M44);

    public static bool operator ==(Matrix a, Matrix b) => a.Equals(b);
    public static bool operator !=(Matrix a, Matrix b) => !a.Equals(b);

    public readonly bool Equals(Matrix other) =>
        M11.Equals(other.M11) && M12.Equals(other.M12) && M13.Equals(other.M13) && M14.Equals(other.M14) &&
        M21.Equals(other.M21) && M22.Equals(other.M22) && M23.Equals(other.M23) && M24.Equals(other.M24) &&
        M31.Equals(other.M31) && M32.Equals(other.M32) && M33.Equals(other.M33) && M34.Equals(other.M34) &&
        M41.Equals(other.M41) && M42.Equals(other.M42) && M43.Equals(other.M43) && M44.Equals(other.M44);

    public override readonly bool Equals(object? obj) => obj is Matrix other && Equals(other);

    public override readonly int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(M11); hash.Add(M12); hash.Add(M13); hash.Add(M14);
        hash.Add(M21); hash.Add(M22); hash.Add(M23); hash.Add(M24);
        hash.Add(M31); hash.Add(M32); hash.Add(M33); hash.Add(M34);
        hash.Add(M41); hash.Add(M42); hash.Add(M43); hash.Add(M44);
        return hash.ToHashCode();
    }

    public override readonly string ToString() =>
        $"{{M11:{M11} M12:{M12} M13:{M13} M14:{M14}}} " +
        $"{{M21:{M21} M22:{M22} M23:{M23} M24:{M24}}} " +
        $"{{M31:{M31} M32:{M32} M33:{M33} M34:{M34}}} " +
        $"{{M41:{M41} M42:{M42} M43:{M43} M44:{M44}}}";
}
