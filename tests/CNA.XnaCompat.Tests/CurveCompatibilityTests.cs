using Xunit;
using XnaCurve = Microsoft.Xna.Framework.Curve;
using XnaCurveKey = Microsoft.Xna.Framework.CurveKey;
using XnaCurveKeyCollection = Microsoft.Xna.Framework.CurveKeyCollection;

namespace CNA.XnaCompat.Tests;

public class CurveCompatibilityTests
{
    [Fact]
    public void Collection_AllowsDuplicatePositions_AndKeepsInsertionOrderAmongDuplicates()
    {
        var first = new XnaCurveKey(1f, 10f);
        var second = new XnaCurveKey(1f, 20f);
        var keys = new XnaCurveKeyCollection { first, second };

        Assert.Equal(2, keys.Count);
        Assert.Same(first, keys[0]);
        Assert.Same(second, keys[1]);
    }

    [Fact]
    public void CollectionClone_IsAShallowCopyAsInXna()
    {
        var key = new XnaCurveKey(1f, 10f);
        var keys = new XnaCurveKeyCollection { key };

        XnaCurveKeyCollection clone = keys.Clone();

        Assert.NotSame(keys, clone);
        Assert.Same(key, clone[0]);
        clone[0].Value = 42f;
        Assert.Equal(42f, keys[0].Value);
    }

    [Fact]
    public void CurveClone_RetainsSharedKeyInstances()
    {
        var curve = new XnaCurve();
        curve.Keys.Add(new XnaCurveKey(0f, 5f));

        XnaCurve clone = curve.Clone();

        Assert.NotSame(curve.Keys, clone.Keys);
        Assert.Same(curve.Keys[0], clone.Keys[0]);
    }

    [Fact]
    public void Collection_ImplementsCompatGenericContract()
    {
        ICollection<XnaCurveKey> contract = new XnaCurveKeyCollection();
        contract.Add(new XnaCurveKey(0f, 0f));
        Assert.Single(contract);
    }
}
