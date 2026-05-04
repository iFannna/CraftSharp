using CraftSharp.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CraftSharp.Windows.Panels
{
    public partial class HotkeyPanel : System.Windows.Controls.UserControl
    {
        private AppSettings _settings;
        private bool _isRecordingHotkey = false;
        private System.Windows.Controls.Button? _recordingHotkeyButton = null;

        public HotkeyPanel(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            InitializeControls();
        }

        private void InitializeControls()
        {
            InventoryHotkeyBtn.Content = _settings.InventoryHotkey;
            SettingsHotkeyBtn.Content = _settings.SettingsHotkey;
            var notSetText = System.Windows.Application.Current.TryFindResource("HotkeyNotSet") as string ?? "未设置";
            HotbarToggleHotkeyBtn.Content = string.IsNullOrEmpty(_settings.HotbarToggleHotkey) ? notSetText : _settings.HotbarToggleHotkey;
        }

        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn)
            {
                _isRecordingHotkey = true;
                _recordingHotkeyButton = btn;
                btn.Content = "...";
            }
        }

        public void HandleKeyDown(System.Windows.Input.KeyEventArgs e)
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

        private static string BuildHotkeyString(System.Windows.Input.KeyEventArgs e)
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

        private void UpdateHotkeySetting(System.Windows.Controls.Button btn, string hotkey)
        {
            if (btn == InventoryHotkeyBtn) _settings.InventoryHotkey = hotkey;
            else if (btn == SettingsHotkeyBtn) _settings.SettingsHotkey = hotkey;
            else if (btn == HotbarToggleHotkeyBtn) _settings.HotbarToggleHotkey = hotkey;

            // 即时保存设置
            SaveSettings();
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