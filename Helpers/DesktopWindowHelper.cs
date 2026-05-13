using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CraftSharp.Helpers
{
    /// <summary>
    /// 桌面层级窗口助手 - 将窗口放置在桌面图标之上，但在其他应用程序之下
    /// </summary>
    public static class DesktopWindowHelper
    {
        #region Win32 API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int WM_COMMAND = 0x0111;
        private const int WM_USER = 0x0400;
        private const int SW_SHOW = 5;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        #endregion

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
                window.SourceInitialized += (s, e) =>
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
            IntPtr progman = FindWindow("Progman", null);

            // 发送消息创建 WorkerW 窗口（这是 Windows 7+ 的方式）
            // 0x052C 是一个 undocumented 消息，用于在 Progman 下创建一个新的 WorkerW
            SendMessage(progman, WM_COMMAND, (IntPtr)0x052C, IntPtr.Zero);

            // 找到 WorkerW 窗口
            IntPtr workerw = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);

            // 遦历找到正确的 WorkerW（在 Progman 下的那个）
            while (workerw != IntPtr.Zero)
            {
                IntPtr shelldll = FindWindowEx(workerw, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shelldll != IntPtr.Zero)
                {
                    // 找到了正确的 WorkerW
                    IntPtr desktop = FindWindowEx(shelldll, IntPtr.Zero, "SysListView32", null);
                    if (desktop != IntPtr.Zero)
                    {
                        // 找到下一个 WorkerW（我们要将窗口放在这个下面）
                        IntPtr tempWorkerw = FindWindowEx(IntPtr.Zero, workerw, "WorkerW", null);
                        if (tempWorkerw != IntPtr.Zero)
                        {
                            workerw = tempWorkerw;
                        }
                        break;
                    }
                }
                workerw = FindWindowEx(IntPtr.Zero, workerw, "WorkerW", null);
            }

            // 设置窗口父级为 WorkerW
            SetParent(hwnd, workerw);

            // SetParent 会重置样式，需要重新设置 WS_EX_TOOLWINDOW
            ApplyToolWindowStyle(hwnd);
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
                window.SourceInitialized += (s, e) =>
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
                ApplyToolWindowStyle(hwnd);
            }
            else
            {
                // 等待窗口初始化
                window.SourceInitialized += (s, e) =>
                {
                    var handle = new WindowInteropHelper(window).Handle;
                    ApplyToolWindowStyle(handle);
                };
            }
        }

        /// <summary>
        /// 应用 WS_EX_TOOLWINDOW 样式
        /// </summary>
        private static void ApplyToolWindowStyle(IntPtr hwnd)
        {
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }
    }
}