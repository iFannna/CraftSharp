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
            EnsureFillPosition();
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
                EnsureFillPosition();
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
    /// 将壁纸显示位置强制为 Fill（cover）。
    /// DWM 渲染跟随传统注册表 WallpaperStyle；而 IDesktopWallpaper.SetPosition 在
    /// Win11 上返回 S_OK 却会异步把该值改写为 6（Fit），导致非 16:9 屏出现黑边，
    /// 因此绝不能调用 SetPosition，只能直接写注册表。
    /// </summary>
    private void EnsureFillPosition()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            key?.SetValue("WallpaperStyle", "10");
            key?.SetValue("TileWallpaper", "0");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Wallpaper] EnsureFillPosition failed: {ex.Message}");
        }
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
