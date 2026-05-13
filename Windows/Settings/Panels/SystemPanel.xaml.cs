using CraftSharp.Models;
using CraftSharp.Services;
using System;
using System.Windows.Controls;

namespace CraftSharp.Windows.Settings.Panels
{
    public partial class SystemPanel : System.Windows.Controls.UserControl
    {
        private AppSettings _settings;

        /// <summary>
        /// 卡片状态记忆开关变化事件
        /// </summary>
        public event EventHandler<bool>? CardStatesRememberChanged;

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
            RememberPositionToggle.IsChecked = _settings.SettingsWindowRememberPosition;
            RememberSizeToggle.IsChecked = _settings.SettingsWindowRememberSize;
            RememberCardStatesToggle.IsChecked = _settings.RememberCardStates; // 默认开启
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

                // 即时保存设置
                SaveSettings();
            }
        }

        private void AutoStartToggle_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch toggle)
            {
                _settings.AutoStart = toggle.IsChecked ?? false;
                // 即时保存设置
                SaveSettings();
            }
        }

        private void RememberPositionToggle_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch toggle)
            {
                _settings.SettingsWindowRememberPosition = toggle.IsChecked ?? false;
                // 即时保存设置
                SaveSettings();
            }
        }

        private void RememberSizeToggle_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch toggle)
            {
                _settings.SettingsWindowRememberSize = toggle.IsChecked ?? false;
                // 即时保存设置
                SaveSettings();
            }
        }

        private void RememberCardStatesToggle_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch toggle)
            {
                bool isChecked = toggle.IsChecked ?? false;
                _settings.RememberCardStates = isChecked;

                // 如果关闭开关，清空已保存的状态
                if (!isChecked)
                {
                    _settings.CardExpandedStates.Clear();
                }

                // 即时保存设置
                SaveSettings();

                // 触发事件，通知其他 Panel 刷新卡片状态
                CardStatesRememberChanged?.Invoke(this, isChecked);
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