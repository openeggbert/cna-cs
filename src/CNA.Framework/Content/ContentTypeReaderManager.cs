using CNA.Interop;

namespace CNA.Content;

/// <summary>Matches real XNA's <c>ContentTypeReaderManager</c>: the registry that maps a canonical
/// type name from an asset header to the <see cref="ContentTypeReader"/> that can deserialize it.
/// Static, as in XNA -- the registry is process-wide, not per content manager.</summary>
public static class ContentTypeReaderManager
{
    public static bool IsRegistered(string canonicalName)
    {
        ArgumentNullException.ThrowIfNull(canonicalName);

        byte isRegistered = 0;
        CnaResult result = CnaStringMarshal.WithStringView(
            canonicalName, view => Native.cna_content_type_reader_manager_get_is_registered(view, out isRegistered));
        CnaException.ThrowIfFailed(result, nameof(IsRegistered));
        return isRegistered != 0;
    }

    /// <summary>Creates the reader registered for <paramref name="canonicalName"/>. The caller owns
    /// the result and must dispose it -- unlike the borrowed views elsewhere in this project, this
    /// really is a fresh native object per call.</summary>
    public static ContentTypeReader CreateReader(string canonicalName)
    {
        ArgumentNullException.ThrowIfNull(canonicalName);

        CnaHandle reader = default;
        CnaResult result = CnaStringMarshal.WithStringView(
            canonicalName, view => Native.cna_content_type_reader_manager_create_reader(view, out reader));
        CnaException.ThrowIfFailed(result, nameof(CreateReader));
        return new ContentTypeReader(reader.AsNint);
    }

    /// <summary>Empties the registry. Real XNA has no equivalent; the C API does, and it exists for
    /// test isolation -- without it a registration from one test leaks into the next.</summary>
    public static void ClearTypeCreators()
    {
        CnaResult result = Native.cna_content_type_reader_manager_clear_type_creators();
        CnaException.ThrowIfFailed(result, nameof(ClearTypeCreators));
    }
}
