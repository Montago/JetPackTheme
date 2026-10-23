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

    // Window style bits and messages for keeping the system caption buttons away
    public const int WmStyleChanging = 0x007C;
    public const int WmSysCommand = 0x0112;
    public const int ScKeyMenu = 0xF100;
    public const int GwlStyle = -16;
    public const int WsSysMenu = 0x00080000;
    private const int SwpNoSize = 0x1, SwpNoMove = 0x2, SwpNoZOrder = 0x4, SwpNoActivate = 0x10, SwpFrameChanged = 0x20;

    /// <summary>STYLESTRUCT, sent with WM_STYLECHANGING.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct StyleStruct
    {
        public int StyleOld;
        public int StyleNew;
    }

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, int flags);

    /// <summary>Sets or clears window style bits and lets the frame pick up the change.</summary>
    public static void SetStyleBits(IntPtr hwnd, int bits, bool set)
    {
        var style = GetWindowLong(hwnd, GwlStyle);
        var updated = set ? style | bits : style & ~bits;
        if (updated != style)
        {
            SetWindowLong(hwnd, GwlStyle, updated);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpNoSize | SwpNoMove | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
        }
    }

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
