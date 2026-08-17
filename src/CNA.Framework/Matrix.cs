namespace CNA;

/// <summary>
/// Local, managed row-major 4x4 matrix, row-vector convention (a point transforms as
/// <c>v' = v * M</c>), matching real XNA's <c>Matrix</c> exactly. See the rationale in Vector2.cs.
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

    /// <summary>Shared precondition for every perspective-matrix constructor, matching real
    /// XNA's own validation. <c>farPlaneDistance</c> is deliberately allowed to be
    /// <see cref="float.PositiveInfinity"/> (an infinite far plane is a real, supported case --
    /// see <see cref="NegFarRange"/>), so only the ordering/non-positivity checks apply to it.
    /// </summary>
    private static void ValidatePerspectivePlanes(float nearPlaneDistance, float farPlaneDistance)
    {
        if (nearPlaneDistance <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), nearPlaneDistance, "Must be greater than zero.");
        }

        if (farPlaneDistance <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(farPlaneDistance), farPlaneDistance, "Must be greater than zero.");
        }

        if (nearPlaneDistance >= farPlaneDistance)
        {
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), nearPlaneDistance, "Must be less than farPlaneDistance.");
        }
    }

    /// <summary>Shared by every perspective-matrix constructor for the <c>M33</c>/<c>M43</c>
    /// far-plane terms -- previously reimplemented identically three times in this file.</summary>
    private static float NegFarRange(float nearPlaneDistance, float farPlaneDistance) =>
        float.IsPositiveInfinity(farPlaneDistance) ? -1f : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);

    public static Matrix CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
    {
        ValidatePerspectivePlanes(nearPlaneDistance, farPlaneDistance);

        float yScale = 1f / MathF.Tan(fieldOfView * 0.5f);
        float xScale = yScale / aspectRatio;
        float negFarRange = NegFarRange(nearPlaneDistance, farPlaneDistance);

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

    /// <summary>The non-FOV perspective overload -- same <c>M33</c>/<c>M43</c>/<c>M34</c> formulas
    /// as <see cref="CreatePerspectiveFieldOfView"/>, just deriving <c>M11</c>/<c>M22</c> straight
    /// from a near-plane width/height instead of a field of view; cross-checked against it in
    /// MatrixTests for an equivalent width/height/fov/aspect combination.</summary>
    public static Matrix CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance)
    {
        ValidatePerspectivePlanes(nearPlaneDistance, farPlaneDistance);
        float negFarRange = NegFarRange(nearPlaneDistance, farPlaneDistance);

        Matrix result = default;
        result.M11 = (2f * nearPlaneDistance) / width;
        result.M22 = (2f * nearPlaneDistance) / height;
        result.M33 = negFarRange;
        result.M34 = -1f;
        result.M43 = nearPlaneDistance * negFarRange;
        return result;
    }

    /// <summary>The asymmetric-frustum perspective overload (used for things like off-axis VR
    /// projections). Reduces to <see cref="CreatePerspective"/> when the frustum is centered
    /// (<c>left = -right</c>, <c>bottom = -top</c>) -- cross-checked against it in
    /// MatrixTests.</summary>
    public static Matrix CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
    {
        ValidatePerspectivePlanes(nearPlaneDistance, farPlaneDistance);
        float negFarRange = NegFarRange(nearPlaneDistance, farPlaneDistance);

        Matrix result = default;
        result.M11 = (2f * nearPlaneDistance) / (right - left);
        result.M22 = (2f * nearPlaneDistance) / (top - bottom);
        result.M31 = (left + right) / (right - left);
        result.M32 = (top + bottom) / (top - bottom);
        result.M33 = negFarRange;
        result.M34 = -1f;
        result.M43 = nearPlaneDistance * negFarRange;
        return result;
    }

    /// <summary>
    /// Decomposes a translation * rotation * non-uniform-scale matrix back into its three parts.
    /// Extracts scale as each row's length, normalizes the rows to a pure rotation matrix, then
    /// converts that to a quaternion via <see cref="Quaternion.CreateFromRotationMatrix"/>.
    /// Known limitation, worth flagging explicitly: unlike real XNA's own (independently
    /// known-imperfect) negative-scale heuristic, this does not attempt to detect a negative/
    /// mirrored scale component at all -- a matrix built with a negative scale axis decomposes
    /// to a positive scale plus a compensating rotation instead. Fine for the overwhelmingly
    /// common case (uniform or non-uniform *positive* scale, no shear); returns <c>false</c> (and
    /// <see cref="Quaternion.Identity"/>) only when a scale component is exactly (near-)zero,
    /// which is genuinely non-decomposable rather than a limitation of this implementation.
    /// </summary>
    public readonly bool Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation)
    {
        translation = new Vector3(M41, M42, M43);

        var row1 = new Vector3(M11, M12, M13);
        var row2 = new Vector3(M21, M22, M23);
        var row3 = new Vector3(M31, M32, M33);

        float scaleX = row1.Length();
        float scaleY = row2.Length();
        float scaleZ = row3.Length();
        scale = new Vector3(scaleX, scaleY, scaleZ);

        if (scaleX < float.Epsilon || scaleY < float.Epsilon || scaleZ < float.Epsilon)
        {
            rotation = Quaternion.Identity;
            return false;
        }

        var rotationMatrix = new Matrix(
            row1.X / scaleX, row1.Y / scaleX, row1.Z / scaleX, 0f,
            row2.X / scaleY, row2.Y / scaleY, row2.Z / scaleY, 0f,
            row3.X / scaleZ, row3.Y / scaleZ, row3.Z / scaleZ, 0f,
            0f, 0f, 0f, 1f);

        rotation = Quaternion.CreateFromRotationMatrix(rotationMatrix);
        return true;
    }

    /// <summary>
    /// Builds a world matrix for a sprite/quad that always faces <paramref name="cameraPosition"/>,
    /// free to rotate around any axis (compare <see cref="CreateConstrainedBillboard"/>, which
    /// locks one axis). Row layout mirrors <see cref="CreateLookAt"/>'s right/up/forward
    /// construction. <paramref name="cameraForwardVector"/> is only consulted in the degenerate
    /// case where <paramref name="objectPosition"/> and <paramref name="cameraPosition"/>
    /// coincide (no direction to face) -- negated, matching real XNA/MonoGame's own fallback
    /// (<c>cameraForwardVector</c> is "the direction the camera is looking," i.e. into the
    /// scene, whereas this method's <c>forward</c> variable points away from the camera, so the
    /// two need a sign flip between them).
    /// </summary>
    public static Matrix CreateBillboard(
        Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3? cameraForwardVector)
    {
        Vector3 delta = objectPosition - cameraPosition;
        float deltaLengthSquared = delta.LengthSquared();
        Vector3 forward = deltaLengthSquared < 0.0001f
            ? (cameraForwardVector.HasValue ? -cameraForwardVector.Value : Vector3.Forward)
            : delta * (1f / MathF.Sqrt(deltaLengthSquared));

        Vector3 right = Vector3.Normalize(Vector3.Cross(cameraUpVector, forward));
        Vector3 up = Vector3.Cross(forward, right);

        return new Matrix(
            right.X, right.Y, right.Z, 0f,
            up.X, up.Y, up.Z, 0f,
            forward.X, forward.Y, forward.Z, 0f,
            objectPosition.X, objectPosition.Y, objectPosition.Z, 1f);
    }

    /// <summary>
    /// Like <see cref="CreateBillboard"/>, but the "up" axis is locked to
    /// <paramref name="rotateAxis"/> instead of being derived from a camera up vector -- used for
    /// things like tree/grass billboards that should only ever rotate around the world Y axis.
    /// The near-parallel-axis degenerate branch (facing direction and rotate axis almost
    /// coincide, so "right" can't be derived from their cross product) is a simplified fallback,
    /// not a reproduction of real XNA's specific fallback-axis-selection logic -- lower confidence
    /// than <see cref="CreateBillboard"/>'s primary path, which this otherwise matches exactly,
    /// including the <paramref name="cameraForwardVector"/> negation in the coincident-positions
    /// fallback (see that method's doc comment).
    /// </summary>
    public static Matrix CreateConstrainedBillboard(
        Vector3 objectPosition,
        Vector3 cameraPosition,
        Vector3 rotateAxis,
        Vector3? cameraForwardVector,
        Vector3? objectForwardVector)
    {
        Vector3 delta = objectPosition - cameraPosition;
        float deltaLengthSquared = delta.LengthSquared();
        Vector3 faceDirection = deltaLengthSquared < 0.0001f
            ? (cameraForwardVector.HasValue ? -cameraForwardVector.Value : Vector3.Forward)
            : delta * (1f / MathF.Sqrt(deltaLengthSquared));

        Vector3 up = Vector3.Normalize(rotateAxis);
        float axisAlignment = Vector3.Dot(up, faceDirection);

        Vector3 right, forward;
        if (MathF.Abs(axisAlignment) > 0.9982547f)
        {
            forward = objectForwardVector ?? (MathF.Abs(up.Z) > 0.9982547f ? Vector3.Right : new Vector3(0f, 0f, -1f));
            right = Vector3.Normalize(Vector3.Cross(up, forward));
            forward = Vector3.Cross(right, up);
        }
        else
        {
            right = Vector3.Normalize(Vector3.Cross(up, faceDirection));
            forward = Vector3.Cross(right, up);
        }

        return new Matrix(
            right.X, right.Y, right.Z, 0f,
            up.X, up.Y, up.Z, 0f,
            forward.X, forward.Y, forward.Z, 0f,
            objectPosition.X, objectPosition.Y, objectPosition.Z, 1f);
    }

    /// <summary>
    /// Standard planar-shadow-projection matrix (flattens geometry onto <paramref name="plane"/>
    /// as seen from <paramref name="lightDirection"/>) -- textbook formula, not specific to XNA.
    /// The result generally has <c>M44 != 1</c>, so transforming a point through it needs a real
    /// homogeneous divide (<see cref="Vector4.Transform(Vector4, Matrix)"/> then divide by
    /// <c>W</c>), not the affine-only <see cref="Vector3.Transform(Vector3, Matrix)"/> -- see
    /// MatrixTests for how this is actually verified.
    /// </summary>
    public static Matrix CreateShadow(Vector3 lightDirection, Plane plane)
    {
        Plane normalized = Plane.Normalize(plane);
        float dot = Vector3.Dot(normalized.Normal, lightDirection);
        float nx = normalized.Normal.X, ny = normalized.Normal.Y, nz = normalized.Normal.Z, d = normalized.D;

        Matrix result;
        result.M11 = (-nx * lightDirection.X) + dot;
        result.M12 = -nx * lightDirection.Y;
        result.M13 = -nx * lightDirection.Z;
        result.M14 = 0f;

        result.M21 = -ny * lightDirection.X;
        result.M22 = (-ny * lightDirection.Y) + dot;
        result.M23 = -ny * lightDirection.Z;
        result.M24 = 0f;

        result.M31 = -nz * lightDirection.X;
        result.M32 = -nz * lightDirection.Y;
        result.M33 = (-nz * lightDirection.Z) + dot;
        result.M34 = 0f;

        result.M41 = -d * lightDirection.X;
        result.M42 = -d * lightDirection.Y;
        result.M43 = -d * lightDirection.Z;
        result.M44 = dot;

        return result;
    }

    /// <summary>Standard Householder-style planar reflection matrix -- textbook formula, not
    /// specific to XNA. Unlike <see cref="CreateShadow"/>, this one is a proper affine (rigid,
    /// <c>M44 = 1</c>) transform, so the ordinary <see cref="Vector3.Transform(Vector3, Matrix)"/>
    /// is enough to use it.</summary>
    public static Matrix CreateReflection(Plane plane)
    {
        Plane normalized = Plane.Normalize(plane);
        float x = normalized.Normal.X, y = normalized.Normal.Y, z = normalized.Normal.Z;
        float x2 = -2f * x, y2 = -2f * y, z2 = -2f * z;

        Matrix result;
        result.M11 = (x2 * x) + 1f;
        result.M12 = y2 * x;
        result.M13 = z2 * x;
        result.M14 = 0f;

        result.M21 = x2 * y;
        result.M22 = (y2 * y) + 1f;
        result.M23 = z2 * y;
        result.M24 = 0f;

        result.M31 = x2 * z;
        result.M32 = y2 * z;
        result.M33 = (z2 * z) + 1f;
        result.M34 = 0f;

        result.M41 = x2 * normalized.D;
        result.M42 = y2 * normalized.D;
        result.M43 = z2 * normalized.D;
        result.M44 = 1f;

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

    /// <summary>Matches <see cref="CNA.Interop.CnaMatrix"/>'s own row-major field order exactly --
    /// a direct field copy, not a transpose (see that type's own doc comment).</summary>
    internal readonly CNA.Interop.CnaMatrix ToNative() => new(
        M11, M12, M13, M14,
        M21, M22, M23, M24,
        M31, M32, M33, M34,
        M41, M42, M43, M44);

    /// <summary>Inverse of <see cref="ToNative"/>.</summary>
    internal static Matrix FromNative(CNA.Interop.CnaMatrix native) => new(
        native.M11, native.M12, native.M13, native.M14,
        native.M21, native.M22, native.M23, native.M24,
        native.M31, native.M32, native.M33, native.M34,
        native.M41, native.M42, native.M43, native.M44);
}
