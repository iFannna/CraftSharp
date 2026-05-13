using CraftSharp.Models;
using CraftSharp.Services;
using CraftSharp.Windows.Dialogs;
using System.Windows.Controls;

namespace CraftSharp.Windows.Settings.Panels
{
    public partial class AppearancePanel : System.Windows.Controls.UserControl
    {
        private AppSettings _settings;
        private System.Windows.Window? _parentWindow;

        public AppearancePanel(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            InitializeControls();
        }

        public void SetParentWindow(System.Windows.Window parent)
        {
            _parentWindow = parent;
        }

        private void InitializeControls()
        {
            ThemeComboBox.SelectedIndex = GetThemeIndex(_settings.Appearance.Theme);
            FontComboBox.SelectedIndex = GetFontIndex(_settings.Appearance.Font);
            FontSizeComboBox.SelectedIndex = GetFontSizeIndex(_settings.Appearance.FontSize);
            LoadAppIconPreview();
        }

        private void LoadAppIconPreview()
        {
            var preview = IconService.Instance.GetIconPreview(_settings.Appearance.AppIconPath);
            if (preview != null)
            {
                AppIconPreview.Source = preview;
            }
        }

        private static int GetThemeIndex(string theme) => theme switch { "暗色" => 1, "亮色" => 2, _ => 0 };

        private static int GetFontIndex(string fontTag)
        {
            // 处理旧版本存储的中文名称
            var tag = fontTag switch
            {
                "像素字体" => "pixel",
                "统一字体" => "unifont",
                "宋体" => "songti",
                "黑体" => "heiti",
                "楷体" => "kaiti",
                _ => fontTag
            };
            return tag switch { "pixel" => 1, "unifont" => 2, "songti" => 3, "kaiti" => 4, "heiti" => 5, _ => 0 };
        }

        private static int GetFontSizeIndex(int fontSize) => fontSize switch { 10 => 0, 12 => 1, 16 => 3, 18 => 4, 20 => 5, _ => 2 };

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
                _settings.Appearance.Theme = themeValue;

                // 使用新的 SetThemeMode 方法切换主题模式
                ThemeService.Instance.SetThemeMode(themeValue);

                // 即时保存设置
                SaveSettings();
            }
        }

        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null) return;
            if (FontComboBox.SelectedItem is ComboBoxItem item)
            {
                // 直接使用 Tag 作为字体标识符存储
                var tag = item.Tag?.ToString() ?? "yahei";
                _settings.Appearance.Font = tag;

                // 切换字体（使用标识符）
                FontService.Instance.SetFont(tag);

                // 即时保存设置
                SaveSettings();
            }
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null) return;
            if (FontSizeComboBox.SelectedItem is ComboBoxItem item)
            {
                // 根据 Tag 获取实际的字体大小值
                var tag = item.Tag?.ToString() ?? "14";
                if (int.TryParse(tag, out var fontSize))
                {
                    _settings.Appearance.FontSize = fontSize;

                    // 切换字体大小
                    FontService.Instance.SetFontSize(fontSize);

                    // 即时保存设置
                    SaveSettings();
                }
            }
        }

        private void AppIconPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var picker = new IconPickerWindow();
            picker.Owner = _parentWindow;

            if (picker.ShowDialog() == true && picker.SelectedIconPath != null)
            {
                // 更新设置
                _settings.Appearance.AppIconPath = picker.SelectedIconPath;

                // 更新图标
                IconService.Instance.SetAppIcon(picker.SelectedIconPath);

                // 更新预览
                LoadAppIconPreview();

                // 即时保存设置
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            if (System.Windows.Application.Current is App app)
            {
                app.SaveSettings();
            }
        }
    }
}