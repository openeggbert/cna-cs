namespace Microsoft.Xna.Framework;

/// <summary>
/// Internal XNA 4.0 Gilbert-Johnson-Keerthi simplex implementation used by
/// <see cref="BoundingFrustum"/> intersection tests.
/// </summary>
internal sealed class Gjk
{
    private static readonly int[] BitsToIndices =
    [
        0, 1, 2, 17, 3, 25, 26, 209, 4, 33, 34, 273, 35, 281, 282, 2257,
    ];

    private Vector3 _closestPoint;
    private readonly Vector3[] _y = new Vector3[4];
    private readonly float[] _yLengthSquared = new float[4];
    private readonly Vector3[][] _edges =
    [
        new Vector3[4],
        new Vector3[4],
        new Vector3[4],
        new Vector3[4],
    ];
    private readonly float[][] _edgeLengthSquared =
    [
        new float[4],
        new float[4],
        new float[4],
        new float[4],
    ];
    private readonly float[][] _determinants = CreateDeterminants();
    private int _simplexBits;
    private float _maxLengthSquared;

    public bool FullSimplex => _simplexBits == 15;

    public float MaxLengthSquared => _maxLengthSquared;

    public Vector3 ClosestPoint => _closestPoint;

    public void Reset()
    {
        _simplexBits = 0;
        _maxLengthSquared = 0f;
    }

    public bool AddSupportPoint(ref Vector3 newPoint)
    {
        int newIndex = (BitsToIndices[_simplexBits ^ 15] & 7) - 1;
        _y[newIndex] = newPoint;
        _yLengthSquared[newIndex] = newPoint.LengthSquared();

        for (int indices = BitsToIndices[_simplexBits]; indices != 0; indices >>= 3)
        {
            int index = (indices & 7) - 1;
            Vector3 edge = _y[index] - newPoint;
            _edges[index][newIndex] = edge;
            _edges[newIndex][index] = -edge;
            _edgeLengthSquared[newIndex][index] =
                _edgeLengthSquared[index][newIndex] = edge.LengthSquared();
        }

        UpdateDeterminant(newIndex);
        return UpdateSimplex(newIndex);
    }

    private static float[][] CreateDeterminants()
    {
        var result = new float[16][];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new float[4];
        }

        return result;
    }

    private static float Dot(ref Vector3 a, ref Vector3 b) =>
        (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    private void UpdateDeterminant(int newIndex)
    {
        int newBit = 1 << newIndex;
        _determinants[newBit][newIndex] = 1f;
        int allIndices = BitsToIndices[_simplexBits];
        int remainingIndices = allIndices;
        int priorCount = 0;

        while (remainingIndices != 0)
        {
            int index = (remainingIndices & 7) - 1;
            int indexBit = 1 << index;
            int pairBits = indexBit | newBit;
            _determinants[pairBits][index] = Dot(ref _edges[newIndex][index], ref _y[newIndex]);
            _determinants[pairBits][newIndex] = Dot(ref _edges[index][newIndex], ref _y[index]);

            int earlierIndices = allIndices;
            for (int i = 0; i < priorCount; i++)
            {
                int earlierIndex = (earlierIndices & 7) - 1;
                int earlierBit = 1 << earlierIndex;
                int tripleBits = pairBits | earlierBit;

                int edgeIndex = _edgeLengthSquared[index][earlierIndex] <
                    _edgeLengthSquared[newIndex][earlierIndex] ? index : newIndex;
                _determinants[tripleBits][earlierIndex] =
                    (_determinants[pairBits][index] * Dot(ref _edges[edgeIndex][earlierIndex], ref _y[index])) +
                    (_determinants[pairBits][newIndex] * Dot(ref _edges[edgeIndex][earlierIndex], ref _y[newIndex]));

                edgeIndex = _edgeLengthSquared[earlierIndex][index] <
                    _edgeLengthSquared[newIndex][index] ? earlierIndex : newIndex;
                _determinants[tripleBits][index] =
                    (_determinants[earlierBit | newBit][earlierIndex] * Dot(ref _edges[edgeIndex][index], ref _y[earlierIndex])) +
                    (_determinants[earlierBit | newBit][newIndex] * Dot(ref _edges[edgeIndex][index], ref _y[newIndex]));

                edgeIndex = _edgeLengthSquared[index][newIndex] <
                    _edgeLengthSquared[earlierIndex][newIndex] ? index : earlierIndex;
                _determinants[tripleBits][newIndex] =
                    (_determinants[indexBit | earlierBit][earlierIndex] * Dot(ref _edges[edgeIndex][newIndex], ref _y[earlierIndex])) +
                    (_determinants[indexBit | earlierBit][index] * Dot(ref _edges[edgeIndex][newIndex], ref _y[index]));

                earlierIndices >>= 3;
            }

            remainingIndices >>= 3;
            priorCount++;
        }

        if ((_simplexBits | newBit) != 15)
        {
            return;
        }

        int selected = !(_edgeLengthSquared[1][0] < _edgeLengthSquared[2][0])
            ? (_edgeLengthSquared[2][0] < _edgeLengthSquared[3][0] ? 2 : 3)
            : (_edgeLengthSquared[1][0] < _edgeLengthSquared[3][0] ? 1 : 3);
        _determinants[15][0] =
            (_determinants[14][1] * Dot(ref _edges[selected][0], ref _y[1])) +
            (_determinants[14][2] * Dot(ref _edges[selected][0], ref _y[2])) +
            (_determinants[14][3] * Dot(ref _edges[selected][0], ref _y[3]));

        selected = !(_edgeLengthSquared[0][1] < _edgeLengthSquared[2][1])
            ? (_edgeLengthSquared[2][1] < _edgeLengthSquared[3][1] ? 2 : 3)
            : (!(_edgeLengthSquared[0][1] < _edgeLengthSquared[3][1]) ? 3 : 0);
        _determinants[15][1] =
            (_determinants[13][0] * Dot(ref _edges[selected][1], ref _y[0])) +
            (_determinants[13][2] * Dot(ref _edges[selected][1], ref _y[2])) +
            (_determinants[13][3] * Dot(ref _edges[selected][1], ref _y[3]));

        selected = !(_edgeLengthSquared[0][2] < _edgeLengthSquared[1][2])
            ? (_edgeLengthSquared[1][2] < _edgeLengthSquared[3][2] ? 1 : 3)
            : (!(_edgeLengthSquared[0][2] < _edgeLengthSquared[3][2]) ? 3 : 0);
        _determinants[15][2] =
            (_determinants[11][0] * Dot(ref _edges[selected][2], ref _y[0])) +
            (_determinants[11][1] * Dot(ref _edges[selected][2], ref _y[1])) +
            (_determinants[11][3] * Dot(ref _edges[selected][2], ref _y[3]));

        selected = !(_edgeLengthSquared[0][3] < _edgeLengthSquared[1][3])
            ? (_edgeLengthSquared[1][3] < _edgeLengthSquared[2][3] ? 1 : 2)
            : (!(_edgeLengthSquared[0][3] < _edgeLengthSquared[2][3]) ? 2 : 0);
        _determinants[15][3] =
            (_determinants[7][0] * Dot(ref _edges[selected][3], ref _y[0])) +
            (_determinants[7][1] * Dot(ref _edges[selected][3], ref _y[1])) +
            (_determinants[7][2] * Dot(ref _edges[selected][3], ref _y[2]));
    }

    private bool UpdateSimplex(int newIndex)
    {
        int allBits = _simplexBits | (1 << newIndex);
        int newBit = 1 << newIndex;

        for (int bits = _simplexBits; bits != 0; bits--)
        {
            if ((bits & allBits) == bits && IsSatisfiesRule(bits | newBit, allBits))
            {
                _simplexBits = bits | newBit;
                _closestPoint = ComputeClosestPoint();
                return true;
            }
        }

        if (!IsSatisfiesRule(newBit, allBits))
        {
            return false;
        }

        _simplexBits = newBit;
        _closestPoint = _y[newIndex];
        _maxLengthSquared = _yLengthSquared[newIndex];
        return true;
    }

    private Vector3 ComputeClosestPoint()
    {
        float determinantSum = 0f;
        Vector3 result = Vector3.Zero;
        _maxLengthSquared = 0f;

        for (int indices = BitsToIndices[_simplexBits]; indices != 0; indices >>= 3)
        {
            int index = (indices & 7) - 1;
            float determinant = _determinants[_simplexBits][index];
            determinantSum += determinant;
            result += _y[index] * determinant;
            _maxLengthSquared = MathHelper.Max(_maxLengthSquared, _yLengthSquared[index]);
        }

        return result / determinantSum;
    }

    private bool IsSatisfiesRule(int candidateBits, int allBits)
    {
        for (int indices = BitsToIndices[allBits]; indices != 0; indices >>= 3)
        {
            int index = (indices & 7) - 1;
            int bit = 1 << index;
            if ((bit & candidateBits) != 0)
            {
                if (_determinants[candidateBits][index] <= 0f)
                {
                    return false;
                }
            }
            else if (_determinants[candidateBits | bit][index] > 0f)
            {
                return false;
            }
        }

        return true;
    }
}
