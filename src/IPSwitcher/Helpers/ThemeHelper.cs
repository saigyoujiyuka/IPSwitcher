using System.Runtime.InteropServices;
using IPSwitcher.Models;
using Microsoft.Win32;

namespace IPSwitcher.Helpers;

public static class ThemeHelper
{
    private const string RegKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    [DllImport("uxtheme.dll", SetLastError = true)]
    private static extern IntPtr AllowDarkModeForWindow(IntPtr hwnd, bool allow);

    public static bool IsSystemDark()
    {
        try
        {
            var value = Registry.GetValue(RegKey, "AppsUseLightTheme", null);
            if (value is int i)
            {
                return i == 0;
            }
        }
        catch
        {
        }
        return false;
    }

    public static bool ResolveDark(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => IsSystemDark(),
        };
    }
}
