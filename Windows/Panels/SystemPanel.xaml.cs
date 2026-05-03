using CraftSharp.Models;
using CraftSharp.Services;
using System.Windows.Controls;

namespace CraftSharp.Windows.Panels
{
    public partial class SystemPanel : System.Windows.Controls.UserControl
    {
        private AppSettings _settings;

        public SystemPanel(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            InitializeControls();
        }

        private void InitializeControls()
        {
            LanguageComboBox.SelectedIndex = _settings.Language == "简体中文" ? 0 : 1;
            AutoStartToggle.IsChecked = _settings.AutoStart;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null) return;
            if (LanguageComboBox.SelectedItem is ComboBoxItem item)
            {
                // 使用显示名称作为设置值
                var language = item.Content.ToString() ?? "简体中文";
                _settings.Language = language;

                // 切换语言
                LocalizationService.Instance.SetLanguage(language);
            }
        }

        private void AutoStartToggle_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch toggle)
                _settings.AutoStart = toggle.IsChecked ?? false;
        }
    }
}