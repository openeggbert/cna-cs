namespace CNA.Content.Xnb;

/// <summary>
/// Reads a real <c>.xnb</c> <c>SpriteFontReader</c> object graph.
///
/// The field order is XNA's own and is load-bearing -- texture, glyph bounds, cropping, character
/// map, line spacing, spacing, kerning, default character. Everything except the two scalars is a
/// nested object read through the type-reader table, which is why this reader needs the generic
/// list and nullable readers <see cref="XnbContentReader"/> registers.
///
/// This exists because there is no native route to load one. <c>content.h</c> has
/// <c>cna_content_manager_load_texture2d</c>, <c>_load_sound_effect</c> and
/// <c>_load_texture_cube</c> and nothing for fonts; <c>sprite_font.h</c> can only build a font from
/// a glyph table a caller already has. The binding previously papered over that with a P/Invoke to
/// <c>cna_content_load_spritefont</c>, which exists in no header -- so every
/// <c>Load&lt;SpriteFont&gt;</c> would have died with an <c>EntryPointNotFoundException</c>.
/// Parsing the container here is the honest way to get the glyph table the C API asks for, and it
/// is the same thing this project already does for <c>Model</c>.
/// </summary>
internal static class XnbSpriteFontReader
{
    internal static object Read(XnbContentReader reader)
    {
        XnbTextureData texture = XnbContentReader.RequireType<XnbTextureData>(
            reader.ReadObject(), "a SpriteFont's texture");

        IReadOnlyList<Rectangle> glyphBounds = reader.ReadList<Rectangle>("glyph bounds");
        IReadOnlyList<Rectangle> cropping = reader.ReadList<Rectangle>("cropping rectangles");
        IReadOnlyList<char> characters = reader.ReadList<char>("character map");

        int lineSpacing = reader.ReadInt32();
        float spacing = reader.ReadSingle();

        IReadOnlyList<Vector3> kerning = reader.ReadList<Vector3>("kerning values");
        char? defaultCharacter = reader.ReadObject() as char?;

        if (glyphBounds.Count != characters.Count
            || cropping.Count != characters.Count
            || kerning.Count != characters.Count)
        {
            throw new ContentLoadException(
                "Corrupt .xnb SpriteFont: the glyph bounds, cropping, character and kerning lists have " +
                $"different lengths ({glyphBounds.Count}/{cropping.Count}/{characters.Count}/{kerning.Count}).");
        }

        return new XnbSpriteFontData(
            texture, glyphBounds, cropping, characters, lineSpacing, spacing, kerning, defaultCharacter);
    }
}
