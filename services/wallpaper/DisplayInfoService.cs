using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CraftSharp.Helpers;
using CraftSharp.Models;

namespace CraftSharp.Services.Wallpaper
{
    /// <summary>
    /// 显示器扩展只读信息：在 MonitorLayoutService 的 COM 枚举基础上，
    /// 通过 user32 按矩形匹配 HMONITOR 补充设备名/型号/缩放/方向/刷新率。
    /// 复制拓扑下多屏共享同一矩形（EnumDisplayMonitors 只回一个 HMONITOR），
    /// 未匹配到的屏沿用同矩形兄弟屏的取值
    /// </summary>
    public record DisplayInfo(
        MonitorInfo Monitor,
        string DeviceName,
        string FriendlyName,
        uint Dpi,
        int Orientation,
        int Frequency);

    public static class DisplayInfoService
    {
        public static List<DisplayInfo> GetDisplays(List<MonitorInfo>? monitors = null)
        {
            var result = new List<DisplayInfo>();
            var list = monitors ?? MonitorLayoutService.Instance.GetMonitors();
            if (list.Count == 0) return result;

            // 必须在 PMv2 上下文内取矩形与 DPI（同 MonitorLayoutService 惯例）：
            // 系统级 DPI 感知线程上 EnumDisplayMonitors 返回虚拟化矩形（非主屏缩放比例对不上物理 Bounds），
            // 且 GetDpiForMonitor 只回系统 DPI，导致副屏缩放/DPI/工作区全错
            var dpiScope = DpiScope.EnterPerMonitorV2();
            try
            {
                var hmonitors = EnumerateHmonitors();
                var used = new HashSet<IntPtr>();

                foreach (var monitor in list)
                {
                    var device = "";
                    var friendly = "";
                    uint dpi = 96;
                    var orientation = 0;
                    var frequency = 0;

                    var match = hmonitors.FirstOrDefault(h =>
                        !used.Contains(h.Hmonitor) &&
                        h.Rect.Left == monitor.Bounds.Left &&
                        h.Rect.Top == monitor.Bounds.Top &&
                        h.Rect.Right == monitor.Bounds.Right &&
                        h.Rect.Bottom == monitor.Bounds.Bottom);

                    if (match != null)
                    {
                        used.Add(match.Hmonitor);
                        device = match.Device;

                        try
                        {
                            if (GetDpiForMonitor(match.Hmonitor, 0, out var dpiX, out _) >= 0 && dpiX > 0)
                                dpi = dpiX;
                        }
                        catch
                        {
                            // Shcore 不可用时回落 96
                        }

                        var dm = new DEVMODE { dmSize = (short)Marshal.SizeOf<DEVMODE>() };
                        if (EnumDisplaySettings(device, ENUM_CURRENT_SETTINGS, ref dm))
                        {
                            orientation = dm.dmDisplayOrientation;
                            frequency = dm.dmDisplayFrequency;
                        }

                        friendly = GetMonitorName(device);
                    }

                    result.Add(new DisplayInfo(monitor, device, friendly, dpi, orientation, frequency));
                }

                for (var i = 0; i < result.Count; i++)
                {
                    if (result[i].DeviceName.Length > 0) continue;

                    var sibling = result.FirstOrDefault(d =>
                        d.DeviceName.Length > 0 &&
                        d.Monitor.Bounds.Left == result[i].Monitor.Bounds.Left &&
                        d.Monitor.Bounds.Top == result[i].Monitor.Bounds.Top);

                    if (sibling != null)
                        result[i] = result[i] with
                        {
                            Dpi = sibling.Dpi,
                            Orientation = sibling.Orientation,
                            Frequency = sibling.Frequency
                        };
                }

                return result;
            }
            finally
            {
                dpiScope?.Dispose();
            }
        }

        /// <summary>
        /// 呈现模式：extend=扩展 clone=复制 single=仅此屏
        /// </summary>
        public static string GetTopologyKind(List<DisplayInfo> displays)
        {
            if (displays.Count <= 1) return "single";
            var first = displays[0].Monitor.Bounds;
            return displays.All(d =>
                d.Monitor.Bounds.Left == first.Left &&
                d.Monitor.Bounds.Top == first.Top)
                ? "clone" : "extend";
        }

        #region win32 互操作

        private sealed record HmonitorEntry(IntPtr Hmonitor, string Device, Win32Helper.RECT Rect);

        private static List<HmonitorEntry> EnumerateHmonitors()
        {
            var entries = new List<HmonitorEntry>();

            var proc = new MonitorEnumProc(delegate(IntPtr hmon, IntPtr hdc, ref Win32Helper.RECT rect, IntPtr data)
            {
                var info = new MONITORINFOEXW { cbSize = Marshal.SizeOf<MONITORINFOEXW>() };
                var device = GetMonitorInfo(hmon, ref info) ? info.szDevice : "";
                entries.Add(new HmonitorEntry(hmon, device, rect));
                return true;
            });

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);
            return entries;
        }

        /// <summary>
        /// 取某显示器的屏幕/工作区矩形（中心点在 PMv2 上下文命中 HMONITOR，矩形读取在应用线程）。
        /// 实测契约：矩形原点是物理坐标，但对缩放≠系统缩放的屏，尺寸被 OS 折算成 物理尺寸×(sysScale/monScale)
        /// （2560 宽的 150% 副屏报 2133）——原点可直接当物理值，距原点的差值须×monScale/sysScale 还原。
        /// 直接当 WPF DIP 或纯物理矩形用都会错，参见 DisplaySettingsWindow.Identify_Click
        /// </summary>
        public static (Win32Helper.RECT Monitor, Win32Helper.RECT Work) GetAppSpaceBounds(DisplayInfo info)
        {
            var b = info.Monitor.Bounds;
            IntPtr hmon;

            // 物理中心点命中 HMONITOR（DPI 无关），矩形读取回到应用线程上下文取本进程坐标系
            var prev = Win32Helper.SetThreadDpiAwarenessContext(Win32Helper.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            try
            {
                hmon = MonitorFromPoint(new POINT((b.Left + b.Right) / 2, (b.Top + b.Bottom) / 2), 2);
            }
            finally
            {
                if (prev != IntPtr.Zero)
                    Win32Helper.SetThreadDpiAwarenessContext(prev);
            }

            if (hmon != IntPtr.Zero)
            {
                var mi = new MONITORINFOEXW { cbSize = Marshal.SizeOf<MONITORINFOEXW>() };
                if (GetMonitorInfo(hmon, ref mi))
                    return mi.rcWork.Bottom - mi.rcWork.Top > 0
                        ? (mi.rcMonitor, mi.rcWork)
                        : (mi.rcMonitor, mi.rcMonitor);
            }

            return (b, b);
        }

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private static string GetMonitorName(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName)) return "";

            var adapter = new DISPLAY_DEVICEW { cb = Marshal.SizeOf<DISPLAY_DEVICEW>() };
            for (uint i = 0; EnumDisplayDevices(null, i, ref adapter, 0); i++)
            {
                if (adapter.DeviceName == deviceName)
                {
                    var monitor = new DISPLAY_DEVICEW { cb = Marshal.SizeOf<DISPLAY_DEVICEW>() };
                    if (EnumDisplayDevices(deviceName, 0, ref monitor, 0) && !string.IsNullOrEmpty(monitor.DeviceString))
                        return monitor.DeviceString;
                    return "";
                }
                adapter.cb = Marshal.SizeOf<DISPLAY_DEVICEW>();
            }
            return "";
        }

        private const int ENUM_CURRENT_SETTINGS = -1;

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Win32Helper.RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEXW lpmi);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICEW lpDisplayDevice, uint dwFlags);

        [DllImport("Shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MONITORINFOEXW
        {
            public int cbSize;
            public Win32Helper.RECT rcMonitor;
            public Win32Helper.RECT rcWork;
            public int dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DISPLAY_DEVICEW
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        #endregion
    }
}
