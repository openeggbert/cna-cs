using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>ABI-shaped 2D vector. See ../../cnabinding/analysis_binding.md §23.</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaVector2
{
    public readonly float X;
    public readonly float Y;

    public CnaVector2(float x, float y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>ABI-shaped RGBA color, one byte per channel. See analysis_binding.md §8.</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaColor
{
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;
    public readonly byte A;

    public CnaColor(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
}

/// <summary>
/// ABI-shaped frame time value. Ticks use the same resolution as
/// <see cref="System.TimeSpan.Ticks"/> (100ns), converted at the CNA boundary.
/// See ../../cnabinding/analysis_binding_sharp_runtime.md §42.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaGameTime
{
    public readonly long TotalGameTimeTicks;
    public readonly long ElapsedGameTimeTicks;
    public readonly byte IsRunningSlowly;

    public CnaGameTime(long totalGameTimeTicks, long elapsedGameTimeTicks, bool isRunningSlowly)
    {
        TotalGameTimeTicks = totalGameTimeTicks;
        ElapsedGameTimeTicks = elapsedGameTimeTicks;
        IsRunningSlowly = isRunningSlowly ? (byte)1 : (byte)0;
    }
}

/// <summary>
/// A full-keyboard snapshot, one bit per CNA key ordinal (0-255), packed into four 64-bit
/// words so callers do not need <c>unsafe</c>/fixed-buffer access. See the "input as snapshots"
/// guidance in ../../cnabinding/analysis_binding.md §25.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaKeyboardState
{
    public readonly ulong Bits0;
    public readonly ulong Bits1;
    public readonly ulong Bits2;
    public readonly ulong Bits3;

    public bool IsKeyDown(int keyOrdinal)
    {
        if (keyOrdinal is < 0 or > 255)
        {
            return false;
        }

        ulong word = keyOrdinal switch
        {
            < 64 => Bits0,
            < 128 => Bits1,
            < 192 => Bits2,
            _ => Bits3,
        };

        return (word & (1UL << (keyOrdinal % 64))) != 0;
    }
}

/// <summary>ABI-shaped mouse snapshot. See the "input as snapshots" guidance in
/// ../../cnabinding/analysis_binding.md §25. Buttons are packed as bit 0=left, 1=middle,
/// 2=right, 3=XButton1, 4=XButton2.</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaMouseState
{
    public readonly int X;
    public readonly int Y;
    public readonly int ScrollWheelValue;
    public readonly int HorizontalScrollWheelValue;
    public readonly byte Buttons;

    public bool IsButtonDown(int bit) => (Buttons & (1 << bit)) != 0;
}

/// <summary>
/// ABI-shaped game pad snapshot for one player index. <c>Buttons</c> is a bitmask matching the
/// core (non-thumbstick, non-trigger) subset of real XNA's <c>Buttons</c> flags enum -- see
/// CNA.Input.Buttons for the exact bit assignments and what is intentionally omitted.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaGamePadState
{
    public readonly byte IsConnected;
    public readonly uint Buttons;
    public readonly float LeftThumbStickX;
    public readonly float LeftThumbStickY;
    public readonly float RightThumbStickX;
    public readonly float RightThumbStickY;
    public readonly float LeftTrigger;
    public readonly float RightTrigger;

    public bool HasButton(uint mask) => (Buttons & mask) != 0;
}

/// <summary>
/// ABI-shaped game pad capabilities snapshot. No ABI shape exists upstream for this call (same
/// caveat as <c>cna_render_target2d_create</c> in Native.cs) -- self-designed for this
/// repository. <c>SupportedButtons</c> reuses <see cref="CnaGamePadState.Buttons"/>'s exact bit
/// layout (see <c>CNA.Input.Buttons</c>) rather than one bool field per button, and
/// <c>Features</c> packs the remaining thumbstick/trigger/vibration/voice booleans into a second
/// bitmask -- see <c>CNA.Input.GamePadCapabilities</c> for the bit assignments, which live there
/// (not here) since they are this project's own invention, not part of any ABI convention.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaGamePadCapabilities
{
    public readonly byte IsConnected;
    public readonly int GamePadType;
    public readonly uint SupportedButtons;
    public readonly uint Features;
}

/// <summary>ABI-shaped integer rectangle (pixel-space source/destination rects). Not named in the
/// analysis docs as its own struct, but implied by the <c>source</c> field of the
/// <c>CNA_SpriteDrawCommand</c> example in analysis_binding.md §22.</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaRect
{
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;

    public CnaRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}

/// <summary>
/// ABI-shaped single sprite draw command -- field-for-field the same as the batched
/// <c>CNA_SpriteDrawCommand</c> example struct in ../../cnabinding/analysis_binding.md §22
/// (texture, position, source, color, rotation, origin, scale, effects, layer_depth). Used here
/// for the *single*-draw extended form (<c>cna_sprite_batch_draw_ex</c>), not the Phase 5 batched
/// call the doc example was actually illustrating -- see plan.md Phase 4/5. There is no dedicated
/// "no source rectangle" flag: the "draw the whole texture" case is resolved to a concrete
/// <see cref="Source"/> covering the full texture bounds at the CNA.Framework call site, before
/// this struct is built, so the ABI shape needs nothing beyond what §22 already shows. This
/// struct's shape is otherwise unvalidated against any real upstream ABI (none exists yet for
/// this call); expect a signature audit once Track A ships, same as everything else in Phase 4.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaSpriteDrawCommand
{
    public readonly CnaHandle Texture;
    public readonly CnaVector2 Position;
    public readonly CnaRect Source;
    public readonly CnaColor Color;
    public readonly float Rotation;
    public readonly CnaVector2 Origin;
    public readonly CnaVector2 Scale;
    public readonly int Effects;
    public readonly float LayerDepth;

    public CnaSpriteDrawCommand(
        CnaHandle texture,
        CnaVector2 position,
        CnaRect source,
        CnaColor color,
        float rotation,
        CnaVector2 origin,
        CnaVector2 scale,
        int effects,
        float layerDepth)
    {
        Texture = texture;
        Position = position;
        Source = source;
        Color = color;
        Rotation = rotation;
        Origin = origin;
        Scale = scale;
        Effects = effects;
        LayerDepth = layerDepth;
    }
}

/// <summary>
/// ABI-shaped single glyph entry for <see cref="CnaSpriteFontData"/>. <c>Character</c> is a full
/// Unicode code point (not a UTF-16 <c>char</c>), so this can represent glyphs outside the BMP
/// without the surrogate-pair ambiguity a 16-bit field would have. <c>LeftSideBearing</c>/
/// <c>Width</c>/<c>RightSideBearing</c> are the classic "ABC" kerning triple -- see
/// <c>CNA.Graphics.SpriteFont</c>'s own doc comment for the algorithm these feed. No ABI shape
/// for any of this exists upstream (same caveat as <c>RenderTarget2D</c>'s natives) --
/// self-designed for this repository.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaGlyphMetrics
{
    public readonly int Character;
    public readonly CnaRect Bounds;
    public readonly CnaRect Cropping;
    public readonly float LeftSideBearing;
    public readonly float Width;
    public readonly float RightSideBearing;

    public CnaGlyphMetrics(int character, CnaRect bounds, CnaRect cropping, float leftSideBearing, float width, float rightSideBearing)
    {
        Character = character;
        Bounds = bounds;
        Cropping = cropping;
        LeftSideBearing = leftSideBearing;
        Width = width;
        RightSideBearing = rightSideBearing;
    }
}

/// <summary>
/// A fixed-capacity inline array of <see cref="CnaGlyphMetrics"/>, using the C# 12 <c>InlineArray</c>
/// feature so the whole buffer marshals as a flat, contiguous block -- no separate pointer/length
/// two-call dance (like <c>CnaError.GetLastErrorMessage</c> needs for a truly unbounded string) is
/// needed as long as a font's glyph count fits the capacity. <see cref="CnaSpriteFontData.GlyphCount"/>
/// says how many of the <see cref="MaxGlyphs"/> slots are actually populated. This is a real,
/// deliberate limitation, not an oversight -- flagged in <c>CNA.Content.ContentManager</c>'s doc
/// comment, not hidden.
/// </summary>
[InlineArray(MaxGlyphs)]
internal struct CnaGlyphBuffer
{
    public const int MaxGlyphs = 256;

    private CnaGlyphMetrics _element0;
}

/// <summary>
/// The result of loading a <c>SpriteFont</c> asset. No ABI shape for <c>SpriteFont</c> content
/// loading exists upstream -- self-designed for this repository, see
/// <c>CNA.Content.ContentManager.LoadSpriteFontData</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaSpriteFontData
{
    public CnaHandle Texture;
    public int LineSpacing;
    public float Spacing;
    public byte HasDefaultCharacter;
    public int DefaultCharacter;
    public int GlyphCount;
    public CnaGlyphBuffer Glyphs;
}
