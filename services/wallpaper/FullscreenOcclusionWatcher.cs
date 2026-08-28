using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
using CraftSharp.Helpers;

namespace CraftSharp.Services.Wallpaper
{
    /// <summary>
    /// 一秒轮询看门狗：前台全屏窗口盖住某路动态壁纸时通过 IPC 暂停对应 mpv（省 CPU/GPU），
    /// 前台切换走后自动恢复；宿主窗口被 WorkerW 重建连带销毁时原位重建自愈。
    /// </summary>
    public class FullscreenOcclusionWatcher
    {
        public static FullscreenOcclusionWatcher Instance { get; } = new();

        private readonly DispatcherTimer _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        private readonly Dictionary<string, bool> _paused = new();
        private bool _initialized;

        private FullscreenOcclusionWatcher() { }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _timer.Tick += (_, _) => Poll();
            _timer.Start();
        }

        private void Poll()
        {
            var service = DynamicWallpaperService.Instance;
            if (!service.IsPlaying)
            {
                _paused.Clear();
                return;
            }

            var foreground = Win32Helper.GetForegroundWindow();
            if (foreground == IntPtr.Zero) return;

            // 点击桌面时前台是 Progman/WorkerW，其矩形覆盖整个虚拟屏，
            // 会被误判为全屏遮挡——而桌面恰恰是最需要播壁纸的场景，视为未遮挡
            var className = new System.Text.StringBuilder(256);
            Win32Helper.GetClassName(foreground, className, 256);
            var isDesktopShell = className.ToString() is "Progman" or "WorkerW" or "SHELLDLL_DefView";

            // GetWindowRect 返回值跟随调用线程的 DPI 上下文虚拟化，
            // 必须切到 PMv2 拿物理像素，才能与壁纸窗口的物理 bounds 比较
            Win32Helper.RECT fr;
            using (DpiScope.EnterPerMonitorV2())
            {
                if (!Win32Helper.GetWindowRect(foreground, out fr)) return;
            }

            foreach (var key in service.ActiveKeys)
            {
                // 宿主窗口被 WorkerW 延迟重建连带销毁（mpv 随之退出）时原位重建，
                // 编排层快照对此无感知，只有这里能自愈
                if (!service.IsHostAlive(key))
                {
                    Debug.WriteLine($"[Wallpaper] Host dead for {key}, restarting");
                    _paused.Remove(key);
                    _ = service.RestartAsync(key);
                    continue;
                }

                var bounds = service.GetBounds(key);
                if (bounds == null) continue;

                var covered =
                    !isDesktopShell &&
                    fr.Left <= bounds.Value.Left + 2 &&
                    fr.Top <= bounds.Value.Top + 2 &&
                    fr.Right >= bounds.Value.Right - 2 &&
                    fr.Bottom >= bounds.Value.Bottom - 2;

                if (_paused.TryGetValue(key, out var was) && was == covered) continue;
                _paused[key] = covered;
                _ = service.SetPausedAsync(key, covered);
                Debug.WriteLine($"[Wallpaper] Occlusion pause {(covered ? "ON " : "OFF")} for {key}");
            }

            foreach (var stale in new List<string>(_paused.Keys))
                if (!service.IsPlayingForKey(stale))
                    _paused.Remove(stale);
        }
    }
}
