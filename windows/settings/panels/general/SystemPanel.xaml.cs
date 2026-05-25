using CraftSharp.Models;
using CraftSharp.Services.Core;
using CraftSharp.Services.Hud;
using CraftSharp.Services.Slot;
using CraftSharp.Services.Resource;
using CraftSharp.Windows.Dialogs;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CraftSharp.Windows.Settings.Panels.General
{
    public partial class SystemPanel : global::System.Windows.Controls.UserControl
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
            LanguageComboBox.SelectedIndex = _settings.System.Language == "zh-CN" ? 0 : 1;

            // 以注册表 Run 键的实际状态为准
            var actualAutoStart = IsAutoStartInRegistry();
            _settings.System.AutoStart = actualAutoStart;
            AutoStartToggle.IsChecked = actualAutoStart;

            DefaultOpenPanelToggle.IsChecked = _settings.System.DefaultOpenPanel;
            RememberPositionToggle.IsChecked = _settings.System.RememberWindowPosition;
            RememberSizeToggle.IsChecked = _settings.System.RememberWindowSize;
            RememberCardStatesToggle.IsChecked = _settings.System.RememberCardStates; // 默认开启
            RememberNavSelectionToggle.IsChecked = _settings.System.RememberNavSelection; // 默认开启
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null) return;
            if (LanguageComboBox.SelectedItem is ComboBoxItem item)
            {
                // 从显示名称获取语言代码
                var displayName = item.Content.ToString() ?? "简体中文";
                var languageCode = displayName == "English" ? "en-US" : "zh-CN";
                _settings.System.Language = languageCode;

                // 切换语言
                LocalizationService.Instance.SetLanguage(displayName);

                // 即时保存设置
                SaveSettings();
            }
        }

        private void AutoStartToggle_Click(object sender, global::System.Windows.RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch toggle)
            {
                _settings.System.AutoStart = toggle.IsChecked ?? false;
                SetAutoStartRegistry(_settings.System.AutoStart);
                // 即时保存设置
                SaveSettings();
            }
        }

        private void DefaultOpenPanelToggle_Click(object sender, global::System.Windows.RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch toggle)
            {
                _settings.System.DefaultOpenPanel = toggle.IsChecked ?? false;
                // 即时保存设置
                SaveSettings();
            }
        }

        private void RememberPositionToggle_Click(object sender, global::System.Windows.RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch toggle)
            {
                bool isChecked = toggle.IsChecked ?? false;
                _settings.System.RememberWindowPosition = isChecked;

                // 如果关闭开关，重置位置为默认值（下次启动居中）
                if (!isChecked)
                {
                    _settings.System.WindowPositionX = 0;
                    _settings.System.WindowPositionY = 0;
                    _settings.System.WindowState = "normal";
                }

                // 即时保存设置
                SaveSettings();
            }
        }

        private void RememberSizeToggle_Click(object sender, global::System.Windows.RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch toggle)
            {
                bool isChecked = toggle.IsChecked ?? false;
                _settings.System.RememberWindowSize = isChecked;

                // 如果关闭开关，重置大小为默认值
                if (!isChecked)
                {
                    _settings.System.WindowWidth = 1080;
                    _settings.System.WindowHeight = 720;
                    _settings.System.WindowState = "normal";
                }

                // 即时保存设置
                SaveSettings();
            }
        }

        private void RememberCardStatesToggle_Click(object sender, global::System.Windows.RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch toggle)
            {
                bool isChecked = toggle.IsChecked ?? false;
                _settings.System.RememberCardStates = isChecked;

                // 如果关闭开关，清空已保存的状态
                if (!isChecked)
                {
                    _settings.System.CardExpandedStates.Clear();
                }

                // 即时保存设置
                SaveSettings();

                // 触发事件，通知其他 Panel 刷新卡片状态
                CardStatesRememberChanged?.Invoke(this, isChecked);
            }
        }

        private void RememberNavSelectionToggle_Click(object sender, global::System.Windows.RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ToggleSwitch toggle)
            {
                bool isChecked = toggle.IsChecked ?? false;
                _settings.System.RememberNavSelection = isChecked;

                // 如果关闭开关，清空已保存的导航选项
                if (!isChecked)
                {
                    _settings.System.LastSelectedNav = "system";
                }

                // 即时保存设置
                SaveSettings();
            }
        }

        private static bool IsAutoStartInRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue("CraftSharp") != null;
            }
            catch { return false; }
        }

        private void SetAutoStartRegistry(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;
                if (enable)
                {
                    var exePath = Environment.ProcessPath ?? "";
                    key.SetValue("CraftSharp", $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue("CraftSharp", false);
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            if (global::System.Windows.Application.Current is App app)
            {
                app.SaveSettings();
            }
        }

        private void RestoreDefaultsBtn_Click(object sender, global::System.Windows.RoutedEventArgs e)
        {
            var title = (string)Application.Current.TryFindResource("RestoreDefaultsConfirmTitle") ?? "";
            var message = (string)Application.Current.TryFindResource("RestoreDefaultsConfirmMessage") ?? "";
            var dialog = new ConfirmDialog(title, message) { Owner = Window.GetWindow(this) };
            dialog.ShowDialog();
            if (!dialog.IsConfirmed) return;

            _settings.System = new SystemSettings();
            InitializeControls();
            SaveSettings();
        }
    }
}