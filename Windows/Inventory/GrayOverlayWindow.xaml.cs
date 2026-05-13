using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace CraftSharp.Windows.Inventory
{
    /// <summary>
    /// 灰色蒙版窗口 - 全屏覆盖，鼠标穿透，不出现在 Alt+Tab 列表中
    /// </summary>
    public partial class GrayOverlayWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        /// <summary>
        /// 创建灰色蒙版窗口
        /// </summary>
        /// <param name="opacity">透明度值（0-100，默认50）</param>
        public GrayOverlayWindow(int opacity = 50)
        {
            InitializeComponent();

            // 设置窗口覆盖全屏
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;

            // 根据透明度设置背景颜色
            SetOpacity(opacity);

            // 设置 WS_EX_TOOLWINDOW + WS_EX_TRANSPARENT + WS_EX_LAYERED 样式
            // TOOLWINDOW: 隐藏 Alt+Tab
            // TRANSPARENT: 鼠标穿透
            // LAYERED: 支持透明窗口
            SourceInitialized += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT | WS_EX_LAYERED);

                // 强制刷新窗口样式
                SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
            };
        }

        /// <summary>
        /// 设置透明度
        /// </summary>
        /// <param name="opacity">透明度值（0-100）</param>
        public void SetOpacity(int opacity)
        {
            byte alpha = (byte)(opacity * 255 / 100);
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(alpha, 0, 0, 0));
        }
    }
}