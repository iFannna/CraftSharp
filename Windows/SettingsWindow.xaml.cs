using System;
using CraftSharp.Models;
using CraftSharp.Windows.Panels;
using Newtonsoft.Json;
using Wpf.Ui.Controls;
using IOPath = System.IO.Path;

namespace CraftSharp.Windows
{
    public partial class SettingsWindow : FluentWindow
    {
        private AppSettings _settings = new();
        private string _settingsPath;
        private SystemPanel _panelSystem = null!;
        private AppearancePanel _panelAppearance = null!;
        private HudPanel _panelHud = null!;
        private HotkeyPanel _panelHotkey = null!;
        private AboutPanel _panelAbout = null!;

        public SettingsWindow()
        {
            InitializeComponent();

            _settingsPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            LoadSettings();

            // 创建各个面板并添加到容器
            _panelSystem = new SystemPanel(_settings);
            _panelAppearance = new AppearancePanel(_settings);
            _panelHud = new HudPanel(_settings);
            _panelHotkey = new HotkeyPanel(_settings);
            _panelAbout = new AboutPanel();

            ContentContainer.Children.Add(_panelSystem);
            ContentContainer.Children.Add(_panelAppearance);
            ContentContainer.Children.Add(_panelHud);
            ContentContainer.Children.Add(_panelHotkey);
            ContentContainer.Children.Add(_panelAbout);

            ShowPanel("system");
        }

        private void LoadSettings()
        {
            if (System.IO.File.Exists(_settingsPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(_settingsPath);
                    _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                catch { _settings = new AppSettings(); }
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

        private void NavListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (NavListBox.SelectedItem is System.Windows.Controls.ListBoxItem item && item.Tag is string tag)
            {
                ShowPanel(tag);
            }
        }

        private void ShowPanel(string tag)
        {
            if (_panelSystem == null) return;
            _panelSystem.Visibility = tag == "system" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            _panelAppearance.Visibility = tag == "appearance" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            _panelHud.Visibility = tag == "hud" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            _panelHotkey.Visibility = tag == "hotkey" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            _panelAbout.Visibility = tag == "about" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (_panelHotkey != null)
                _panelHotkey.HandleKeyDown(e);

            if (!e.Handled)
                base.OnKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            base.OnClosed(e);
        }
    }
}