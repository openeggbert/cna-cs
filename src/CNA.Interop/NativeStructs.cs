using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CNA.Interop;

/// <summary>ABI-shaped 2D vector. See openeggbert/cna's analysis_binding.md §23.</summary>
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
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_GameTime</c> exactly
/// (<c>runtime.h:15-27</c>) -- ticks use the same resolution as <see cref="System.TimeSpan.Ticks"/>
/// (100ns), matching the real struct's own documented units. <see cref="Reserved"/> exists only so
/// this struct's size/layout matches the real one byte-for-byte; it carries no meaning of its own.
/// Passed as a nullable *pointer* by every real lifecycle callback (null for load/unload/exit
/// notifications, non-null for update/draw) rather than by value -- see
/// <see cref="CnaManagedGameCallbacks"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaGameTime
{
    public readonly long TotalGameTimeTicks;
    public readonly long ElapsedGameTimeTicks;
    public readonly byte IsRunningSlowly;
    public readonly CnaReservedBytes7 Reserved;

    public CnaGameTime(long totalGameTimeTicks, long elapsedGameTimeTicks, bool isRunningSlowly)
    {
        TotalGameTimeTicks = totalGameTimeTicks;
        ElapsedGameTimeTicks = elapsedGameTimeTicks;
        IsRunningSlowly = isRunningSlowly ? (byte)1 : (byte)0;
        Reserved = default;
    }
}

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_KeyboardState</c> exactly
/// (<c>input.h:598-607</c>) -- one bit per CNA key ordinal (0-255), packed into four 64-bit words
/// so callers do not need <c>unsafe</c>/fixed-buffer access ("key N is bit N modulo 64 of word N
/// divided by 64", confirmed against the real header text to match this type's own pre-migration
/// bit-indexing exactly). Only the <see cref="StructSize"/>/<see cref="StructVersion"/> header
/// needed adding -- see <see cref="CnaGameFrameHooks"/>'s own constructor doc comment for the
/// self-populating-constructor rationale.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaKeyboardState
{
    public readonly uint StructSize;
    public readonly uint StructVersion;
    public readonly ulong Bits0;
    public readonly ulong Bits1;
    public readonly ulong Bits2;
    public readonly ulong Bits3;

    public unsafe CnaKeyboardState()
    {
        StructSize = (uint)sizeof(CnaKeyboardState);
        StructVersion = 1;
    }

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

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_MouseState</c> exactly
/// (<c>input.h:353-377</c>). <see cref="Buttons"/> widened from the old guessed shape's single byte
/// to the real <c>CNA_MouseButtonFlags</c> width (a full <c>uint32_t</c>) -- a real memory-layout
/// fix, not just cosmetic, even though every currently-defined button bit still fits in a byte's
/// range. Bit values (left=1&lt;&lt;0, middle=1&lt;&lt;1, right=1&lt;&lt;2, x1=1&lt;&lt;3,
/// x2=1&lt;&lt;4) were already correct before this migration reached this file. See
/// <see cref="CnaGameFrameHooks"/>'s own constructor doc comment for the self-populating
/// -constructor rationale.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaMouseState
{
    public readonly uint StructSize;
    public readonly uint StructVersion;
    public readonly int X;
    public readonly int Y;
    public readonly int ScrollWheelValue;
    public readonly int HorizontalScrollWheelValue;
    public readonly uint Buttons;
    public readonly uint Reserved;

    public unsafe CnaMouseState()
    {
        StructSize = (uint)sizeof(CnaMouseState);
        StructVersion = 1;
    }

    public bool IsButtonDown(int bit) => (Buttons & (1u << bit)) != 0;
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_GamePadAnalogState</c>
/// exactly (<c>input.h:479-491</c>) -- a plain, unversioned nested struct (matching
/// <see cref="CnaVector2"/>'s own no-header convention: a fixed value, not an extensible top-level
/// input/output).</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaGamePadAnalogState
{
    public readonly CnaVector2 LeftThumbStick;
    public readonly CnaVector2 RightThumbStick;
    public readonly float LeftTrigger;
    public readonly float RightTrigger;
}

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_GamePadState</c> exactly
/// (<c>input.h:494-518</c>) -- the old guessed shape had the right idea (bitmask buttons,
/// individual thumbstick/trigger floats) but the wrong layout: the real struct nests every analog
/// value into <see cref="CnaGamePadAnalogState"/> and adds <see cref="IsConnected"/>/
/// <see cref="PacketNumber"/>, neither of which the old struct had at all. <see cref="Buttons"/>'s
/// own bit values were already correct before this migration reached this file (confirmed against
/// <c>CNA_GAMEPAD_BUTTON_*</c> -- dpad folded into the same mask as other buttons, matching
/// <c>CNA.Input.Buttons</c> exactly).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaGamePadState
{
    public readonly uint StructSize;
    public readonly uint StructVersion;
    public readonly byte IsConnected;
    public readonly CnaReservedBytes3 Reserved0;
    public readonly int PacketNumber;
    public readonly uint Buttons;
    public readonly uint Reserved1;
    public readonly CnaGamePadAnalogState Analog;

    public unsafe CnaGamePadState()
    {
        StructSize = (uint)sizeof(CnaGamePadState);
        StructVersion = 1;
    }

    public bool HasButton(uint mask) => (Buttons & mask) != 0;
}

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_GamePadCapabilities</c> exactly
/// (<c>input_gamepad.h:45-134</c>) -- a complete redesign from the old guessed shape, which packed
/// everything into two bitmasks (<c>SupportedButtons</c>/<c>Features</c>, this project's own
/// invention with no ABI backing). The real struct has one individual <c>CNA_Bool</c> field per
/// capability instead -- 24 XNA-canonical <c>Has*</c> fields (already matching
/// <c>CNA.Input.GamePadCapabilities</c>'s own pre-migration property names and count exactly,
/// confirmed field-for-field before writing this struct) plus 10 CNA-extension <c>_ext</c> fields
/// this project doesn't expose in its own public <c>GamePadCapabilities</c> type -- kept here only
/// so this struct's own size/layout matches the real one exactly for correct marshaling, not
/// because anything reads them (real feature surface for a future session, not added
/// speculatively). See <see cref="CnaGameFrameHooks"/>'s own constructor doc comment for the
/// self-populating-constructor rationale.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaGamePadCapabilities
{
    public readonly uint StructSize;
    public readonly uint StructVersion;
    public readonly uint GamePadType;
    public readonly byte IsConnected;

    public readonly byte HasAButton;
    public readonly byte HasBButton;
    public readonly byte HasXButton;
    public readonly byte HasYButton;
    public readonly byte HasBackButton;
    public readonly byte HasStartButton;
    public readonly byte HasBigButton;

    public readonly byte HasDPadUpButton;
    public readonly byte HasDPadDownButton;
    public readonly byte HasDPadLeftButton;
    public readonly byte HasDPadRightButton;

    public readonly byte HasLeftShoulderButton;
    public readonly byte HasRightShoulderButton;
    public readonly byte HasLeftStickButton;
    public readonly byte HasRightStickButton;

    public readonly byte HasLeftXThumbStick;
    public readonly byte HasLeftYThumbStick;
    public readonly byte HasRightXThumbStick;
    public readonly byte HasRightYThumbStick;

    public readonly byte HasLeftTrigger;
    public readonly byte HasRightTrigger;
    public readonly byte HasLeftVibrationMotor;
    public readonly byte HasRightVibrationMotor;
    public readonly byte HasVoiceSupport;

    public readonly byte HasLightBarExt;
    public readonly byte HasTriggerVibrationMotorsExt;
    public readonly byte HasMisc1Ext;
    public readonly byte HasPaddle1Ext;
    public readonly byte HasPaddle2Ext;
    public readonly byte HasPaddle3Ext;
    public readonly byte HasPaddle4Ext;
    public readonly byte HasTouchpadExt;
    public readonly byte HasGyroExt;
    public readonly byte HasAccelerometerExt;

    public readonly byte Reserved;

    public unsafe CnaGamePadCapabilities()
    {
        StructSize = (uint)sizeof(CnaGamePadCapabilities);
        StructVersion = 1;
    }
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
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_SpriteBatchBeginInfo</c> exactly
/// (<c>graphics.h:416-428</c>) -- the old guessed <c>cna_sprite_batch_begin</c> took no parameters
/// beyond the handle at all; the real one needs this versioned struct, currently only carrying a
/// sort mode. <c>CNA.Graphics.SpriteBatch.Begin()</c> only ever needs
/// <c>CNA_SPRITE_SORT_MODE_DEFERRED</c> (0, matching real XNA's own default and the only mode this
/// project's batching model supports), so <see cref="SortMode"/> is always left at its default.
/// See <see cref="CnaGameFrameHooks"/>'s own constructor doc comment for the self-populating
/// -constructor rationale.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaSpriteBatchBeginInfo
{
    public uint StructSize;
    public uint StructVersion;
    public uint SortMode;
    public uint Reserved;

    public unsafe CnaSpriteBatchBeginInfo()
    {
        StructSize = (uint)sizeof(CnaSpriteBatchBeginInfo);
        StructVersion = 1;
    }
}

/// <summary>
/// Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_SpriteScaledCommand</c> exactly
/// (<c>graphics.h:478-516</c>) -- confirmed field-for-field identical (texture, position, source,
/// color, rotation, origin, scale, effects, layer_depth) to the shape this struct already had
/// before the native-ABI migration reached it, needing only the <see cref="StructSize"/>/
/// <see cref="StructVersion"/> header this migration adds everywhere. One of these is buffered per
/// <c>SpriteBatch.Draw</c>/<c>DrawString</c> call and the whole batch is flushed through one
/// <c>cna_sprite_batch_submit_scaled_many</c> call at <c>End()</c> -- see
/// <c>CNA.Interop.Native</c>'s SpriteBatch section for why this project only ever needs the
/// position+scale route, not the real ABI's other one
/// (<c>CNA_SpriteCommand</c>/<c>cna_sprite_batch_submit_many</c>, destination-rectangle-based).
/// There is no dedicated "no source rectangle" flag: the "draw the whole texture" case is resolved
/// to a concrete <see cref="Source"/> covering the full texture bounds at the CNA.Framework call
/// site, before this struct is built, matching what an empty/zero-size <see cref="Source"/> means
/// to the real ABI's own optional-source convention.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaSpriteDrawCommand
{
    public readonly uint StructSize;
    public readonly uint StructVersion;
    public readonly CnaHandle Texture;
    public readonly CnaVector2 Position;
    public readonly CnaRect Source;
    public readonly CnaColor Color;
    public readonly float Rotation;
    public readonly CnaVector2 Origin;
    public readonly CnaVector2 Scale;
    public readonly int Effects;
    public readonly float LayerDepth;

    public unsafe CnaSpriteDrawCommand(
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
        StructSize = (uint)sizeof(CnaSpriteDrawCommand);
        StructVersion = 1;
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

/// <summary>ABI-shaped 3D vector -- used throughout the ABI wherever a real function takes or
/// returns a <c>CNA_Vector3</c> (directional lights, effect colors, and more).</summary>
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

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_Quaternion</c> exactly
/// (<c>quaternion.h</c>). Added by Phase 8 WP4a -- <c>cna_effect_parameter_get/set_value</c>'s
/// value-type tag includes a quaternion, the first time this project needed one across the
/// ABI.</summary>
internal readonly struct CnaQuaternion
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;
    public readonly float W;

    public CnaQuaternion(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
}

/// <summary>Mirrors the real, shipped openeggbert/cna C API's own <c>CNA_Vector4</c> exactly
/// (<c>vectors.h</c>). Added by Phase 8 WP4a, which is the first thing in this project to need a
/// four-component vector across the ABI (<c>cna_effect_annotation_get_value_vector4</c>).</summary>
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

/// <summary>ABI-shaped rectangle -- <c>core.h:133</c>'s <c>CNA_Rectangle</c>, four
/// <see cref="int"/>s in x/y/width/height order.</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CnaRectangle
{
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;

    public CnaRectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}

/// <summary>
/// One entry of a SpriteFont's glyph table -- <c>sprite_font.h</c>'s <c>CNA_SpriteFontGlyph</c>.
///
/// Versioned per *element*, not per array: <c>struct_size</c> and <c>struct_version</c> sit on
/// every entry, and version one "requires exact current size". So a caller-supplied array has to
/// stamp both on each element, and this is the one array in the binding where the two-call
/// size-then-copy pattern hands back caller-initialized structs rather than plain values.
///
/// <c>Character</c> is UTF-16, which maps to C#'s <see cref="char"/> exactly. <c>Reserved</c> must
/// be zero.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaSpriteFontGlyph
{
    public uint StructSize;
    public uint StructVersion;
    public CnaRectangle GlyphBounds;
    public CnaRectangle Cropping;
    public ushort Character;
    public ushort Reserved;
    public CnaVector3 Kerning;

    /// <summary>A zeroed element carrying the version stamp this ABI requires, for the
    /// caller-initialized half of the contract.</summary>
    public static unsafe CnaSpriteFontGlyph Versioned() => new()
    {
        StructSize = (uint)sizeof(CnaSpriteFontGlyph),
        StructVersion = 1,
    };
}

/// <summary>
/// A SpriteFont's layout summary -- <c>sprite_font.h</c>'s <c>CNA_SpriteFontInfo</c>.
///
/// Caller-initialized and versioned, so it is passed by <c>ref</c> rather than <c>out</c>: an
/// <c>out</c> parameter skips the parameterless constructor, which is where the version stamp
/// comes from.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CnaSpriteFontInfo
{
    public uint StructSize;
    public uint StructVersion;
    public ulong CharacterCount;
    public int LineSpacing;
    public float Spacing;
    public ushort DefaultCharacter;
    public byte HasDefaultCharacter;

    /// <summary>Matches the header's trailing `uint8_t reserved[5]`, which is returned as zero.
    /// Declared explicitly rather than left to padding so the managed size matches
    /// `struct_size`, which version one requires to be exact.</summary>
    private byte _reserved0;
    private byte _reserved1;
    private byte _reserved2;
    private byte _reserved3;
    private byte _reserved4;

    public static unsafe CnaSpriteFontInfo Versioned() => new()
    {
        StructSize = (uint)sizeof(CnaSpriteFontInfo),
        StructVersion = 1,
    };
}

/// <summary>
/// The behaviour table a caller-supplied content type reader is registered with --
/// <c>content_readers.h</c>'s <c>CNA_ContentTypeReaderCallbacks</c>.
///
/// Every pointer is copied during registration, but <see cref="Context"/> stays caller-owned and
/// must outlive the registration, so whatever roots the managed side has to be held for at least
/// that long.
///
/// <see cref="Create"/> and <see cref="Read"/> are required; <see cref="Destroy"/> may be null.
/// Unlike <c>CNA_GameComponentCallbacks</c>, every one of these returns <c>CNA_Result</c> -- a
/// content reader that fails needs to fail the load rather than have its exception stashed and
/// rethrown later.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CnaContentTypeReaderCallbacks
{
    public uint StructSize;
    public uint StructVersion;
    public CnaStringView TargetTypeName;
    public int TypeVersion;
    public byte CanDeserializeIntoExistingObject;

    private byte _reserved0;
    private byte _reserved1;
    private byte _reserved2;

    public delegate* unmanaged[Cdecl]<nint, nint*, CnaResult> Create;
    public delegate* unmanaged[Cdecl]<nint, CnaHandle, nint, nint*, CnaResult> Read;
    public delegate* unmanaged[Cdecl]<nint, CnaResult> Destroy;
    public nint Context;

    public static CnaContentTypeReaderCallbacks Versioned() => new()
    {
        StructSize = (uint)sizeof(CnaContentTypeReaderCallbacks),
        StructVersion = 1,
    };
}
