using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace CraftSharp.Helpers
{
    /// <summary>
    /// 模态弹窗辅助：拦截点击被禁用主窗口时系统的提示音与弹窗闪烁
    /// </summary>
    public static class ModalDialogHelper
    {
        /// <summary>
        /// 模态显示窗口，并在期间吞掉落在本进程被禁用窗口上的鼠标点击
        /// </summary>
        public static bool? ShowDialogQuiet(this Window window)
        {
            DisabledClickFilter.Acquire();
            try
            {
                return window.ShowDialog();
            }
            finally
            {
                DisabledClickFilter.Release();
            }
        }
    }

    /// <summary>
    /// WH_MOUSE_LL 低级鼠标钩子，引用计数管理，仅在本进程存在被禁用的顶层窗口时拦截点击。
    /// 回调异常时放行事件，最坏退回系统默认行为（提示音 + 闪烁）。
    /// </summary>
    internal static class DisabledClickFilter
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int GWL_STYLE = -16;
        private const int WS_DISABLED = 0x08000000;

        private static int _refCount;
        private static IntPtr _hookId = IntPtr.Zero;
        // 静态字段持有委托，防止被 GC 回收导致钩子失效
        private static readonly HookProc _hookProc = HookCallback;

        public static void Acquire()
        {
            if (Interlocked.Increment(ref _refCount) != 1)
            {
                return;
            }
            _hookId = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, GetModuleHandle(null), 0);
        }

        public static void Release()
        {
            if (Interlocked.Decrement(ref _refCount) != 0)
            {
                return;
            }
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && IsButtonDown((int)wParam))
                {
                    var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    if (IsOverDisabledWindow(info.pt))
                    {
                        return new IntPtr(1);
                    }
                }
            }
            catch
            {
                // 放行事件
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static bool IsButtonDown(int message)
        {
            return message == WM_LBUTTONDOWN || message == WM_RBUTTONDOWN
                || message == WM_MBUTTONDOWN || message == WM_XBUTTONDOWN;
        }

        // 按 Z 序自顶向下找第一个包含该点且可命中的窗口（即系统实际路由点击的目标），
        // 仅当它属于本进程且被禁用时才吞掉。
        // 不能只做矩形包含判断：弹窗覆盖在属主窗口中央，否则弹窗自身的点击也会被吞掉
        private static bool IsOverDisabledWindow(Win32Helper.POINT pt)
        {
            int result = -1; // -1 未命中，0 放行，1 吞掉
            int processId = Environment.ProcessId;
            Win32Helper.EnumWindows((hwnd, _) =>
            {
                if (!IsWindowVisible(hwnd) || IsTransparent(hwnd) || IsCloaked(hwnd))
                {
                    return true;
                }
                Win32Helper.GetWindowRect(hwnd, out Win32Helper.RECT rect);
                if (pt.X < rect.Left || pt.X >= rect.Right || pt.Y < rect.Top || pt.Y >= rect.Bottom)
                {
                    return true;
                }
                Win32Helper.GetWindowThreadProcessId(hwnd, out int windowProcessId);
                result = windowProcessId == processId
                    && (Win32Helper.GetWindowLong(hwnd, GWL_STYLE) & WS_DISABLED) != 0 ? 1 : 0;
                return false;
            }, IntPtr.Zero);
            return result == 1;
        }

        private static bool IsTransparent(IntPtr hwnd)
        {
            const int WS_EX_TRANSPARENT = 0x00000020;
            return (Win32Helper.GetWindowLong(hwnd, Win32Helper.GWL_EXSTYLE) & WS_EX_TRANSPARENT) != 0;
        }

        private static bool IsCloaked(IntPtr hwnd)
        {
            const int DWMWA_CLOAKED = 14;
            return DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0;
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public Win32Helper.POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);
    }
}
