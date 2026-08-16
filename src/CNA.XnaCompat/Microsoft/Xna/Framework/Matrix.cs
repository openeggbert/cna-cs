namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>Matrix</c>. Fields are duplicated (real XNA code reads/writes
/// <c>matrix.M11</c> etc. directly), but every formula -- construction, multiplication,
/// inversion, transpose -- delegates to <see cref="CNA.Matrix"/> via the implicit
/// conversion operators, so there is exactly one implementation of the actual math. See
/// docs/architecture.md and Matrix.cs in CNA for what is and isn't implemented.
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

    public static Matrix Identity => From(CNA.Matrix.Identity);

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

    public static Matrix CreateTranslation(Vector3 position) => From(CNA.Matrix.CreateTranslation(position));

    public static Matrix CreateTranslation(float x, float y, float z) => From(CNA.Matrix.CreateTranslation(x, y, z));

    public static Matrix CreateScale(float scale) => From(CNA.Matrix.CreateScale(scale));

    public static Matrix CreateScale(Vector3 scale) => From(CNA.Matrix.CreateScale(scale));

    public static Matrix CreateScale(float xScale, float yScale, float zScale) =>
        From(CNA.Matrix.CreateScale(xScale, yScale, zScale));

    public static Matrix CreateRotationX(float radians) => From(CNA.Matrix.CreateRotationX(radians));

    public static Matrix CreateRotationY(float radians) => From(CNA.Matrix.CreateRotationY(radians));

    public static Matrix CreateRotationZ(float radians) => From(CNA.Matrix.CreateRotationZ(radians));

    public static Matrix CreateFromAxisAngle(Vector3 axis, float angle) =>
        From(CNA.Matrix.CreateFromAxisAngle(axis, angle));

    public static Matrix CreateFromQuaternion(Quaternion quaternion) =>
        From(CNA.Matrix.CreateFromQuaternion(quaternion));

    public static Matrix CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector) =>
        From(CNA.Matrix.CreateLookAt(cameraPosition, cameraTarget, cameraUpVector));

    public static Matrix CreateWorld(Vector3 position, Vector3 forward, Vector3 up) =>
        From(CNA.Matrix.CreateWorld(position, forward, up));

    public static Matrix CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance) =>
        From(CNA.Matrix.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance));

    public static Matrix CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane) =>
        From(CNA.Matrix.CreateOrthographic(width, height, zNearPlane, zFarPlane));

    public static Matrix CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane) =>
        From(CNA.Matrix.CreateOrthographicOffCenter(left, right, bottom, top, zNearPlane, zFarPlane));

    public readonly float Determinant() => ((CNA.Matrix)this).Determinant();

    public static Matrix Transpose(Matrix matrix) => From(CNA.Matrix.Transpose(matrix));

    public static Matrix Invert(Matrix matrix) => From(CNA.Matrix.Invert(matrix));

    public static Matrix Multiply(Matrix a, Matrix b) => a * b;

    public static Matrix operator *(Matrix a, Matrix b) => From((CNA.Matrix)a * (CNA.Matrix)b);
    public static Matrix operator +(Matrix a, Matrix b) => From((CNA.Matrix)a + (CNA.Matrix)b);
    public static Matrix operator -(Matrix a, Matrix b) => From((CNA.Matrix)a - (CNA.Matrix)b);

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

    private static Matrix From(CNA.Matrix value) => new(
        value.M11, value.M12, value.M13, value.M14,
        value.M21, value.M22, value.M23, value.M24,
        value.M31, value.M32, value.M33, value.M34,
        value.M41, value.M42, value.M43, value.M44);

    public static implicit operator CNA.Matrix(Matrix value) => new(
        value.M11, value.M12, value.M13, value.M14,
        value.M21, value.M22, value.M23, value.M24,
        value.M31, value.M32, value.M33, value.M34,
        value.M41, value.M42, value.M43, value.M44);

    public static implicit operator Matrix(CNA.Matrix value) => From(value);
}
