using CraftSharp.Models;
using CraftSharp.Services;
using CraftSharp.Windows;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using IOPath = System.IO.Path;

namespace CraftSharp.Windows.Panels
{
    public partial class AppearancePanel : System.Windows.Controls.UserControl
    {
        private AppSettings _settings;
        private System.Windows.Window? _parentWindow;
        private string _settingsPath;

        public AppearancePanel(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            _settingsPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            InitializeControls();
        }

        public void SetParentWindow(System.Windows.Window parent)
        {
            _parentWindow = parent;
        }

        private void InitializeControls()
        {
            ThemeComboBox.SelectedIndex = GetThemeIndex(_settings.Theme);
            FontComboBox.SelectedIndex = GetFontIndex(_settings.Font);
            LoadAppIconPreview();
        }

        private void LoadAppIconPreview()
        {
            var preview = IconService.Instance.GetIconPreview(_settings.AppIconPath);
            if (preview != null)
            {
                AppIconPreview.Source = preview;
            }
        }

        private static int GetThemeIndex(string theme) => theme switch { "暗色" => 1, "亮色" => 2, _ => 0 };
        private static int GetFontIndex(string font) => font switch { "宋体" => 1, "黑体" => 2, "楷体" => 3, "像素字体" => 4, _ => 0 };

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

        private void AppIconPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var picker = new IconPickerWindow();
            picker.Owner = _parentWindow;

            if (picker.ShowDialog() == true && picker.SelectedIconPath != null)
            {
                // 更新设置
                _settings.AppIconPath = picker.SelectedIconPath;

                // 更新图标
                IconService.Instance.SetAppIcon(picker.SelectedIconPath);

                // 更新预览
                LoadAppIconPreview();

                // 保存设置到配置文件
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                System.IO.File.WriteAllText(_settingsPath, json);
            }
            catch { }
        }
    }
}