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
            var notSetText = (string)Application.Current.TryFindResource("HotkeyNotSet") ?? "";
            InventoryHotkeyBtn.Content = string.IsNullOrEmpty(_settings.Hotkeys.Inventory) ? notSetText : _settings.Hotkeys.Inventory;
            SettingsHotkeyBtn.Content = string.IsNullOrEmpty(_settings.Hotkeys.Settings) ? notSetText : _settings.Hotkeys.Settings;
        }

        private void HotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Wpf.Ui.Controls.Button btn) return;

            if (_isRecording)
            {
                FinishRecording();
            }

            _isRecording = true;
            _recordingButton = btn;

            btn.Content = "...";
            btn.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            btn.LostKeyboardFocus += OnRecordingButtonLostFocus;

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

            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                e.Key == Key.LWin || e.Key == Key.RWin ||
                e.Key == Key.System)
                return;

            var hotkey = BuildHotkeyString(e);
            if (string.IsNullOrEmpty(hotkey)) return;

            // 暂存引用，防止弹窗导致失焦后 _recordingButton 被清空
            var btn = _recordingButton;
            var hotkeyId = (string)btn.Tag;

            btn.LostKeyboardFocus -= OnRecordingButtonLostFocus;

            var conflictFunc = CheckConflict(hotkeyId, hotkey);
            if (conflictFunc != null)
            {
                var owner = Window.GetWindow(this);
                var dialog = new HotkeyConflictDialog(hotkey, conflictFunc);
                dialog.Owner = owner;
                dialog.ShowDialog();

                if (!dialog.IsConfirmed)
                {
                    btn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                    btn.LostKeyboardFocus -= OnRecordingButtonLostFocus;
                    RefreshDisplay();
                    _recordingButton = null;
                    _isRecording = false;
                    NotifyHotkeyServiceChanged();
                    e.Handled = true;
                    return;
                }
            }

            btn.Content = hotkey;
            btn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            _isRecording = false;
            _recordingButton = null;

            SetHotkeyString(hotkeyId, hotkey);
            SaveSettings();
            NotifyHotkeyServiceChanged();

            e.Handled = true;
        }

        private void ClearHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not string hotkeyId) return;

            var notSetText = (string)Application.Current.TryFindResource("HotkeyNotSet") ?? "";
            SetHotkeyString(hotkeyId, "");
            RefreshDisplay();
            SaveSettings();
            NotifyHotkeyServiceChanged();
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

        private string? CheckConflict(string currentHotkeyId, string newHotkey)
        {
            if (string.IsNullOrEmpty(newHotkey)) return null;
            if (currentHotkeyId != "Inventory" && _settings.Hotkeys.Inventory == newHotkey)
                return (string)Application.Current.TryFindResource("HotkeyInventoryLabel");
            if (currentHotkeyId != "Settings" && _settings.Hotkeys.Settings == newHotkey)
                return (string)Application.Current.TryFindResource("HotkeySettingsLabel");
            return null;
        }

        private void ShowConflictWarning(string hotkey)
        {
            var owner = Window.GetWindow(this);
            var dialog = new HotkeyConflictDialog(hotkey, "");
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
