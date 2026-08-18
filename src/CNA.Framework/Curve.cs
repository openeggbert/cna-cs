namespace CNA;

/// <summary>
/// Matches real XNA's <c>Curve</c>: a keyframed scalar animation curve evaluated with cubic
/// Hermite interpolation between neighbouring <see cref="CurveKey"/>s, with configurable
/// extrapolation outside the key range (<see cref="PreLoop"/>/<see cref="PostLoop"/>).
///
/// <b>Implemented in managed code, not bound to native.</b> The real C API does have a complete
/// <c>Curve</c> object (<c>curve.h</c>: <c>cna_curve_create</c>/<c>_evaluate</c>/
/// <c>_compute_tangents</c>/<c>_destroy</c>, plus a whole <c>cna_curve_key_collection_*</c>
/// surface), so this was a genuine choice rather than a fallback -- <c>plan.md</c> WP8 asked for
/// the decision to be made by reading the header and then recorded. Two reasons decided it:
///
/// <list type="number">
/// <item>Design invariant #3: this is pure math with no GPU or platform resource behind it, and
/// every other math type in this project (<see cref="Matrix"/>, <see cref="BoundingFrustum"/>,
/// <see cref="Quaternion"/>) is likewise managed. A curve evaluation is not a device
/// operation.</item>
/// <item>It is therefore <em>testable</em>. Native-backed types in this project cannot be
/// exercised at all without a real <c>cna-native</c>, so binding this would have traded a fully
/// unit-tested implementation for an untestable one -- see this type's own test file, which pins
/// the Hermite basis and all five loop modes.</item>
/// </list>
///
/// The evaluation semantics below are real XNA's, reproduced deliberately: Hermite basis
/// <c>(2t³-3t²+1)p₀ + (t³-2t²+t)m₀ + (-2t³+3t²)p₁ + (t³-t²)m₁</c> applied to the tangents
/// verbatim (XNA's <see cref="ComputeTangents(CurveTangent)"/> already expresses them as a value
/// delta across the segment, so they are not rescaled again at evaluation time), and
/// <see cref="CurveContinuity.Step"/> on the *starting* key of a segment holding that key's value
/// across the whole segment.
/// </summary>
public class Curve
{
    public CurveKeyCollection Keys { get; private set; } = new();

    public CurveLoopType PreLoop { get; set; }

    public CurveLoopType PostLoop { get; set; }

    /// <summary>True when the curve has fewer than two keys, so every
    /// <see cref="Evaluate"/> answers the same value regardless of input. Matches real
    /// XNA.</summary>
    public bool IsConstant => Keys.Count <= 1;

    public Curve Clone() => new()
    {
        Keys = Keys.Clone(),
        PreLoop = PreLoop,
        PostLoop = PostLoop,
    };

    public float Evaluate(float position)
    {
        if (Keys.Count == 0)
        {
            return 0f;
        }

        if (Keys.Count == 1)
        {
            return Keys[0].Value;
        }

        CurveKey first = Keys[0];
        CurveKey last = Keys[Keys.Count - 1];

        if (position < first.Position)
        {
            return EvaluateOutside(position, PreLoop, before: true, first, last);
        }

        if (position > last.Position)
        {
            return EvaluateOutside(position, PostLoop, before: false, first, last);
        }

        return EvaluateInside(position);
    }

    /// <summary>Extrapolation outside the key range. <see cref="CurveLoopType.Cycle"/> and
    /// <see cref="CurveLoopType.Oscillate"/> map <paramref name="position"/> back inside the range
    /// and re-evaluate; <see cref="CurveLoopType.CycleOffset"/> does the same and then adds a
    /// whole-cycle value delta per elapsed cycle; <see cref="CurveLoopType.Linear"/> extrapolates
    /// along the boundary key's own tangent.</summary>
    private float EvaluateOutside(float position, CurveLoopType loop, bool before, CurveKey first, CurveKey last)
    {
        float rangeStart = first.Position;
        float rangeEnd = last.Position;
        float range = rangeEnd - rangeStart;

        switch (loop)
        {
            case CurveLoopType.Constant:
                return before ? first.Value : last.Value;

            case CurveLoopType.Linear:
                return before
                    ? first.Value - (first.TangentIn * (rangeStart - position))
                    : last.Value + (last.TangentOut * (position - rangeEnd));

            case CurveLoopType.Cycle:
                return EvaluateInside(rangeStart + Modulo(position - rangeStart, range));

            case CurveLoopType.CycleOffset:
            {
                float cycles = CycleCount(position, rangeStart, range);
                return EvaluateInside(rangeStart + Modulo(position - rangeStart, range))
                    + (cycles * (last.Value - first.Value));
            }

            case CurveLoopType.Oscillate:
            {
                float offset = Modulo(position - rangeStart, range * 2f);
                float folded = offset <= range ? offset : (range * 2f) - offset;
                return EvaluateInside(rangeStart + folded);
            }

            default:
                return before ? first.Value : last.Value;
        }
    }

    /// <summary>Number of whole cycles <paramref name="position"/> lies beyond the key range,
    /// negative when it lies before it. Uses <see cref="Math.Floor(double)"/> rather than a
    /// truncating cast so that negative positions round away from zero, which is what makes
    /// <see cref="CurveLoopType.CycleOffset"/> symmetric on both sides.</summary>
    private static float CycleCount(float position, float rangeStart, float range) =>
        (float)Math.Floor((position - rangeStart) / range);

    /// <summary>Always returns a value in <c>[0, modulus)</c>, unlike C#'s <c>%</c>, which keeps
    /// the dividend's sign and would send positions before the range to the wrong end of
    /// it.</summary>
    private static float Modulo(float value, float modulus)
    {
        float result = value % modulus;
        return result < 0f ? result + modulus : result;
    }

    private float EvaluateInside(float position)
    {
        int index = FindSegment(position);
        CurveKey start = Keys[index];
        CurveKey end = Keys[index + 1];

        if (start.Continuity == CurveContinuity.Step)
        {
            return position >= end.Position ? end.Value : start.Value;
        }

        float duration = end.Position - start.Position;
        if (duration == 0f)
        {
            return start.Value;
        }

        float t = (position - start.Position) / duration;
        float t2 = t * t;
        float t3 = t2 * t;

        // Cubic Hermite basis, matching real XNA exactly. The tangents are NOT rescaled by the
        // segment duration here: XNA's ComputeTangents already produces them as a value delta
        // across the segment (see ComputeTangentIn/Out below), so scaling again would square the
        // effect of every computed tangent.
        return (((2f * t3) - (3f * t2) + 1f) * start.Value)
            + ((t3 - (2f * t2) + t) * start.TangentOut)
            + (((-2f * t3) + (3f * t2)) * end.Value)
            + ((t3 - t2) * end.TangentIn);
    }

    /// <summary>Index of the key starting the segment containing <paramref name="position"/>.
    /// Binary search, which is why <see cref="CurveKeyCollection"/> maintains its sort
    /// order.</summary>
    private int FindSegment(float position)
    {
        int low = 0;
        int high = Keys.Count - 1;

        while (low < high - 1)
        {
            int middle = (low + high) / 2;
            if (Keys[middle].Position <= position)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    public void ComputeTangents(CurveTangent tangentType) => ComputeTangents(tangentType, tangentType);

    public void ComputeTangents(CurveTangent tangentInType, CurveTangent tangentOutType)
    {
        for (int i = 0; i < Keys.Count; i++)
        {
            ComputeTangent(i, tangentInType, tangentOutType);
        }
    }

    public void ComputeTangent(int keyIndex, CurveTangent tangentType) =>
        ComputeTangent(keyIndex, tangentType, tangentType);

    public void ComputeTangent(int keyIndex, CurveTangent tangentInType, CurveTangent tangentOutType)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(keyIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(keyIndex, Keys.Count);

        CurveKey key = Keys[keyIndex];

        // An endpoint has no neighbour on one side; XNA substitutes the key itself, which makes
        // that side's delta zero rather than needing a special case per tangent type.
        CurveKey previous = keyIndex > 0 ? Keys[keyIndex - 1] : key;
        CurveKey next = keyIndex < Keys.Count - 1 ? Keys[keyIndex + 1] : key;

        key.TangentIn = ComputeTangentIn(tangentInType, previous, key, next);
        key.TangentOut = ComputeTangentOut(tangentOutType, previous, key, next);
    }

    /// <summary>Real XNA computes the incoming and outgoing tangents with *different* formulas for
    /// both <see cref="CurveTangent.Linear"/> and <see cref="CurveTangent.Smooth"/> -- the
    /// incoming one looks backwards to <paramref name="previous"/>, the outgoing one forwards to
    /// <paramref name="next"/> -- which is why this is two methods rather than one shared
    /// helper.</summary>
    private static float ComputeTangentIn(CurveTangent type, CurveKey previous, CurveKey key, CurveKey next) =>
        type switch
        {
            CurveTangent.Linear => key.Value - previous.Value,
            CurveTangent.Smooth => SmoothTangent(previous, next, key.Position - previous.Position),
            _ => 0f,
        };

    private static float ComputeTangentOut(CurveTangent type, CurveKey previous, CurveKey key, CurveKey next) =>
        type switch
        {
            CurveTangent.Linear => next.Value - key.Value,
            CurveTangent.Smooth => SmoothTangent(previous, next, next.Position - key.Position),
            _ => 0f,
        };

    /// <summary>The smooth tangent weights the whole <paramref name="previous"/>-to-
    /// <paramref name="next"/> value change by this side's share of that span. Guards a zero span
    /// (both neighbours at one position, or an endpoint whose substituted neighbour is the key
    /// itself) so it yields a flat tangent instead of dividing by zero.</summary>
    private static float SmoothTangent(CurveKey previous, CurveKey next, float sideSpan)
    {
        float totalSpan = next.Position - previous.Position;
        if (Math.Abs(totalSpan) < float.Epsilon)
        {
            return 0f;
        }

        return (next.Value - previous.Value) * Math.Abs(sideSpan / totalSpan);
    }
}
