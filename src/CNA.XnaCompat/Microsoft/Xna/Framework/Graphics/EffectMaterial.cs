namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible <c>EffectMaterial</c>. A pure subclass -- it adds no members of its
/// own beyond the clone constructor. Takes the base <see cref="CNA.Graphics.Effect"/> as its clone
/// source, which is the type the compat stock effects actually share (see
/// <see cref="BasicEffect"/>'s doc comment).</summary>
public class EffectMaterial : CNA.Graphics.EffectMaterial
{
    public EffectMaterial(CNA.Graphics.Effect cloneSource)
        : base(cloneSource)
    {
    }
}
