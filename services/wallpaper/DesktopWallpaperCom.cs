using System;
using System.Runtime.InteropServices;
using CraftSharp.Helpers;

namespace CraftSharp.Services.Wallpaper
{
    /// <summary>
    /// 壁纸显示位置（对应 DESK_WALLPAPER_POSITION）
    /// </summary>
    public enum DeskWallpaperPosition
    {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fill = 3,
        Fit = 4,
        Span = 5
    }

    /// <summary>
    /// IDesktopWallpaper COM 接口（Windows 8+），支持按显示器设备路径设置壁纸。
    /// 方法声明顺序即 vtable 顺序，GetPosition 之后的方法未声明（只要不调用即安全）。
    /// </summary>
    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDesktopWallpaper
    {
        [PreserveSig]
        int SetWallpaper(
            [MarshalAs(UnmanagedType.LPWStr)] string monitorId,
            [MarshalAs(UnmanagedType.LPWStr)] string imagePath);

        [PreserveSig]
        int GetWallpaper(
            [MarshalAs(UnmanagedType.LPWStr)] string monitorId,
            out IntPtr wallpaper);

        [PreserveSig]
        int GetMonitorDevicePathAt(uint monitorIndex, out IntPtr monitorId);

        [PreserveSig]
        int GetMonitorDevicePathCount(out uint count);

        [PreserveSig]
        int GetMonitorRECT(
            [MarshalAs(UnmanagedType.LPWStr)] string monitorId,
            out Win32Helper.RECT monitorRect);

        [PreserveSig]
        int SetBackgroundColor(uint color);

        [PreserveSig]
        int GetBackgroundColor(out uint color);

        [PreserveSig]
        int SetPosition(DeskWallpaperPosition position);

        [PreserveSig]
        int GetPosition(out DeskWallpaperPosition position);
    }

    [ComImport]
    [Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
    internal class DesktopWallpaperComObject
    {
    }
}
