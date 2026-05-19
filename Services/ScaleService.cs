using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace CraftSharp.Services
{
    /// <summary>
    /// 屏幕缩放服务 - 基于屏幕分辨率统一计算缩放比例
    ///
    /// 缩放规则：
    /// - 基准分辨率：2560（2560px宽度下）
    /// - 基准放大倍数：6倍
    /// - 当前缩放比例 = (当前屏幕宽度 / 2560) * 6
    /// </summary>
    public class ScaleService
    {
        private static ScaleService? _instance;
        public static ScaleService Instance => _instance ??= new ScaleService();

        // 基准分辨率：2560下放大6倍
        public const double BaseScreenWidth = 2560;
        public const double BaseScaleMultiplier = 6;

        private double _scaleFactor;
        private double _screenWidth;
        private double _screenHeight;
        private double _dpiScaleX = 1.0;
        private double _dpiScaleY = 1.0;

        public double ScaleFactor => _scaleFactor;
        public double ScreenWidth => _screenWidth;
        public double ScreenHeight => _screenHeight;
        public double DpiScaleX => _dpiScaleX;
        public double DpiScaleY => _dpiScaleY;

        // Win32 API for screen info
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const uint MONITOR_DEFAULTTOPRIMARY = 1;

        /// <summary>
        /// 初始化缩放服务（使用 SystemParameters.PrimaryScreenWidth）
        /// </summary>
        public void Initialize()
        {
            _screenWidth = SystemParameters.PrimaryScreenWidth;
            _screenHeight = SystemParameters.PrimaryScreenHeight;
            _scaleFactor = (_screenWidth / BaseScreenWidth) * BaseScaleMultiplier;
        }

        /// <summary>
        /// 初始化缩放服务（考虑DPI缩放，用于覆盖全屏的窗口）
        /// 从PresentationSource获取DPI缩放因子，并转换物理像素到WPF逻辑像素
        /// </summary>
        public void InitializeWithDpi(PresentationSource? presentationSource)
        {
            // 获取DPI缩放因子
            if (presentationSource != null)
            {
                _dpiScaleX = presentationSource.CompositionTarget.TransformToDevice.M11;
                _dpiScaleY = presentationSource.CompositionTarget.TransformToDevice.M22;
            }
            else
            {
                _dpiScaleX = 1.0;
                _dpiScaleY = 1.0;
            }

            // 使用 Win32 API 获取主显示器物理像素尺寸
            IntPtr monitor = MonitorFromPoint(new POINT { X = 0, Y = 0 }, MONITOR_DEFAULTTOPRIMARY);
            var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(monitor, ref monitorInfo))
            {
                double physicalWidth = monitorInfo.rcMonitor.Right - monitorInfo.rcMonitor.Left;
                double physicalHeight = monitorInfo.rcMonitor.Bottom - monitorInfo.rcMonitor.Top;

                // 转换为 WPF 逻辑像素
                _screenWidth = physicalWidth / _dpiScaleX;
                _screenHeight = physicalHeight / _dpiScaleY;
            }
            else
            {
                // 回退到 WPF 原生 API
                _screenWidth = SystemParameters.PrimaryScreenWidth;
                _screenHeight = SystemParameters.PrimaryScreenHeight;
            }

            _scaleFactor = (_screenWidth / BaseScreenWidth) * BaseScaleMultiplier;
        }
    }
}