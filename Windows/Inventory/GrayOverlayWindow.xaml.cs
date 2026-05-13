using System.Windows;

namespace CraftSharp.Windows.Inventory
{
    /// <summary>
    /// 灰色蒙版窗口 - 全屏覆盖，50%透明深灰色，不响应点击
    /// </summary>
    public partial class GrayOverlayWindow : Window
    {
        public GrayOverlayWindow()
        {
            InitializeComponent();

            // 设置窗口覆盖全屏
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
        }
    }
}