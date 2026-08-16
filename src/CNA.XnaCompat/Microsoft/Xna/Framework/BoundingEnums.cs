namespace Microsoft.Xna.Framework;

// Member order intentionally matches CNA.ContainmentType / PlaneIntersectionType
// exactly, so a plain numeric cast converts between them.

public enum ContainmentType
{
    Disjoint,
    Contains,
    Intersects,
}

public enum PlaneIntersectionType
{
    Front,
    Back,
    Intersecting,
}

internal static class BoundingEnumConversions
{
    public static ContainmentType ToCompat(this CNA.ContainmentType value) => (ContainmentType)(int)value;

    public static PlaneIntersectionType ToCompat(this CNA.PlaneIntersectionType value) => (PlaneIntersectionType)(int)value;
}
