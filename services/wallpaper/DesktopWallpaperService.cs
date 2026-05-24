using System.Runtime.InteropServices;
using CraftSharp.Helpers;

namespace CraftSharp.Services.Wallpaper;

public class DesktopWallpaperService
{
    public static DesktopWallpaperService Instance { get; } = new();

    private DesktopWallpaperService() { }

    public bool SetWallpaper(string imagePath)
    {
        return Win32Helper.SystemParametersInfo(
            Win32Helper.SPI_SETDESKWALLPAPER,
            0,
            imagePath,
            Win32Helper.SPIF_UPDATEINIFILE | Win32Helper.SPIF_SENDWININICHANGE);
    }
}
