using System;
using CraftSharp.Helpers;

namespace CraftSharp.Helpers
{
    /// <summary>
    /// 线程级 PerMonitorV2 DPI 上下文作用域。
    /// 用于显示器枚举与壁纸宿主窗口创建，保证拿到/使用物理像素坐标，
    /// 不受进程默认 DPI 感知级别影响。Win10 1703 以下系统返回 null。
    /// </summary>
    public readonly struct DpiScope : IDisposable
    {
        private readonly IntPtr _previous;

        private DpiScope(IntPtr previous) => _previous = previous;

        public static DpiScope? EnterPerMonitorV2()
        {
            var previous = Win32Helper.SetThreadDpiAwarenessContext(
                Win32Helper.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            return previous == IntPtr.Zero ? null : new DpiScope(previous);
        }

        public void Dispose() => Win32Helper.SetThreadDpiAwarenessContext(_previous);
    }
}
