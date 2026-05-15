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
        private InventoryPanel _panelInventory = null!;
        private HotkeyPanel _panelHotkey = null!;
        private AboutPanel _panelAbout = null!;

        // 原生拖放目标（支持 Windows 拖拽缩略图）
        private IDisposable? _nativeDropTarget;

        public SettingsWindow(AppSettings settings)
        {
            // 先设置 _settings，因为 InitializeComponent 会触发 SelectionChanged 事件
            _settings = settings;

            InitializeComponent();

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
            if (_settings.System.RememberWindowPosition)
            {
                // 特殊值判断：位置为(0,0)时视为首次启动，居中显示
                // IsValidScreenPosition 可能会误判(0,0)为有效位置（多显示器环境下可能为负值）
                if (_settings.System.WindowPositionX == 0 && _settings.System.WindowPositionY == 0)
                {
                    // 首次启动：居中显示
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                }
                else if (IsValidScreenPosition(_settings.System.WindowPositionX, _settings.System.WindowPositionY))
                {
                    // 已保存位置：恢复位置
                    WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
                    Left = _settings.System.WindowPositionX;
                    Top = _settings.System.WindowPositionY;
                }
            }

            if (_settings.System.RememberWindowSize)
            {
                Width = _settings.System.WindowWidth;
                Height = _settings.System.WindowHeight;
            }

            // 监听窗口位置变化（即时保存）
            LocationChanged += OnLocationChanged;

            // 监听窗口大小变化（即时保存）
            SizeChanged += OnSizeChanged;

            // 创建各个面板并添加到容器
            _panelSystem = new SystemPanel(_settings);
            _panelSystem.CardStatesRememberChanged += OnCardStatesRememberChanged;
            _panelAppearance = new AppearancePanel(_settings);
            _panelAppearance.SetParentWindow(this);
            _panelHud = new HudPanel(_settings);
            _panelInventory = new InventoryPanel(_settings);
            _panelHotkey = new HotkeyPanel(_settings);
            _panelAbout = new AboutPanel();

            ContentContainer.Children.Add(_panelSystem);
            ContentContainer.Children.Add(_panelAppearance);
            ContentContainer.Children.Add(_panelHud);
            ContentContainer.Children.Add(_panelInventory);
            ContentContainer.Children.Add(_panelHotkey);
            ContentContainer.Children.Add(_panelAbout);

            // 根据设置恢复导航菜单选项
            string initialNav = _settings.System.RememberNavSelection
                ? _settings.System.LastSelectedNav
                : "system";

            // 设置初始选中的导航项（触发 SelectionChanged 会自动调用 ShowPanel）
            SelectNavItem(initialNav);
        }

        private void OnLocationChanged(object? sender, EventArgs e)
        {
            if (_settings.System.RememberWindowPosition)
            {
                _settings.System.WindowPositionX = Left;
                _settings.System.WindowPositionY = Top;
                SaveSettings();
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_settings.System.RememberWindowSize)
            {
                _settings.System.WindowWidth = Width;
                _settings.System.WindowHeight = Height;
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

        /// <summary>
        /// 检查窗口位置是否在有效的屏幕范围内
        /// Windows 有时会在窗口最小化/隐藏时保存无效坐标（如 -25600）
        /// 多显示器环境下坐标可以为负值，所以只检查是否超出虚拟屏幕范围
        /// </summary>
        private static bool IsValidScreenPosition(double x, double y)
        {
            // 检查是否在虚拟屏幕范围内
            // 多显示器环境下 VirtualScreenLeft/Top 可能为负值
            double virtualScreenWidth = System.Windows.SystemParameters.VirtualScreenWidth;
            double virtualScreenHeight = System.Windows.SystemParameters.VirtualScreenHeight;
            double virtualScreenLeft = System.Windows.SystemParameters.VirtualScreenLeft;
            double virtualScreenTop = System.Windows.SystemParameters.VirtualScreenTop;

            // 允许一定的容差，因为窗口可能部分超出屏幕边缘
            double tolerance = 100;
            return x >= virtualScreenLeft - tolerance &&
                   x <= virtualScreenLeft + virtualScreenWidth + tolerance &&
                   y >= virtualScreenTop - tolerance &&
                   y <= virtualScreenTop + virtualScreenHeight + tolerance;
        }

        private void NavListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // 初始化阶段 _settings 可能还未设置
            if (_settings == null) return;

            if (NavListBox.SelectedItem is System.Windows.Controls.ListBoxItem item && item.Tag is string tag)
            {
                ShowPanel(tag);

                // 即时保存导航菜单选项
                if (_settings.System.RememberNavSelection)
                {
                    _settings.System.LastSelectedNav = tag;
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// 根据标签选择导航项
        /// </summary>
        private void SelectNavItem(string tag)
        {
            foreach (var item in NavListBox.Items)
            {
                if (item is System.Windows.Controls.ListBoxItem listBoxItem && listBoxItem.Tag is string itemTag)
                {
                    if (itemTag == tag)
                    {
                        NavListBox.SelectedItem = listBoxItem;
                        break;
                    }
                }
            }
        }

        private void OnCardStatesRememberChanged(object? sender, bool rememberEnabled)
        {
            // 当开关状态变化时，刷新两个 Panel 的卡片状态
            _panelHud.RefreshCardStates(rememberEnabled);
            _panelInventory.RefreshCardStates(rememberEnabled);
        }

        private void ShowPanel(string tag)
        {
            if (_panelSystem == null) return;
            _panelSystem.Visibility = tag == "system" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            _panelAppearance.Visibility = tag == "appearance" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            _panelHud.Visibility = tag == "hud" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            _panelInventory.Visibility = tag == "inventory" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
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