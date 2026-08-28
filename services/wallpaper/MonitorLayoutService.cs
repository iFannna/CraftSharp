using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CraftSharp.Helpers;
using CraftSharp.Models;

namespace CraftSharp.Services.Wallpaper
{
    /// <summary>
    /// 显示器布局服务 - 以 IDesktopWallpaper COM 为单一事实来源，
    /// 提供设备路径、物理像素矩形和拓扑指纹。COM 不可用时返回空集合，调用方降级。
    /// </summary>
    public class MonitorLayoutService
    {
        public static MonitorLayoutService Instance { get; } = new();

        private IDesktopWallpaper? _com;
        private bool _comAvailable;

        private MonitorLayoutService() { }

        public void Initialize()
        {
            try
            {
                _com = (IDesktopWallpaper)new DesktopWallpaperComObject();
                // 不调用 SetPosition：Win11 上它会异步把注册表 WallpaperStyle 改写为 Fit，
                // 显示位置的维护统一由 DesktopWallpaperService.EnsureFillPosition 负责
                _comAvailable = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MonitorLayout] COM init failed: {ex.Message}");
                _com = null;
                _comAvailable = false;
            }
        }

        public bool IsAvailable => _comAvailable;

        /// <summary>
        /// 枚举所有在场显示器（物理像素坐标）。COM 不可用时返回空列表。
        /// </summary>
        public List<MonitorInfo> GetMonitors()
        {
            if (!_comAvailable || _com == null)
                return new List<MonitorInfo>();

            var scope = DpiScope.EnterPerMonitorV2();
            try
            {
                var hrCount = _com.GetMonitorDevicePathCount(out uint count);
                if (hrCount < 0)
                {
                    Debug.WriteLine($"[MonitorLayout] GetMonitorDevicePathCount hr=0x{hrCount:X8}");
                    return new List<MonitorInfo>();
                }

                var monitors = new List<MonitorInfo>();
                for (uint i = 0; i < count; i++)
                {
                    // 部分系统存在幽灵条目（空路径/E_FAIL），逐台容错跳过
                    if (_com.GetMonitorDevicePathAt(i, out IntPtr idPtr) < 0 || idPtr == IntPtr.Zero)
                        continue;

                    var devicePath = Marshal.PtrToStringUni(idPtr) ?? "";
                    Marshal.FreeCoTaskMem(idPtr);

                    if (string.IsNullOrEmpty(devicePath))
                        continue;

                    if (_com.GetMonitorRECT(devicePath, out var rect) < 0)
                        continue;
                    // 已断开的显示器返回空矩形
                    if (rect.Left == rect.Right || rect.Top == rect.Bottom)
                        continue;

                    monitors.Add(new MonitorInfo(
                        devicePath,
                        rect,
                        IsPrimary: rect.Left <= 0 && rect.Top <= 0 && rect.Right > 0 && rect.Bottom > 0,
                        Index: 0));
                }

                var ordered = monitors.OrderBy(m => m.Bounds.Left).ThenBy(m => m.Bounds.Top).ToList();
                for (var i = 0; i < ordered.Count; i++)
                    ordered[i] = ordered[i] with { Index = i + 1 };

                foreach (var m in ordered)
                    Debug.WriteLine($"[MonitorLayout] #{m.Index} Primary={m.IsPrimary} {m.Width}x{m.Height} @({m.Bounds.Left},{m.Bounds.Top}) {m.DevicePath}");

                return ordered;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MonitorLayout] Enumerate failed: {ex.Message}");
                return new List<MonitorInfo>();
            }
            finally
            {
                scope?.Dispose();
            }
        }

        /// <summary>
        /// 虚拟桌面总边界（各屏矩形并集）
        /// </summary>
        public Win32Helper.RECT GetVirtualScreenBounds(List<MonitorInfo>? monitors = null)
        {
            var list = monitors ?? GetMonitors();
            if (list.Count == 0)
                return new Win32Helper.RECT
                {
                    Left = Win32Helper.GetSystemMetrics(Win32Helper.SM_XVIRTUALSCREEN),
                    Top = Win32Helper.GetSystemMetrics(Win32Helper.SM_YVIRTUALSCREEN),
                    Right = Win32Helper.GetSystemMetrics(Win32Helper.SM_XVIRTUALSCREEN) + Win32Helper.GetSystemMetrics(Win32Helper.SM_CXVIRTUALSCREEN),
                    Bottom = Win32Helper.GetSystemMetrics(Win32Helper.SM_YVIRTUALSCREEN) + Win32Helper.GetSystemMetrics(Win32Helper.SM_CYVIRTUALSCREEN)
                };

            return new Win32Helper.RECT
            {
                Left = list.Min(m => m.Bounds.Left),
                Top = list.Min(m => m.Bounds.Top),
                Right = list.Max(m => m.Bounds.Right),
                Bottom = list.Max(m => m.Bounds.Bottom)
            };
        }

        /// <summary>
        /// 拓扑指纹（设备路径+矩形 排序哈希），用于拼接裁片缓存失效判断
        /// </summary>
        public string GetTopologyFingerprint(List<MonitorInfo>? monitors = null)
        {
            var list = (monitors ?? GetMonitors())
                .OrderBy(m => m.DevicePath, StringComparer.Ordinal)
                .Select(m => $"{m.DevicePath}|{m.Bounds.Left},{m.Bounds.Top},{m.Bounds.Right},{m.Bounds.Bottom}");

            var joined = string.Join(";", list);
            var hash = 17;
            foreach (var c in joined)
                hash = hash * 31 + c;
            return $"v1_{unchecked((uint)hash):X8}";
        }
    }
}
