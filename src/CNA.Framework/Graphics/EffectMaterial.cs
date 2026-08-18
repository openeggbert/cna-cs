using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// Matches real XNA's <c>EffectMaterial</c>: a per-material clone of an existing
/// <see cref="Effect"/>, carrying its own parameter values while sharing the source's shader.
///
/// Note the native constructor takes an effect to clone, not a device
/// (<c>cna_effect_material_create</c>) -- which is exactly what the type means, and why this has no
/// device-taking constructor unlike every other effect here.
/// </summary>
public class EffectMaterial : StockEffect
{
    public EffectMaterial(Effect cloneSource)
        : base(RequireDevice(cloneSource), CreateNative(cloneSource))
    {
    }

    private static GraphicsDevice RequireDevice(Effect cloneSource)
    {
        ArgumentNullException.ThrowIfNull(cloneSource);
        return cloneSource.GraphicsDevice;
    }

    private static CnaHandle CreateNative(Effect cloneSource)
    {
        ArgumentNullException.ThrowIfNull(cloneSource);

        if (cloneSource is not StockEffect stockEffect)
        {
            throw new NotSupportedException(
                $"EffectMaterial can only clone an effect backed by a native CNA effect; {cloneSource.GetType().Name} is not one.");
        }

        CnaResult result = Native.cna_effect_material_create(stockEffect.NativeEffectHandleForCloning, out CnaHandle effect);
        CnaException.ThrowIfFailed(result, nameof(EffectMaterial));
        return effect;
    }
}
