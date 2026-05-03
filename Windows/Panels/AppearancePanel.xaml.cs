using CraftSharp.Models;
using CraftSharp.Services;
using System.Windows.Controls;

namespace CraftSharp.Windows.Panels
{
    public partial class AppearancePanel : System.Windows.Controls.UserControl
    {
        private AppSettings _settings;

        public AppearancePanel(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            InitializeControls();
        }

        private void InitializeControls()
        {
            ThemeComboBox.SelectedIndex = GetThemeIndex(_settings.Theme);
            FontComboBox.SelectedIndex = GetFontIndex(_settings.Font);
            IconStyleComboBox.SelectedIndex = GetIconStyleIndex(_settings.IconStyle);
        }

        private static int GetThemeIndex(string theme) => theme switch { "暗色" => 1, "亮色" => 2, _ => 0 };
        private static int GetFontIndex(string font) => font switch { "宋体" => 1, "黑体" => 2, "楷体" => 3, "像素字体" => 4, _ => 0 };
        private static int GetIconStyleIndex(string style) => style switch { "像素" => 1, "简约" => 2, "写实" => 3, _ => 0 };

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null) return;
            if (ThemeComboBox.SelectedItem is ComboBoxItem item)
            {
                // 根据 Tag 获取实际的主题值
                var tag = item.Tag?.ToString() ?? "system";
                var themeValue = tag switch
                {
                    "dark" => "暗色",
                    "light" => "亮色",
                    _ => "跟随系统"
                };
                _settings.Theme = themeValue;

                // 使用新的 SetThemeMode 方法切换主题模式
                ThemeService.Instance.SetThemeMode(themeValue);
            }
        }

        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null) return;
            if (FontComboBox.SelectedItem is ComboBoxItem item)
            {
                // 根据 Tag 获取实际的字体值
                var tag = item.Tag?.ToString() ?? "yahei";
                var fontValue = tag switch
                {
                    "songti" => "宋体",
                    "heiti" => "黑体",
                    "kaiti" => "楷体",
                    "pixel" => "像素字体",
                    _ => "微软雅黑"
                };
                _settings.Font = fontValue;

                // 切换字体
                FontService.Instance.SetFont(fontValue);
            }
        }

        private void IconStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null) return;
            if (IconStyleComboBox.SelectedItem is ComboBoxItem item)
            {
                var tag = item.Tag?.ToString() ?? "default";
                var styleValue = tag switch
                {
                    "pixel" => "像素",
                    "simple" => "简约",
                    "realistic" => "写实",
                    _ => "默认"
                };
                _settings.IconStyle = styleValue;
            }
        }
    }
}