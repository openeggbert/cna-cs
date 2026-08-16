namespace CNA.Graphics;

/// <summary>Matches real XNA 4.0's <c>PrimitiveType</c> exactly (XNA 4 dropped the earlier
/// <c>PointList</c>/<c>PointList_WithinTriangleList</c> members XNA 3.1 had).</summary>
public enum PrimitiveType
{
    TriangleList = 0,
    TriangleStrip = 1,
    LineList = 2,
    LineStrip = 3,
}
