using CraftSharp.Models;
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
        private static int GetFontIndex(string font) => font switch { "微软雅黑" => 1, "宋体" => 2, "黑体" => 3, "楷体" => 4, _ => 0 };
        private static int GetIconStyleIndex(string style) => style switch { "像素风格" => 1, "简约风格" => 2, "写实风格" => 3, _ => 0 };

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null) return;
            if (ThemeComboBox.SelectedItem is ComboBoxItem item)
                _settings.Theme = item.Content.ToString() ?? "跟随系统";
        }

        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null) return;
            if (FontComboBox.SelectedItem is ComboBoxItem item)
                _settings.Font = item.Content.ToString() ?? "跟随系统";
        }

        private void IconStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null) return;
            if (IconStyleComboBox.SelectedItem is ComboBoxItem item)
                _settings.IconStyle = item.Content.ToString() ?? "跟随系统";
        }
    }
}