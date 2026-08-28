using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CraftSharp.Helpers;
using Microsoft.Win32;

namespace CraftSharp.Services.Wallpaper;

public class DesktopWallpaperService
{
    public static DesktopWallpaperService Instance { get; } = new();

    private IDesktopWallpaper? _com;
    private bool _comAvailable;

    private DesktopWallpaperService() { }

    public void Initialize()
    {
        try
        {
            _com = (IDesktopWallpaper)new DesktopWallpaperComObject();
            _com.SetPosition(DeskWallpaperPosition.Fill);
            _comAvailable = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Wallpaper] DesktopWallpaper COM init failed: {ex.Message}");
            _com = null;
            _comAvailable = false;
        }
    }

    public bool IsComAvailable => _comAvailable;

    /// <summary>
    /// 按显示器设备路径设置静态壁纸。COM 不可用时降级为全局设置。
    /// </summary>
    public bool SetWallpaperForMonitor(string monitorDevicePath, string imagePath)
    {
        if (_comAvailable && _com != null)
        {
            try
            {
                var hr = _com.SetWallpaper(monitorDevicePath, imagePath);
                if (hr < 0)
                {
                    Debug.WriteLine($"[Wallpaper] COM SetWallpaper hr=0x{hr:X8}, monitor={monitorDevicePath}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Wallpaper] COM SetWallpaper failed: {ex.Message}");
                return false;
            }
        }

        return SetWallpaper(imagePath);
    }

    /// <summary>
    /// 读取指定显示器当前壁纸路径，失败返回 null
    /// </summary>
    public string? GetWallpaperForMonitor(string monitorDevicePath)
    {
        if (!_comAvailable || _com == null)
            return null;

        try
        {
            var hr = _com.GetWallpaper(monitorDevicePath, out IntPtr ptr);
            if (hr < 0 || ptr == IntPtr.Zero)
                return null;
            var path = Marshal.PtrToStringUni(ptr);
            Marshal.FreeCoTaskMem(ptr);
            return path;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Wallpaper] COM GetWallpaper failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 全局设置壁纸（降级路径，所有显示器同图）
    /// </summary>
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

    public void ClearWallpaper()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            key?.SetValue("Wallpaper", "");
        }
        catch { }

        Win32Helper.SystemParametersInfo(
            Win32Helper.SPI_SETDESKWALLPAPER,
            0,
            "",
            Win32Helper.SPIF_UPDATEINIFILE | Win32Helper.SPIF_SENDWININICHANGE);
    }
}
