namespace Microsoft.Xna.Framework;

/// <summary>XNA 4.0-compatible keyframed scalar curve.</summary>
public class Curve
{
    private const float TangentEpsilon = 1.1920929E-07f;
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

    public void ComputeTangent(int keyIndex, CurveTangent tangentType) =>
        ComputeTangent(keyIndex, tangentType, tangentType);

    public void ComputeTangent(int keyIndex, CurveTangent tangentInType, CurveTangent tangentOutType)
    {
        if (Keys.Count <= keyIndex || keyIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(keyIndex));
        }

        CurveKey key = Keys[keyIndex];
        float previousPosition = key.Position;
        float previousValue = key.Value;
        float nextPosition = key.Position;
        float nextValue = key.Value;

        if (keyIndex > 0)
        {
            previousPosition = Keys[keyIndex - 1].Position;
            previousValue = Keys[keyIndex - 1].Value;
        }

        if (keyIndex + 1 < Keys.Count)
        {
            nextPosition = Keys[keyIndex + 1].Position;
            nextValue = Keys[keyIndex + 1].Value;
        }

        if (tangentInType == CurveTangent.Smooth)
        {
            float positionSpan = nextPosition - previousPosition;
            float valueSpan = nextValue - previousValue;
            key.TangentIn = Math.Abs(valueSpan) < TangentEpsilon
                ? 0f
                : valueSpan * Math.Abs(previousPosition - key.Position) / positionSpan;
        }
        else if (tangentInType == CurveTangent.Linear)
        {
            key.TangentIn = key.Value - previousValue;
        }
        else
        {
            key.TangentIn = 0f;
        }

        if (tangentOutType == CurveTangent.Smooth)
        {
            float positionSpan = nextPosition - previousPosition;
            float valueSpan = nextValue - previousValue;
            key.TangentOut = Math.Abs(valueSpan) < TangentEpsilon
                ? 0f
                : valueSpan * Math.Abs(nextPosition - key.Position) / positionSpan;
        }
        else if (tangentOutType == CurveTangent.Linear)
        {
            key.TangentOut = nextValue - key.Value;
        }
        else
        {
            key.TangentOut = 0f;
        }
    }

    public void ComputeTangents(CurveTangent tangentType) => ComputeTangents(tangentType, tangentType);

    public void ComputeTangents(CurveTangent tangentInType, CurveTangent tangentOutType)
    {
        for (int i = 0; i < Keys.Count; i++)
        {
            ComputeTangent(i, tangentInType, tangentOutType);
        }
    }

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
        float virtualPosition = position;
        float valueOffset = 0f;

        if (virtualPosition < first.Position)
        {
            if (PreLoop == CurveLoopType.Constant)
            {
                return first.Value;
            }

            if (PreLoop == CurveLoopType.Linear)
            {
                return first.Value - (first.TangentIn * (first.Position - virtualPosition));
            }

            float cycle = CalculateCycle(virtualPosition);
            float cyclePosition = virtualPosition - (first.Position + (cycle * Keys.TimeRange));
            if (PreLoop == CurveLoopType.Cycle)
            {
                virtualPosition = first.Position + cyclePosition;
            }
            else if (PreLoop == CurveLoopType.CycleOffset)
            {
                virtualPosition = first.Position + cyclePosition;
                valueOffset = (last.Value - first.Value) * cycle;
            }
            else
            {
                // XNA routes Oscillate and undefined enum values through the same path.
                virtualPosition = (((int)cycle & 1) == 0)
                    ? first.Position + cyclePosition
                    : last.Position - cyclePosition;
            }
        }
        else if (last.Position < virtualPosition)
        {
            if (PostLoop == CurveLoopType.Constant)
            {
                return last.Value;
            }

            if (PostLoop == CurveLoopType.Linear)
            {
                return last.Value - (last.TangentOut * (last.Position - virtualPosition));
            }

            float cycle = CalculateCycle(virtualPosition);
            float cyclePosition = virtualPosition - (first.Position + (cycle * Keys.TimeRange));
            if (PostLoop == CurveLoopType.Cycle)
            {
                virtualPosition = first.Position + cyclePosition;
            }
            else if (PostLoop == CurveLoopType.CycleOffset)
            {
                virtualPosition = first.Position + cyclePosition;
                valueOffset = (last.Value - first.Value) * cycle;
            }
            else
            {
                virtualPosition = (((int)cycle & 1) == 0)
                    ? first.Position + cyclePosition
                    : last.Position - cyclePosition;
            }
        }

        float amount = FindSegment(virtualPosition, out CurveKey start, out CurveKey end);
        return valueOffset + Hermite(start, end, amount);
    }

    private float CalculateCycle(float position)
    {
        float cycle = (position - Keys[0].Position) * Keys.InverseTimeRange;
        if (cycle < 0f)
        {
            cycle -= 1f;
        }

        return (int)cycle;
    }

    private float FindSegment(float position, out CurveKey start, out CurveKey end)
    {
        float amount = position;
        start = Keys[0];
        end = null!;

        for (int i = 1; i < Keys.Count; i++)
        {
            end = Keys[i];
            if (end.Position >= position)
            {
                double startPosition = start.Position;
                double endPosition = end.Position;
                double targetPosition = position;
                double positionSpan = endPosition - startPosition;
                amount = 0f;
                if (positionSpan > 1e-10)
                {
                    amount = (float)((targetPosition - startPosition) / positionSpan);
                }

                return amount;
            }

            start = end;
        }

        return amount;
    }

    private static float Hermite(CurveKey start, CurveKey end, float amount)
    {
        if (start.Continuity == CurveContinuity.Step)
        {
            return amount < 1f ? start.Value : end.Value;
        }

        float amountSquared = amount * amount;
        float amountCubed = amountSquared * amount;
        float startValue = start.Value;
        float endValue = end.Value;
        float startTangent = start.TangentOut;
        float endTangent = end.TangentIn;

        return (startValue * (((2f * amountCubed) - (3f * amountSquared)) + 1f))
            + (endValue * ((-2f * amountCubed) + (3f * amountSquared)))
            + (startTangent * ((amountCubed - (2f * amountSquared)) + amount))
            + (endTangent * (amountCubed - amountSquared));
    }
}
