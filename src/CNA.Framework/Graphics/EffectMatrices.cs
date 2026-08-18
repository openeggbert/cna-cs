using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// The shared <c>cna_effect_matrices_*</c> round trips, factored out so every stock effect that
/// implements <see cref="IEffectMatrices"/> forwards to one implementation instead of repeating
/// six near-identical property bodies.
///
/// Those native functions are effect-wide, not per-effect-type (confirmed while binding
/// <see cref="BasicEffect"/> during the native-ABI migration: <c>World</c>/<c>View</c>/
/// <c>Projection</c> route through the shared <c>IEffectMatrices</c> contract in the C API too),
/// which is what makes a single helper correct here rather than a shortcut.
/// </summary>
internal static class EffectMatrices
{
    public static Matrix GetWorld(CnaHandle effect) => Get(Native.cna_effect_matrices_get_world, effect, "World");

    public static void SetWorld(CnaHandle effect, Matrix value) => Set(Native.cna_effect_matrices_set_world, effect, value, "World");

    public static Matrix GetView(CnaHandle effect) => Get(Native.cna_effect_matrices_get_view, effect, "View");

    public static void SetView(CnaHandle effect, Matrix value) => Set(Native.cna_effect_matrices_set_view, effect, value, "View");

    public static Matrix GetProjection(CnaHandle effect) => Get(Native.cna_effect_matrices_get_projection, effect, "Projection");

    public static void SetProjection(CnaHandle effect, Matrix value) =>
        Set(Native.cna_effect_matrices_set_projection, effect, value, "Projection");

    private delegate CnaResult GetFunc(CnaHandle effect, out CnaMatrix outValue);

    private delegate CnaResult SetFunc(CnaHandle effect, CnaMatrix value);

    private static Matrix Get(GetFunc getter, CnaHandle effect, string propertyName)
    {
        CnaResult result = getter(effect, out CnaMatrix value);
        CnaException.ThrowIfFailed(result, propertyName);
        return Matrix.FromNative(value);
    }

    private static void Set(SetFunc setter, CnaHandle effect, Matrix value, string propertyName)
    {
        CnaResult result = setter(effect, value.ToNative());
        CnaException.ThrowIfFailed(result, propertyName);
    }
}
