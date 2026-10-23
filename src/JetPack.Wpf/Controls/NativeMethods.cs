using System.Runtime.InteropServices;

namespace JetPack.Wpf.Controls;

internal static class NativeMethods
{
    // DWMWINDOWATTRIBUTE values (Windows 11, build 22000+; the backdrop type needs 22621+)
    public const int DwmwaUseImmersiveDarkMode = 20;
    public const int DwmwaWindowCornerPreference = 33;
    public const int DwmwaBorderColor = 34;
    public const int DwmwaSystemBackdropType = 38;

    // DWM_SYSTEMBACKDROP_TYPE values
    public const int DwmsbtNone = 1;
    public const int DwmsbtMainWindow = 2;

    // DWM_WINDOW_CORNER_PREFERENCE values
    public const int DwmwcpDoNotRound = 1;
    public const int DwmwcpRound = 2;

    // COLORREF meaning "draw no border"
    public const int DwmwaColorNone = unchecked((int)0xFFFFFFFE);

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>Sets an int-sized DWM attribute; returns false where the OS does not support it.</summary>
    public static bool SetDwmAttribute(IntPtr hwnd, int attribute, int value)
    {
        try
        {
            return DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int)) == 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }
}
