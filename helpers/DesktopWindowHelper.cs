using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;

namespace CraftSharp.Helpers
{
    /// <summary>
    /// 桌面层级窗口助手 - 将窗口放置在桌面图标之上，但在其他应用程序之下
    /// </summary>
    public static class DesktopWindowHelper
    {
        /// <summary>
        /// 将窗口设置为桌面层级（在桌面图标之上，在其他应用程序之下）
        /// </summary>
        public static void SetWindowToDesktopLevel(Window window)
        {
            if (window == null) return;

            // 确保窗口已显示
            if (!window.IsVisible)
            {
                window.Show();
            }

            // 获取窗口句柄
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                // 等待窗口初始化完成
                window.SourceInitialized += (_, _) =>
                {
                    var handle = new WindowInteropHelper(window).Handle;
                    SetParentToDesktop(handle);
                };
            }
            else
            {
                SetParentToDesktop(hwnd);
            }
        }

        /// <summary>
        /// 将窗口句柄设置为桌面层级（在桌面图标之上）
        /// </summary>
        private static void SetParentToDesktop(IntPtr hwnd)
        {
            // 找到 Program Manager 窗口
            IntPtr progman = Win32Helper.FindWindow("Progman", null);

            // 发送消息创建 WorkerW 窗口（使用 SendMessageTimeout 更可靠）
            Win32Helper.SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
                Win32Helper.SMTO_NORMAL, 3000, out _);

            // 查找包含 SHELLDLL_DefView 的 WorkerW（桌面图标所在层）
            IntPtr iconWorkerw = IntPtr.Zero;
            IntPtr workerw = Win32Helper.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);

            while (workerw != IntPtr.Zero)
            {
                IntPtr shelldll = Win32Helper.FindWindowEx(workerw, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shelldll != IntPtr.Zero)
                {
                    iconWorkerw = workerw;
                    break;
                }
                workerw = Win32Helper.FindWindowEx(IntPtr.Zero, workerw, "WorkerW", null);
            }

            // 如果找到了图标 WorkerW，设置父级为其前一个 WorkerW
            if (iconWorkerw != IntPtr.Zero)
            {
                IntPtr prevWorkerw = Win32Helper.FindWindowEx(IntPtr.Zero, iconWorkerw, "WorkerW", null);
                if (prevWorkerw == IntPtr.Zero)
                {
                    prevWorkerw = iconWorkerw;
                }
                Win32Helper.SetParent(hwnd, prevWorkerw);
            }
            else
            {
                Win32Helper.SetParent(hwnd, progman);
            }

            Win32Helper.ApplyToolWindowStyle(hwnd);
        }

        /// <summary>
        /// 将窗口设置为桌面层级并同时隐藏 Alt+Tab
        /// </summary>
        public static void SetWindowToDesktopLevelAndHideAltTab(Window window)
        {
            if (window == null) return;

            // 确保窗口已显示
            if (!window.IsVisible)
            {
                window.Show();
            }

            // 获取窗口句柄
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                // 等待窗口初始化完成
                window.SourceInitialized += (_, _) =>
                {
                    var handle = new WindowInteropHelper(window).Handle;
                    SetParentToDesktop(handle);
                };
            }
            else
            {
                SetParentToDesktop(hwnd);
            }
        }

        /// <summary>
        /// 将窗口设置为桌面背景层级（在桌面图标之下）
        /// </summary>
        public static void SetWindowBehindDesktopIcons(Window window)
        {
            if (window == null) return;

            if (!window.IsVisible)
                window.Show();

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                window.SourceInitialized += (_, _) =>
                {
                    var handle = new WindowInteropHelper(window).Handle;
                    SetParentBehindIcons(handle);
                };
            }
            else
            {
                SetParentBehindIcons(hwnd);
            }
        }

        private static void SetParentBehindIcons(IntPtr hwnd)
        {
            // 1. 找到 Progman 窗口
            IntPtr progman = Win32Helper.FindWindow("Progman", null);
            Debug.WriteLine($"[Wallpaper] Progman handle: {progman}");

            // 2. 发送 0x052C 强制创建 WorkerW 子窗口（使用 SendMessageTimeout 更可靠）
            Win32Helper.SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
                Win32Helper.SMTO_NORMAL, 3000, out _);

            // 3. 查找 Progman 下的 WorkerW 子窗口
            IntPtr workerw = FindWorkerWUnderProgman(progman);
            Debug.WriteLine($"[Wallpaper] WorkerW handle: {workerw}");

            // 4. 如果找不到 WorkerW，回退到直接使用 Progman
            IntPtr targetParent = workerw != IntPtr.Zero ? workerw : progman;
            Debug.WriteLine($"[Wallpaper] Target parent: {targetParent} ({(workerw != IntPtr.Zero ? "WorkerW" : "Progman fallback")})");

            // 5. 设为子窗口
            Win32Helper.SetParent(hwnd, targetParent);
            Win32Helper.ApplyToolWindowStyle(hwnd);

            // 确保壁纸窗口在 Z-Order 最底层
            Win32Helper.SetWindowPos(hwnd, Win32Helper.HWND_BOTTOM,
                0, 0, 0, 0,
                Win32Helper.SWP_NOMOVE | Win32Helper.SWP_NOSIZE | Win32Helper.SWP_FRAMECHANGED);

            Debug.WriteLine($"[Wallpaper] Window {hwnd} parented to {targetParent}");
        }

        /// <summary>
        /// 查找 Progman 下的 WorkerW 子窗口（Win11 24H2 兼容）
        /// </summary>
        private static IntPtr FindWorkerWUnderProgman(IntPtr progman)
        {
            // 方法1：FindWindowEx 直接查找 Progman 的子窗口
            IntPtr workerw = Win32Helper.FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
            if (workerw != IntPtr.Zero)
                return workerw;

            // 方法2：枚举所有顶级窗口，找 Parent == Progman 的 WorkerW
            IntPtr result = IntPtr.Zero;
            Win32Helper.EnumWindows((hWnd, _) =>
            {
                var className = new System.Text.StringBuilder(256);
                Win32Helper.GetClassName(hWnd, className, 256);
                if (className.ToString() == "WorkerW")
                {
                    IntPtr parent = Win32Helper.GetParent(hWnd);
                    if (parent == progman)
                    {
                        result = hWnd;
                        return false; // 停止枚举
                    }
                }
                return true; // 继续枚举
            }, IntPtr.Zero);

            return result;
        }

        /// <summary>
        /// 隐藏窗口在 Alt+Tab 列表中（设置 WS_EX_TOOLWINDOW 样式）
        /// 如果窗口已初始化则立即执行，否则等待 SourceInitialized 事件
        /// </summary>
        public static void HideFromAltTab(Window window)
        {
            if (window == null) return;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd != IntPtr.Zero)
            {
                // 窗口已初始化，立即设置样式
                Win32Helper.ApplyToolWindowStyle(hwnd);
            }
            else
            {
                // 等待窗口初始化
                window.SourceInitialized += (s, e) =>
                {
                    var handle = new WindowInteropHelper(window).Handle;
                    Win32Helper.ApplyToolWindowStyle(handle);
                };
            }
        }
    }
}