using System;
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
        /// 将窗口句柄设置为桌面层级
        /// </summary>
        private static void SetParentToDesktop(IntPtr hwnd)
        {
            // 找到 Program Manager 窗口
            IntPtr progman = Win32Helper.FindWindow("Progman", null);

            // 发送消息创建 WorkerW 窗口（这是 Windows 7+ 的方式）
            // 0x052C 是一个 undocumented 消息，用于在 Progman 下创建一个新的 WorkerW
            Win32Helper.SendMessage(progman, Win32Helper.WM_COMMAND, (IntPtr)0x052C, IntPtr.Zero);

            // 找到 WorkerW 窗口
            IntPtr workerw = Win32Helper.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);

            // 遍历找到正确的 WorkerW（在 Progman 下的那个）
            while (workerw != IntPtr.Zero)
            {
                IntPtr shelldll = Win32Helper.FindWindowEx(workerw, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shelldll != IntPtr.Zero)
                {
                    // 找到了正确的 WorkerW
                    IntPtr desktop = Win32Helper.FindWindowEx(shelldll, IntPtr.Zero, "SysListView32", null);
                    if (desktop != IntPtr.Zero)
                    {
                        // 找到下一个 WorkerW（我们要将窗口放在这个下面）
                        IntPtr tempWorkerw = Win32Helper.FindWindowEx(IntPtr.Zero, workerw, "WorkerW", null);
                        if (tempWorkerw != IntPtr.Zero)
                        {
                            workerw = tempWorkerw;
                        }
                        break;
                    }
                }
                workerw = Win32Helper.FindWindowEx(IntPtr.Zero, workerw, "WorkerW", null);
            }

            // 设置窗口父级为 WorkerW
            Win32Helper.SetParent(hwnd, workerw);

            // SetParent 会重置样式，需要重新设置 WS_EX_TOOLWINDOW
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