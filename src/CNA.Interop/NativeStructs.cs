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
/// ABI-shaped sprite draw command -- field-for-field the same as the
/// <c>CNA_SpriteDrawCommand</c> example struct in ../../cnabinding/analysis_binding.md §22
/// (texture, position, source, color, rotation, origin, scale, effects, layer_depth). One of
/// these is buffered per <c>SpriteBatch.Draw</c>/<c>DrawString</c> call and the whole batch is
/// flushed through one <c>cna_sprite_batch_draw_many</c> call at <c>End()</c> -- exactly the
/// batched shape §22's own example illustrates, not a single-draw primitive anymore (see
/// plan.md Phase 5). There is no dedicated "no source rectangle" flag: the "draw the whole
/// texture" case is resolved to a concrete <see cref="Source"/> covering the full texture bounds
/// at the CNA.Framework call site, before this struct is built, so the ABI shape needs nothing
/// beyond what §22 already shows. This struct's shape is otherwise unvalidated against any real
/// upstream ABI (none exists yet for this call); expect a signature audit once Track A ships,
/// same as everything else in Phase 4/5.
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

/// <summary>ABI-shaped 3D vector, needed starting with <see cref="CnaBasicEffectParams"/> --
/// nothing before it needed one crossing the ABI.</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaVector3
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;

    public CnaVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

/// <summary>ABI-shaped 4D vector -- needed for <see cref="CnaBasicEffectParams.DiffuseColor"/>
/// (RGBA) and <c>FogVector</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaVector4
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;
    public readonly float W;

    public CnaVector4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
}

/// <summary>A flat 16-float buffer for a column-major 4x4 matrix, using the same C# 12
/// <c>InlineArray</c> technique <see cref="CnaGlyphBuffer"/> already uses -- avoids either 16
/// separate named fields or a 16-parameter constructor section just for one matrix.</summary>
[InlineArray(16)]
internal struct CnaMatrix16
{
    private float _element0;
}

/// <summary>
/// One directional light, matching the fields the real openeggbert/cna C++ engine's
/// <c>BasicEffect::FillGpuDrawParams</c> actually reads from each of its own three
/// <c>DirectionalLight</c> members (confirmed by reading that method's source directly, not
/// guessed) -- <c>Direction</c> is always sent regardless of <c>Enabled</c>, but
/// <c>CNA.Graphics.BasicEffect</c> zeroes <see cref="DiffuseColor"/>/<see cref="SpecularColor"/>
/// itself before crossing the ABI when a light is disabled, mirroring that method's own behavior
/// exactly (the real C++ <c>DirectionalLight</c> type does not zero these itself on its
/// <c>Enabled</c> setter, so the zeroing has to happen at the point <c>FillGpuDrawParams</c> --
/// and here, this project's equivalent -- actually reads the light, not earlier).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaDirectionalLight
{
    public CnaVector3 Direction;
    public CnaVector3 DiffuseColor;
    public CnaVector3 SpecularColor;
}

/// <summary>
/// The data <c>BasicEffect.Apply()</c> pushes to the device as pending per-draw effect state, for
/// the next <c>DrawPrimitives</c>/<c>DrawIndexedPrimitives</c> call to actually use -- matching
/// real XNA's own two-step "Apply() first, then Draw()" contract (nothing about this project's
/// existing <c>Draw*</c> methods needed to change). No ABI shape for any of this exists in the
/// analysis docs, but this is meaningfully better-grounded than most of this session's other
/// self-designed ABI surfaces: it's a deliberately-reduced subset of the real C++ engine's own
/// internal <c>CNA::Internal::Renderers::GpuDrawParams</c> struct (read directly from
/// <c>IGraphicsRenderer.hpp</c>), containing only the fields <c>BasicEffect::FillGpuDrawParams</c>
/// actually populates -- everything else in the real struct exists for other stock effects
/// (<c>SkinnedEffect</c>'s bone transforms, <c>DualTextureEffect</c>'s second texture,
/// <c>EnvironmentMapEffect</c>'s cube map, ...) this project doesn't implement yet.
///
/// The real struct's <c>worldColMajor</c> is the *only* matrix field -- View/Projection never
/// cross into it at all; the real <c>FillGpuDrawParams</c> only uses them internally to *derive*
/// <c>eyePositionWorld</c> (<c>Matrix.Invert(View).Translation</c>) and <c>fogVector</c> (from
/// <c>World*View</c> plus fog start/end). This project computes those same two derived values in
/// managed code instead (using its own already-implemented, already-tested
/// <c>CNA.Matrix.Invert</c>/multiplication), so the native struct only needs
/// the *results*, not the raw View/Projection matrices themselves -- see
/// <c>CNA.Graphics.BasicEffect</c> for that computation, verified against the real algorithm's
/// exact formula, not reinvented.
///
/// Plain mutable struct (not <c>readonly</c> with a constructor, unlike most other structs in
/// this file) built via object-initializer syntax at its one call site -- a 33-field
/// constructor would be far more error-prone (easy to transpose two same-typed positional
/// arguments) than named field assignment.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaBasicEffectParams
{
    public CnaHandle Texture;
    public byte TextureEnabled;
    public byte VertexColorEnabled;
    public byte LightingEnabled;
    public byte PreferPerPixelLighting;

    /// <summary>RGB = forwarded diffuse (baking in EmissiveColor when unlit, matching the real
    /// algorithm's own comment on why -- see <c>BasicEffect.cs</c>), already alpha-multiplied.
    /// W = alpha.</summary>
    public CnaVector4 DiffuseColor;

    public CnaVector3 AmbientColor;
    public CnaDirectionalLight Light0;
    public CnaDirectionalLight Light1;
    public CnaDirectionalLight Light2;

    /// <summary>Zero when <see cref="LightingEnabled"/> is false -- EmissiveColor was already
    /// folded into <see cref="DiffuseColor"/> above instead, matching the real algorithm's
    /// lit-path-only block exactly.</summary>
    public CnaVector3 EmissiveColor;
    public CnaVector3 SpecularColor;
    public float SpecularPower;
    public CnaVector3 EyePositionWorld;

    /// <summary>World matrix, column-major -- matching the real struct's own <c>worldColMajor</c>
    /// naming and layout exactly, since this project's own <c>Matrix</c> is row-major.</summary>
    public CnaMatrix16 WorldColMajor;

    public byte FogEnabled;
    public CnaVector3 FogColor;
    public CnaVector4 FogVector;
}
