using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CraftSharp.Models;
using CraftSharp.Services.Core;
using CraftSharp.Windows.Dialogs;

namespace CraftSharp.Windows.Settings.Panels.Hotkey
{
    public partial class HotkeyPanel : UserControl
    {
        private readonly AppSettings _settings;
        private bool _isRecording = false;
        private Wpf.Ui.Controls.Button? _recordingButton = null;

        public HotkeyPanel(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            RefreshDisplay();
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged() => RefreshDisplay();

        private void RefreshDisplay()
        {
            InventoryHotkeyBtn.Content = _settings.Hotkeys.Inventory;
            SettingsHotkeyBtn.Content = _settings.Hotkeys.Settings;
        }

        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Wpf.Ui.Controls.Button btn) return;

            if (_isRecording)
            {
                // 录制中点击任意按钮 -> 结束录制（不保存）
                FinishRecording();
            }

            _isRecording = true;
            _recordingButton = btn;

            btn.Content = "...";
            btn.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            btn.LostKeyboardFocus += OnRecordingButtonLostFocus;

            // 录制时临时注销全局快捷键，避免 Win32 拦截按键事件
            HotkeyService.Instance.UnregisterAll();
        }

        private void FinishRecording()
        {
            if (_recordingButton != null)
            {
                _recordingButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                _recordingButton.LostKeyboardFocus -= OnRecordingButtonLostFocus;
            }
            _isRecording = false;
            _recordingButton = null;

            // 重新注册全局快捷键
            NotifyHotkeyServiceChanged();
        }

        private void OnRecordingButtonLostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (_isRecording)
            {
                RefreshDisplay();
                FinishRecording();
            }
        }

        public void HandleKeyDown(KeyEventArgs e)
        {
            if (!_isRecording || _recordingButton == null) return;

            // 忽略纯修饰键
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                e.Key == Key.LWin || e.Key == Key.RWin ||
                e.Key == Key.System)
                return;

            var hotkey = BuildHotkeyString(e);
            if (string.IsNullOrEmpty(hotkey)) return;

            var hotkeyId = (string)_recordingButton.Tag;

            // 冲突检测
            if (CheckConflict(hotkeyId, hotkey))
            {
                ShowConflictWarning(hotkey);
            }

            // 更新按钮和设置
            _recordingButton.Content = hotkey;
            SetHotkeyString(hotkeyId, hotkey);
            SaveSettings();

            // 结束录制（不恢复按钮内容）
            _recordingButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            _recordingButton.LostKeyboardFocus -= OnRecordingButtonLostFocus;
            _isRecording = false;
            _recordingButton = null;

            // 重新注册全局快捷键
            NotifyHotkeyServiceChanged();

            e.Handled = true;
        }

        private static string BuildHotkeyString(KeyEventArgs e)
        {
            var parts = new List<string>();
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");

            parts.Add(e.Key == Key.System ? e.SystemKey.ToString() : e.Key.ToString());
            return string.Join("+", parts);
        }

        private bool CheckConflict(string currentHotkeyId, string newHotkey)
        {
            if (string.IsNullOrEmpty(newHotkey)) return false;
            if (currentHotkeyId != "Inventory" && _settings.Hotkeys.Inventory == newHotkey) return true;
            if (currentHotkeyId != "Settings" && _settings.Hotkeys.Settings == newHotkey) return true;
            return false;
        }

        private void ShowConflictWarning(string hotkey)
        {
            var owner = Window.GetWindow(this);
            var dialog = new HotkeyConflictDialog(hotkey);
            dialog.Owner = owner;
            dialog.ShowDialog();
        }

        private void SetHotkeyString(string hotkeyId, string value)
        {
            switch (hotkeyId)
            {
                case "Inventory": _settings.Hotkeys.Inventory = value; break;
                case "Settings": _settings.Hotkeys.Settings = value; break;
            }
        }

        private void NotifyHotkeyServiceChanged()
        {
            var map = new Dictionary<string, string>
            {
                { "Inventory", _settings.Hotkeys.Inventory },
                { "Settings", _settings.Hotkeys.Settings },
            };
            HotkeyService.Instance.ReRegisterAll(map);
        }

        private void RestoreDefaultsBtn_Click(object sender, RoutedEventArgs e)
        {
            var defaults = HotkeyService.GetDefaults();
            foreach (var (id, value) in defaults)
            {
                SetHotkeyString(id, value);
            }
            RefreshDisplay();
            SaveSettings();
            NotifyHotkeyServiceChanged();
        }

        private void SaveSettings()
        {
            if (Application.Current is App app)
            {
                app.SaveSettings();
            }
        }
    }
}
