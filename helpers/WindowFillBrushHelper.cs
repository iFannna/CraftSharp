using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace CraftSharp.Helpers
{
    /// <summary>
    /// 窗口缩放填充色修正：放大窗口时新暴露区域由 DWM 先行填充，WPF 内容渲染滞后，
    /// 默认按浅色主题填充导致暗色主题下放大闪白。WPF-UI 仅给 MainWindow 设置
    /// DWMWA_USE_IMMERSIVE_DARK_MODE，而本应用从未赋值 MainWindow。
    /// 填充色的具体色值跟随 DWMWA_CAPTION_COLOR，将其钉死为窗口背景色后，
    /// 任意主题色（含未来新增的）都能让填充与背景字节级一致，无需逐主题适配。
    /// 同时替换窗口类背景刷覆盖传统擦除路径。
    /// </summary>
    internal static class WindowFillBrushHelper
    {
        private const int GCLP_HBRBACKGROUND = -10;

        private static readonly Dictionary<int, IntPtr> _brushCache = new();

        /// <summary>
        /// 按窗口当前背景色更新其窗口类背景刷
        /// </summary>
        public static void Update(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var color = ResolveFillColor(window);
            if (color is null)
            {
                return;
            }

            SetClassLongPtr(hwnd, GCLP_HBRBACKGROUND, GetBrush(color.Value));
            ApplyDwmColors(hwnd, color.Value);
        }

        /// <summary>
        /// DWM 暗色标记决定系统部件（标题栏等）的明暗组；
        /// DWMWA_CAPTION_COLOR 决定放大窗口时 DWM 填充新暴露区域的颜色
        /// </summary>
        private static void ApplyDwmColors(IntPtr hwnd, Color color)
        {
            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
            const int DWMWA_CAPTION_COLOR = 35;

            int darkMode = (color.R + color.G + color.B) / 3 < 0x80 ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // COLORREF 布局为 0x00BBGGRR
            int colorRef = unchecked((int)(color.R | ((uint)color.G << 8) | ((uint)color.B << 16)));
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref colorRef, sizeof(int));
        }

        /// <summary>
        /// 主题变化后刷新所有已打开窗口
        /// </summary>
        public static void RefreshAll()
        {
            foreach (Window window in Application.Current.Windows)
            {
                Update(window);
            }
        }

        /// <summary>
        /// 释放缓存的 GDI 画刷
        /// </summary>
        public static void Cleanup()
        {
            foreach (IntPtr brush in _brushCache.Values)
            {
                DeleteObject(brush);
            }
            _brushCache.Clear();
        }

        private static Color? ResolveFillColor(Window window)
        {
            if (window.Background is SolidColorBrush solid && solid.Color.A > 0)
            {
                return solid.Color;
            }
            // 背景Transparent或图片等情况下，退回主题背景色作为缩放填充
            if (Application.Current.Resources["ApplicationBackgroundBrush"] is SolidColorBrush theme)
            {
                return theme.Color;
            }
            return null;
        }

        private static IntPtr GetBrush(Color color)
        {
            int key = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
            if (!_brushCache.TryGetValue(key, out IntPtr brush))
            {
                // COLORREF 布局为 0x00BBGGRR
                uint colorRef = color.R | ((uint)color.G << 8) | ((uint)color.B << 16);
                brush = CreateSolidBrush(colorRef);
                _brushCache[key] = brush;
            }
            return brush;
        }

        [DllImport("user32.dll", EntryPoint = "SetClassLongPtrW")]
        private static extern IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(uint color);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
