namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>BoundingFrustum</c>. Unlike the other bounding volumes, real XNA's
/// <c>BoundingFrustum</c> is a class, not a struct, so this is a genuine subclass of
/// <see cref="CNA.Framework.BoundingFrustum"/> -- no duplicated fields or logic. Only
/// <see cref="GetCorners"/> needs to be re-declared, since array element types don't benefit from
/// the implicit conversion operators the way single values do; everything else (<c>Contains</c>,
/// <c>Intersects</c>, <c>Near</c>/<c>Far</c>/<c>Left</c>/<c>Right</c>/<c>Top</c>/<c>Bottom</c>,
/// <c>Matrix</c>) is inherited unchanged.
/// </summary>
public sealed class BoundingFrustum : CNA.Framework.BoundingFrustum
{
    public BoundingFrustum(Matrix value)
        : base(value)
    {
    }

    public new Vector3[] GetCorners()
    {
        CNA.Framework.Vector3[] source = base.GetCorners();
        var result = new Vector3[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            result[i] = source[i];
        }

        return result;
    }
}
