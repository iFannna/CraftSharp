using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using CraftSharp.Helpers;

namespace CraftSharp.Windows.Inventory
{
    /// <summary>
    /// 灰色蒙版窗口 - 全屏覆盖，鼠标穿透，不出现在 Alt+Tab 列表中
    /// </summary>
    public partial class GrayOverlayWindow : Window
    {
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
                Win32Helper.ApplyOverlayWindowStyle(hwnd);
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