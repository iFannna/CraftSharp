using CraftSharp.Models;
using CraftSharp.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CraftSharp.Windows.Settings.Panels.Hotkey
{
    public partial class HotkeyPanel : global::System.Windows.Controls.UserControl
    {
        private AppSettings _settings;
        private bool _isRecordingHotkey = false;
        private global::System.Windows.Controls.Button? _recordingHotkeyButton = null;

        public HotkeyPanel(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            InitializeControls();

            // 订阅语言切换事件
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            InitializeControls();
        }

        private void InitializeControls()
        {
            InventoryHotkeyBtn.Content = _settings.Hotkeys.Inventory;
            SettingsHotkeyBtn.Content = _settings.Hotkeys.Settings;
            var notSetText = GetResourceString("HotkeyNotSet");
            HotbarToggleHotkeyBtn.Content = string.IsNullOrEmpty(_settings.Hotkeys.HotbarToggle) ? notSetText : _settings.Hotkeys.HotbarToggle;
        }

        private static string GetResourceString(string key)
        {
            return global::System.Windows.Application.Current.TryFindResource(key) as string ?? key;
        }

        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is global::System.Windows.Controls.Button btn)
            {
                _isRecordingHotkey = true;
                _recordingHotkeyButton = btn;
                btn.Content = "...";
            }
        }

        public void HandleKeyDown(global::System.Windows.Input.KeyEventArgs e)
        {
            if (_isRecordingHotkey && _recordingHotkeyButton != null)
            {
                var hotkey = BuildHotkeyString(e);
                _recordingHotkeyButton.Content = hotkey;
                UpdateHotkeySetting(_recordingHotkeyButton, hotkey);
                _isRecordingHotkey = false;
                _recordingHotkeyButton = null;
                e.Handled = true;
            }
        }

        private static string BuildHotkeyString(global::System.Windows.Input.KeyEventArgs e)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            var key = e.Key;
            if (key != Key.LeftCtrl && key != Key.RightCtrl && key != Key.LeftShift && key != Key.RightShift && key != Key.LeftAlt && key != Key.RightAlt)
                parts.Add(key.ToString());
            return string.Join("+", parts);
        }

        private void UpdateHotkeySetting(global::System.Windows.Controls.Button btn, string hotkey)
        {
            if (btn == InventoryHotkeyBtn) _settings.Hotkeys.Inventory = hotkey;
            else if (btn == SettingsHotkeyBtn) _settings.Hotkeys.Settings = hotkey;
            else if (btn == HotbarToggleHotkeyBtn) _settings.Hotkeys.HotbarToggle = hotkey;

            // 即时保存设置
            SaveSettings();
        }

        private void SaveSettings()
        {
            if (global::System.Windows.Application.Current is App app)
            {
                app.SaveSettings();
            }
        }
    }
}