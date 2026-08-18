using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_Matrix</c> exactly
/// (<c>math_values.h:92-...</c>) -- row-major (<c>m11</c> is "row 1, column 1"), the same
/// convention <c>CNA.Matrix</c> already used before this migration (confirmed field-for-field,
/// not assumed): a direct, untransposed field copy is correct. Passed *by value* to every
/// <c>cna_effect_matrices_set_*</c> function and *by pointer* to every <c>_get_*</c> one, matching
/// this file's other by-value blittable structs (<see cref="CnaColor"/>, <see cref="CnaVector2"/>).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaMatrix
{
    public readonly float M11, M12, M13, M14;
    public readonly float M21, M22, M23, M24;
    public readonly float M31, M32, M33, M34;
    public readonly float M41, M42, M43, M44;

    public CnaMatrix(
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
}
