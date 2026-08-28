using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using CraftSharp.Helpers;

namespace CraftSharp.Services.Wallpaper
{
    /// <summary>
    /// 显示器热插拔/分辨率/DPI 变更监听：去抖后触发壁纸重排。
    /// 分屏模式下不在场的屏自动跳过（配置保留，插回自动恢复）；
    /// 拼接模式下拓扑指纹变化会自动重新裁片。
    /// </summary>
    public class DisplayChangeWatcher
    {
        public static DisplayChangeWatcher Instance { get; } = new();

        private readonly DispatcherTimer _debounce = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(800)
        };
        private bool _initialized;

        private DisplayChangeWatcher() { }

        public void Initialize(Window hiddenHost)
        {
            if (_initialized) return;
            _initialized = true;

            _debounce.Tick += (_, _) =>
            {
                _debounce.Stop();
                Debug.WriteLine("[Wallpaper] Display change debounce elapsed, applying layout");
                _ = WallpaperService.Instance.ApplyLayoutAsync();
            };

            Hook(hiddenHost);
        }

        private void Hook(Window host)
        {
            var hwnd = new WindowInteropHelper(host).Handle;
            if (hwnd == IntPtr.Zero)
            {
                host.SourceInitialized += (_, _) => Hook(host);
                return;
            }

            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32Helper.WM_DISPLAYCHANGE || msg == Win32Helper.WM_DPICHANGED)
            {
                Debug.WriteLine($"[Wallpaper] Display change detected (msg=0x{msg:X4}), relayout scheduled");
                _debounce.Stop();
                _debounce.Start();
            }
            return IntPtr.Zero;
        }
    }
}
