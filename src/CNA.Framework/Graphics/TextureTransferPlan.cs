using CNA.Interop;

namespace CNA.Graphics;

/// <summary>
/// How one <c>SetData</c>/<c>GetData</c> call reaches the native transfer: which
/// <c>CNA_TextureDataType</c> tag to use, and the caller's window restated in that tag's elements.
///
/// <b>Why this exists.</b> The ABI's transfer routes are tagged: a tag names one element type, and
/// the native side reads or writes one of those per texel, converting from the surface format. That
/// maps cleanly onto <c>GetData&lt;Color&gt;</c>, and this binding used to pick the tag from
/// <c>T</c> -- throwing <see cref="NotSupportedException"/> when the tag list had no name for it.
///
/// XNA does not work that way. Its transfer is an untyped byte copy: it moves the surface's own
/// bytes and uses <c>T</c> only for a size check. Three consequences, and ported games rely on all
/// three:
///
/// <list type="bullet">
/// <item><c>GetData&lt;uint&gt;</c> on a <see cref="SurfaceFormat.Color"/> texture is an ordinary
/// idiom, and refusing it was a real incompatibility rather than caution.</item>
/// <item><c>GetData&lt;byte&gt;</c> on that texture reads <em>four bytes per texel</em>, not one
/// channel. Picking the tag from <c>T</c> got this actively wrong: it asked the native side to
/// convert each texel to a single byte.</item>
/// <item>a mismatched element type is a size error, not a conversion. XNA never silently converts
/// a surface to a different format on the way out.</item>
/// </list>
///
/// So the tag is taken from the texture's <em>own surface format</em>, always. Source and
/// destination formats are then the same, the native side converts nothing, and the bytes that
/// cross are the surface's bytes -- which is what XNA copies. <c>T</c> contributes only its size,
/// used to restate the window in texels.
///
/// The size rule is not invented here. <c>cna_texture_validate_get_data_format</c> requires the
/// format's unit to be a whole multiple of the element size, which is the rule XNA states as "the
/// type used for the destination element is an invalid size for this resource". A
/// <see cref="Vector4"/> read of a Color surface still fails, and should.
/// </summary>
internal readonly record struct TextureTransferPlan(
    uint DataType,
    ulong StartIndex,
    ulong ElementCount,
    ulong Capacity)
{
    /// <summary>
    /// Builds the plan for a window of <paramref name="elementCount"/> elements of
    /// <paramref name="elementSize"/> bytes starting at <paramref name="startIndex"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">When the surface format has no transfer tag.</exception>
    /// <exception cref="ArgumentException">When the window does not start and end on a whole texel.
    /// XNA reports the same shape of mistake as an <see cref="ArgumentException"/> about the size of
    /// the data passed in.</exception>
    internal static TextureTransferPlan Create(
        SurfaceFormat format,
        int elementSize,
        int startIndex,
        int elementCount,
        int arrayLength)
    {
        (uint tag, int unitBytes) = TextureDataType.ForSurfaceFormat(format);
        return Restate(tag, unitBytes, format.ToString(), elementSize, startIndex, elementCount, arrayLength);
    }

    /// <summary>
    /// The same restatement for the routes whose ABI shape is fixed at one <c>CNA_Color</c> per
    /// texel: the back buffer, <c>TextureCube</c> and <c>Texture3D</c>.
    ///
    /// Those take a <c>CNA_Color*</c> and nothing else, so the transfer is always RGBA8 and a
    /// non-Color surface is converted on the native side. That is the route's own long-standing
    /// semantics and this does not change it. What it changes is that the element type no longer
    /// has to be <see cref="Color"/>: any type whose size divides four reads and writes the same
    /// four bytes per texel, which is how a ported game's <c>GetBackBufferData&lt;uint&gt;</c> or
    /// <c>GetData&lt;byte&gt;</c> is meant to behave.
    /// </summary>
    internal static TextureTransferPlan Rgba8(
        int elementSize,
        int startIndex,
        int elementCount,
        int arrayLength) =>
        Restate(TextureDataType.Color, 4, "RGBA8", elementSize, startIndex, elementCount, arrayLength);

    private static TextureTransferPlan Restate(
        uint tag,
        int unitBytes,
        string unitName,
        int elementSize,
        int startIndex,
        int elementCount,
        int arrayLength)
    {
        long byteOffset = (long)startIndex * elementSize;
        long byteCount = (long)elementCount * elementSize;
        long byteCapacity = (long)arrayLength * elementSize;

        if (byteOffset % unitBytes != 0 || byteCount % unitBytes != 0)
        {
            throw new ArgumentException(
                "The size of the data passed in is too large or too small for this resource: a " +
                $"window of {elementCount} element(s) of {elementSize} byte(s) at index " +
                $"{startIndex} does not start and end on a whole {unitName} unit of {unitBytes} " +
                "byte(s).");
        }

        return new TextureTransferPlan(
            tag,
            (ulong)(byteOffset / unitBytes),
            (ulong)(byteCount / unitBytes),
            (ulong)(byteCapacity / unitBytes));
    }

    /// <summary>
    /// The tag and unit size for a surface format, with the size read from
    /// <c>cna_texture_get_format_size</c> rather than from a table here.
    ///
    /// Asking is worth the call. A duplicated size table is the kind of thing that stays right for
    /// a year and then silently disagrees with upstream about one format, and the failure mode is a
    /// transfer that reads the correct number of bytes from the wrong offset.
    /// </summary>
    internal static int UnitBytes(SurfaceFormat format)
    {
        CnaResult result = Native.cna_texture_get_format_size((uint)format, out int size);
        CnaException.ThrowIfFailed(result, nameof(UnitBytes));
        return size;
    }
}
