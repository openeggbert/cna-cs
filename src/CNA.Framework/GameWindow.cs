using System.Text;
using CNA.Interop;

namespace CNA;

/// <summary>
/// Matches real XNA's <c>GameWindow</c> surface, narrowly: only <see cref="Title"/>. The real,
/// shipped openeggbert/cna C API has no <c>cna_game_window_set_title</c> -- the only setter is
/// <c>cna_game_set_window_title</c> (<c>runtime.h:246</c>), a plain owned-handle call safe to run
/// any time, which is why this needs no lifecycle-callback dance the way
/// <c>GraphicsDevice</c>'s handle resolution does.
/// </summary>
public class GameWindow
{
    private readonly nint _nativeGameHandleValue;

    internal GameWindow(nint nativeGameHandleValue)
    {
        _nativeGameHandleValue = nativeGameHandleValue;
    }

    public string Title
    {
        get => QueryTitle();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CnaResult result = CnaStringMarshal.WithStringView(
                value, view => Native.cna_game_set_window_title(new CnaHandle(_nativeGameHandleValue), view));
            CnaException.ThrowIfFailed(result, nameof(Title));
        }
    }

    private unsafe string QueryTitle()
    {
        CnaHandle game = new(_nativeGameHandleValue);
        CnaResult sizeResult = Native.cna_game_window_get_title_size(game, out ulong length);
        if (sizeResult.IsFailure() || length == 0)
        {
            return string.Empty;
        }

        byte[] buffer = new byte[length];
        fixed (byte* bufferPtr = buffer)
        {
            CnaResult copyResult = Native.cna_game_window_copy_title(game, bufferPtr, length, out ulong written);
            if (copyResult.IsFailure())
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(buffer, 0, (int)written);
        }
    }
}
