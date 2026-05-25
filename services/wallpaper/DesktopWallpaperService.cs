using System.Runtime.InteropServices;
using CraftSharp.Helpers;
using Microsoft.Win32;

namespace CraftSharp.Services.Wallpaper;

public class DesktopWallpaperService
{
    public static DesktopWallpaperService Instance { get; } = new();

    private DesktopWallpaperService() { }

    public bool SetWallpaper(string imagePath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            key?.SetValue("WallpaperStyle", "10");
            key?.SetValue("TileWallpaper", "0");
        }
        catch { }

        return Win32Helper.SystemParametersInfo(
            Win32Helper.SPI_SETDESKWALLPAPER,
            0,
            imagePath,
            Win32Helper.SPIF_UPDATEINIFILE | Win32Helper.SPIF_SENDWININICHANGE);
    }
}
