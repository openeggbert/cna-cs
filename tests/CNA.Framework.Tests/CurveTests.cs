using Xunit;

namespace CNA.Framework.Tests;

/// <summary>
/// <see cref="Curve"/> is one of the few Phase 8 types that is genuinely testable here: it is
/// managed math with no native dependency (see its own doc comment for why that was a deliberate
/// choice over binding <c>curve.h</c>).
///
/// <b>These tests are written to fail on a wrong implementation, not merely to pass on the right
/// one.</b> An earlier revision claimed to pin the Hermite basis but did not: every in-range
/// evaluation used a curve whose tangents were both zero, so swapping <c>TangentIn</c> and
/// <c>TangentOut</c> in the basis passed all of them, as did rescaling both tangent terms by the
/// segment duration. Every loop-mode test also started its range at zero, so dropping the
/// <c>rangeStart</c> offset passed too, and <see cref="Curve"/>'s binary search was only ever run
/// with two keys, where its loop body never executes -- <c>return 0;</c> passed. A code-review
/// pass found all four holes. The tests below close them with hand-computed values on curves that
/// have non-zero asymmetric tangents, a non-zero range start, and five keys.
/// </summary>
public class CurveTests
{
    private static Curve TwoKeyLine()
    {
        var curve = new Curve();
        curve.Keys.Add(new CurveKey(0f, 0f));
        curve.Keys.Add(new CurveKey(1f, 10f));
        return curve;
    }

    [Fact]
    public void Evaluate_NoKeys_ReturnsZero() => Assert.Equal(0f, new Curve().Evaluate(5f));

    [Fact]
    public void Evaluate_SingleKey_AlwaysReturnsThatValue()
    {
        var curve = new Curve();
        curve.Keys.Add(new CurveKey(3f, 7f));

        Assert.Equal(7f, curve.Evaluate(-100f));
        Assert.Equal(7f, curve.Evaluate(3f));
        Assert.Equal(7f, curve.Evaluate(100f));
    }

    [Fact]
    public void IsConstant_TrueBelowTwoKeys()
    {
        var curve = new Curve();
        Assert.True(curve.IsConstant);

        curve.Keys.Add(new CurveKey(0f, 0f));
        Assert.True(curve.IsConstant);

        curve.Keys.Add(new CurveKey(1f, 1f));
        Assert.False(curve.IsConstant);
    }

    [Fact]
    public void Evaluate_AtKeyPositions_ReturnsKeyValuesExactly()
    {
        Curve curve = TwoKeyLine();

        Assert.Equal(0f, curve.Evaluate(0f));
        Assert.Equal(10f, curve.Evaluate(1f));
    }

    /// <summary>With both tangents zero the Hermite basis reduces to smoothstep, so the midpoint
    /// is exactly half -- a value that would change if the basis coefficients were wrong.</summary>
    [Fact]
    public void Evaluate_ZeroTangents_IsSmoothstep()
    {
        Curve curve = TwoKeyLine();

        Assert.Equal(5f, curve.Evaluate(0.5f), 5);
        Assert.Equal(1.5625f, curve.Evaluate(0.25f), 5);
        Assert.Equal(8.4375f, curve.Evaluate(0.75f), 5);
    }

    [Fact]
    public void Evaluate_StepContinuity_HoldsStartValueAcrossSegment()
    {
        var curve = new Curve();
        curve.Keys.Add(new CurveKey(0f, 0f) { Continuity = CurveContinuity.Step });
        curve.Keys.Add(new CurveKey(1f, 10f));

        Assert.Equal(0f, curve.Evaluate(0.1f));
        Assert.Equal(0f, curve.Evaluate(0.9f));
        Assert.Equal(10f, curve.Evaluate(1f));
    }

    [Fact]
    public void Evaluate_ConstantLoop_ClampsToEndpoints()
    {
        Curve curve = TwoKeyLine();
        curve.PreLoop = CurveLoopType.Constant;
        curve.PostLoop = CurveLoopType.Constant;

        Assert.Equal(0f, curve.Evaluate(-5f));
        Assert.Equal(10f, curve.Evaluate(5f));
    }

    [Fact]
    public void Evaluate_CycleLoop_WrapsBackIntoRange()
    {
        Curve curve = TwoKeyLine();
        curve.PostLoop = CurveLoopType.Cycle;
        curve.PreLoop = CurveLoopType.Cycle;

        // 1.5 and 2.5 both map to 0.5 within the [0,1] range.
        Assert.Equal(curve.Evaluate(0.5f), curve.Evaluate(1.5f), 5);
        Assert.Equal(curve.Evaluate(0.5f), curve.Evaluate(2.5f), 5);

        // Negative positions must wrap to the same place, which a plain C# % would get wrong.
        Assert.Equal(curve.Evaluate(0.5f), curve.Evaluate(-0.5f), 5);
    }

    [Fact]
    public void Evaluate_CycleOffsetLoop_AddsOneRangeDeltaPerCycle()
    {
        Curve curve = TwoKeyLine();
        curve.PostLoop = CurveLoopType.CycleOffset;
        curve.PreLoop = CurveLoopType.CycleOffset;

        float mid = curve.Evaluate(0.5f);

        Assert.Equal(mid + 10f, curve.Evaluate(1.5f), 5);
        Assert.Equal(mid + 20f, curve.Evaluate(2.5f), 5);
        Assert.Equal(mid - 10f, curve.Evaluate(-0.5f), 5);
    }

    [Fact]
    public void Evaluate_OscillateLoop_MirrorsEveryOtherCycle()
    {
        Curve curve = TwoKeyLine();
        curve.PostLoop = CurveLoopType.Oscillate;

        // 1.25 folds back to 0.75; 2.25 is a full double-cycle on from 0.25.
        Assert.Equal(curve.Evaluate(0.75f), curve.Evaluate(1.25f), 5);
        Assert.Equal(curve.Evaluate(0.25f), curve.Evaluate(2.25f), 5);
    }

    [Fact]
    public void Evaluate_LinearLoop_ExtrapolatesAlongBoundaryTangents()
    {
        Curve curve = TwoKeyLine();
        curve.Keys[0].TangentIn = 2f;
        curve.Keys[1].TangentOut = 3f;
        curve.PreLoop = CurveLoopType.Linear;
        curve.PostLoop = CurveLoopType.Linear;

        Assert.Equal(0f - (2f * 1f), curve.Evaluate(-1f), 5);
        Assert.Equal(10f + (3f * 2f), curve.Evaluate(3f), 5);
    }

    /// <summary>Real XNA's linear tangents are asymmetric -- in looks back, out looks forward --
    /// which an earlier draft of this type got wrong by computing one formula for both.</summary>
    [Fact]
    public void ComputeTangents_Linear_UsesBackwardAndForwardDeltas()
    {
        var curve = new Curve();
        curve.Keys.Add(new CurveKey(0f, 0f));
        curve.Keys.Add(new CurveKey(1f, 10f));
        curve.Keys.Add(new CurveKey(2f, 12f));

        curve.ComputeTangents(CurveTangent.Linear);

        Assert.Equal(10f, curve.Keys[1].TangentIn, 5);
        Assert.Equal(2f, curve.Keys[1].TangentOut, 5);
    }

    [Fact]
    public void ComputeTangents_Smooth_WeightsTotalDeltaBySideSpan()
    {
        var curve = new Curve();
        curve.Keys.Add(new CurveKey(0f, 0f));
        curve.Keys.Add(new CurveKey(1f, 10f));
        curve.Keys.Add(new CurveKey(3f, 30f));

        curve.ComputeTangents(CurveTangent.Smooth);

        // total span 3, total delta 30; the middle key's in-side span is 1 and out-side span is 2.
        Assert.Equal(10f, curve.Keys[1].TangentIn, 5);
        Assert.Equal(20f, curve.Keys[1].TangentOut, 5);
    }

    [Fact]
    public void ComputeTangents_Flat_ZeroesBothSides()
    {
        Curve curve = TwoKeyLine();
        curve.Keys[0].TangentIn = 5f;
        curve.Keys[0].TangentOut = 5f;

        curve.ComputeTangents(CurveTangent.Flat);

        Assert.Equal(0f, curve.Keys[0].TangentIn);
        Assert.Equal(0f, curve.Keys[0].TangentOut);
    }

    /// <summary>An endpoint substitutes itself for its missing neighbour, which must yield a flat
    /// tangent rather than a division by zero.</summary>
    [Fact]
    public void ComputeTangents_Endpoints_AreFlat()
    {
        Curve curve = TwoKeyLine();
        curve.ComputeTangents(CurveTangent.Smooth);

        // Asserts the actual value, not just "not NaN" -- the earlier version of this test passed
        // for an implementation returning any arbitrary number.
        Assert.Equal(0f, curve.Keys[0].TangentIn);
        Assert.Equal(0f, curve.Keys[1].TangentOut);
    }

    /// <summary>
    /// The one test that actually pins the Hermite basis. Hand-computed: at the segment midpoint
    /// the basis weights are h00=0.5, h10=0.125, h01=0.5, h11=-0.125, and the tangent terms take
    /// the *start* key's <c>TangentOut</c> and the *end* key's <c>TangentIn</c> --
    /// 0.5·0 + 0.125·4 + 0.5·10 + (-0.125)·(-2) = 5.75.
    ///
    /// The other two tangents are set to unrelated values so the assertion discriminates: swapping
    /// in/out yields 7.75, and rescaling the tangent terms by the segment duration (2 here) yields
    /// 6.5. Both were previously undetectable.
    /// </summary>
    [Fact]
    public void Evaluate_AsymmetricNonZeroTangents_MatchesHandComputedHermiteValue()
    {
        var curve = new Curve();
        curve.Keys.Add(new CurveKey(0f, 0f, 99f, 4f));
        curve.Keys.Add(new CurveKey(2f, 10f, -2f, 77f));

        Assert.Equal(5.75f, curve.Evaluate(1f), 4);
    }

    private static Curve FiveKeyRamp()
    {
        var curve = new Curve();
        for (int i = 0; i <= 4; i++)
        {
            curve.Keys.Add(new CurveKey(i, i * 10f));
        }

        return curve;
    }

    /// <summary>Exercises the binary search with enough keys that its loop body actually runs --
    /// with only two keys it never does, so a <c>return 0;</c> stub was indistinguishable.</summary>
    [Theory]
    [InlineData(0.5f, 5f)]
    [InlineData(1.5f, 15f)]
    [InlineData(2.5f, 25f)]
    [InlineData(3.5f, 35f)]
    public void Evaluate_FiveKeys_PicksTheRightSegment(float position, float expected) =>
        Assert.Equal(expected, FiveKeyRamp().Evaluate(position), 4);

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(1f, 10f)]
    [InlineData(2f, 20f)]
    [InlineData(3f, 30f)]
    [InlineData(4f, 40f)]
    public void Evaluate_FiveKeys_AtEachKeyPosition_ReturnsThatKeysValue(float position, float expected) =>
        Assert.Equal(expected, FiveKeyRamp().Evaluate(position), 4);

    /// <summary>A curve whose range does not start at zero. Every earlier loop-mode test spanned
    /// [0,1], so an implementation that dropped the <c>rangeStart</c> offset when folding a
    /// position back into range passed them all.</summary>
    private static Curve OffsetRange()
    {
        var curve = new Curve();
        curve.Keys.Add(new CurveKey(5f, 0f));
        curve.Keys.Add(new CurveKey(7f, 10f));
        return curve;
    }

    [Fact]
    public void Evaluate_CycleLoop_OnAnOffsetRange_FoldsRelativeToRangeStart()
    {
        Curve curve = OffsetRange();
        curve.PostLoop = CurveLoopType.Cycle;
        curve.PreLoop = CurveLoopType.Cycle;

        Assert.Equal(curve.Evaluate(6f), curve.Evaluate(8f), 4);
        Assert.Equal(curve.Evaluate(6f), curve.Evaluate(4f), 4);
    }

    [Fact]
    public void Evaluate_CycleOffsetLoop_OnAnOffsetRange_AddsOneRangeDeltaPerCycle()
    {
        Curve curve = OffsetRange();
        curve.PostLoop = CurveLoopType.CycleOffset;

        Assert.Equal(curve.Evaluate(6f) + 10f, curve.Evaluate(8f), 4);
    }

    /// <summary>Picks a position where oscillate and cycle genuinely disagree -- at a whole-cycle
    /// offset they coincide, so a test there would not tell the two apart.</summary>
    [Fact]
    public void Evaluate_OscillateLoop_OnAnOffsetRange_MirrorsRatherThanWraps()
    {
        Curve oscillate = OffsetRange();
        oscillate.PostLoop = CurveLoopType.Oscillate;

        Curve cycle = OffsetRange();
        cycle.PostLoop = CurveLoopType.Cycle;

        // 7.5 is 0.5 past the end: oscillate folds back to 6.5, cycle wraps to 5.5.
        Assert.Equal(oscillate.Evaluate(6.5f), oscillate.Evaluate(7.5f), 4);
        Assert.Equal(cycle.Evaluate(5.5f), cycle.Evaluate(7.5f), 4);
        Assert.NotEqual(oscillate.Evaluate(7.5f), cycle.Evaluate(7.5f), 4);
    }

    [Fact]
    public void Evaluate_ConstantLoop_OnAnOffsetRange_ClampsToEndpoints()
    {
        Curve curve = OffsetRange();

        Assert.Equal(0f, curve.Evaluate(1f), 4);
        Assert.Equal(10f, curve.Evaluate(99f), 4);
    }

    [Fact]
    public void Keys_StaySortedByPosition_RegardlessOfInsertionOrder()
    {
        var curve = new Curve();
        curve.Keys.Add(new CurveKey(5f, 1f));
        curve.Keys.Add(new CurveKey(1f, 2f));
        curve.Keys.Add(new CurveKey(3f, 3f));

        Assert.Equal([1f, 3f, 5f], curve.Keys.Select(k => k.Position));
    }

    [Fact]
    public void Keys_Add_DuplicatePosition_Throws()
    {
        var curve = new Curve();
        curve.Keys.Add(new CurveKey(1f, 1f));

        Assert.Throws<InvalidOperationException>(() => curve.Keys.Add(new CurveKey(1f, 2f)));
    }

    [Fact]
    public void Keys_Indexer_RejectsAKeyAtADifferentPosition()
    {
        var curve = new Curve();
        curve.Keys.Add(new CurveKey(1f, 1f));

        Assert.Throws<InvalidOperationException>(() => curve.Keys[0] = new CurveKey(2f, 1f));
    }

    [Fact]
    public void Clone_IsDeep_SoMutatingTheCloneLeavesTheOriginalAlone()
    {
        Curve curve = TwoKeyLine();
        Curve clone = curve.Clone();

        clone.Keys[0].Value = 99f;
        clone.PostLoop = CurveLoopType.Oscillate;

        Assert.Equal(0f, curve.Keys[0].Value);
        Assert.Equal(CurveLoopType.Constant, curve.PostLoop);
    }

    /// <summary>Real XNA orders keys by <see cref="CurveKey.Position"/> alone while
    /// <see cref="CurveKey.Equals(CurveKey)"/> compares every field. The asymmetry is deliberate
    /// (see that type's doc comment), so it is pinned rather than left to look like a bug.</summary>
    [Fact]
    public void CurveKey_CompareTo_IgnoresEverythingButPosition()
    {
        var a = new CurveKey(1f, 10f);
        var b = new CurveKey(1f, 20f);

        Assert.Equal(0, a.CompareTo(b));
        Assert.NotEqual(a, b);
    }
}
