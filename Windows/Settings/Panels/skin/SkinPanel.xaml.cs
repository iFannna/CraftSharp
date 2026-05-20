using CraftSharp.Models;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace CraftSharp.Windows.Settings.Panels.Skin
{
    public partial class SkinPanel : global::System.Windows.Controls.UserControl
    {
        private AppSettings _settings;
        private global::System.Windows.Controls.Border[] _optionBorders;
        private global::System.Windows.Controls.TextBlock[] _optionTexts;

        public SkinPanel(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            _optionBorders = new global::System.Windows.Controls.Border[] { BorderSteve, BorderAlex, BorderUpload };
            _optionTexts = new global::System.Windows.Controls.TextBlock[] { TextSteve, TextAlex, TextUpload };
        }

        public void SetParentWindow(global::System.Windows.Window parent)
        {
            // 后续实现
        }

        private void OptionBorder_Click(object sender, global::System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is global::System.Windows.Controls.Border clickedBorder)
            {
                for (int i = 0; i < _optionBorders.Length; i++)
                {
                    var border = _optionBorders[i];
                    var text = _optionTexts[i];

                    if (border == clickedBorder)
                    {
                        // 选中状态：有背景，文字加粗
                        border.Background = TryFindResource("AccentBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(0, 120, 215));
                        text.Foreground = TryFindResource("TextPrimaryBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        text.FontWeight = FontWeights.Medium;
                    }
                    else
                    {
                        // 未选中状态：无背景，文字次要色
                        border.Background = Brushes.Transparent;
                        text.Foreground = TryFindResource("TextSecondaryBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(150, 150, 150));
                        text.FontWeight = FontWeights.Normal;
                    }
                }
            }
        }
    }
}