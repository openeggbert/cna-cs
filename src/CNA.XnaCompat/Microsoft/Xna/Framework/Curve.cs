namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible keyframed scalar curve.</summary>
public class Curve
{
    private readonly CurveKeyCollection _keys;

    public Curve()
    {
        _keys = new CurveKeyCollection();
    }

    private Curve(CurveKeyCollection keys)
    {
        _keys = keys;
    }

    public CurveKeyCollection Keys => _keys;

    public CurveLoopType PreLoop { get; set; }

    public CurveLoopType PostLoop { get; set; }

    public bool IsConstant => Keys.Count <= 1;

    public Curve Clone() => new(Keys.Clone())
    {
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
        CurveKey previous = keyIndex > 0 ? Keys[keyIndex - 1] : key;
        CurveKey next = keyIndex < Keys.Count - 1 ? Keys[keyIndex + 1] : key;

        key.TangentIn = ComputeTangentIn(tangentInType, previous, key, next);
        key.TangentOut = ComputeTangentOut(tangentOutType, previous, key, next);
    }

    private float EvaluateOutside(float position, CurveLoopType loop, bool before, CurveKey first, CurveKey last)
    {
        float rangeStart = first.Position;
        float rangeEnd = last.Position;
        float range = rangeEnd - rangeStart;

        return loop switch
        {
            CurveLoopType.Constant => before ? first.Value : last.Value,
            CurveLoopType.Linear => before
                ? first.Value - (first.TangentIn * (rangeStart - position))
                : last.Value + (last.TangentOut * (position - rangeEnd)),
            CurveLoopType.Cycle => EvaluateInside(rangeStart + Modulo(position - rangeStart, range)),
            CurveLoopType.CycleOffset =>
                EvaluateInside(rangeStart + Modulo(position - rangeStart, range)) +
                (CycleCount(position, rangeStart, range) * (last.Value - first.Value)),
            CurveLoopType.Oscillate => EvaluateOscillating(position, rangeStart, range),
            _ => before ? first.Value : last.Value,
        };
    }

    private float EvaluateOscillating(float position, float rangeStart, float range)
    {
        float offset = Modulo(position - rangeStart, range * 2f);
        float folded = offset <= range ? offset : (range * 2f) - offset;
        return EvaluateInside(rangeStart + folded);
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

        return (((2f * t3) - (3f * t2) + 1f) * start.Value)
            + ((t3 - (2f * t2) + t) * start.TangentOut)
            + (((-2f * t3) + (3f * t2)) * end.Value)
            + ((t3 - t2) * end.TangentIn);
    }

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

    private static float CycleCount(float position, float rangeStart, float range) =>
        (float)Math.Floor((position - rangeStart) / range);

    private static float Modulo(float value, float modulus)
    {
        float result = value % modulus;
        return result < 0f ? result + modulus : result;
    }

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

    private static float SmoothTangent(CurveKey previous, CurveKey next, float sideSpan)
    {
        float totalSpan = next.Position - previous.Position;
        return Math.Abs(totalSpan) < float.Epsilon
            ? 0f
            : (next.Value - previous.Value) * Math.Abs(sideSpan / totalSpan);
    }
}
