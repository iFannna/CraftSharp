using System;
using System.Windows;
using CraftSharp.Helpers;
using CraftSharp.Models;
using CraftSharp.Windows.Settings.Panels;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Settings
{
    public partial class SettingsWindow : FluentWindow
    {
        private AppSettings _settings;
        private SystemPanel _panelSystem = null!;
        private AppearancePanel _panelAppearance = null!;
        private HudPanel _panelHud = null!;
        private HotkeyPanel _panelHotkey = null!;
        private AboutPanel _panelAbout = null!;

        // 原生拖放目标（支持 Windows 拖拽缩略图）
        private IDisposable? _nativeDropTarget;

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();

            _settings = settings;

            // 注册原生拖放（仅显示缩略图，不接受文件）
            SourceInitialized += (s, e) =>
            {
                try
                {
                    _nativeDropTarget = NativeDropHelper.RegisterForThumbnail(this);
                }
                catch (Exception)
                {
                    _nativeDropTarget?.Dispose();
                    _nativeDropTarget = null;
                }
            };

            // 窗口关闭时释放资源
            Closed += (s, e) =>
            {
                _nativeDropTarget?.Dispose();
                _nativeDropTarget = null;
            };

            // 根据设置恢复窗口位置和大小
            if (_settings.SettingsWindowRememberPosition)
            {
                // 只有当位置值有效（不为 0,0）时才恢复位置
                if (_settings.SettingsWindowPositionX != 0 || _settings.SettingsWindowPositionY != 0)
                {
                    WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
                    Left = _settings.SettingsWindowPositionX;
                    Top = _settings.SettingsWindowPositionY;
                }
            }

            if (_settings.SettingsWindowRememberSize)
            {
                Width = _settings.SettingsWindowWidth;
                Height = _settings.SettingsWindowHeight;
            }

            // 监听窗口位置变化（即时保存）
            LocationChanged += OnLocationChanged;

            // 监听窗口大小变化（即时保存）
            SizeChanged += OnSizeChanged;

            // 创建各个面板并添加到容器
            _panelSystem = new SystemPanel(_settings);
            _panelAppearance = new AppearancePanel(_settings);
            _panelAppearance.SetParentWindow(this);
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

        private void OnLocationChanged(object? sender, EventArgs e)
        {
            if (_settings.SettingsWindowRememberPosition)
            {
                _settings.SettingsWindowPositionX = Left;
                _settings.SettingsWindowPositionY = Top;
                SaveSettings();
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_settings.SettingsWindowRememberSize)
            {
                _settings.SettingsWindowWidth = Width;
                _settings.SettingsWindowHeight = Height;
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
    }
}