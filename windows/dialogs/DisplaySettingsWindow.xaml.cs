using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CraftSharp.Helpers;
using CraftSharp.Models;
using CraftSharp.Services.Wallpaper;

namespace CraftSharp.Windows.Dialogs
{
    /// <summary>
    /// 显示器设置弹窗：Win11 系统-屏幕 页一期只读复刻（位置图/呈现模式/主屏/缩放布局），
    /// 底部为壁纸应用动作区。多屏时经 OpenForWallpaper 进入，壁纸应用在本弹窗内完成
    /// </summary>
    public partial class DisplaySettingsWindow
    {
        private readonly WallpaperItem _wallpaper;
        private List<DisplayInfo> _displays = new();
        private readonly List<System.Windows.Controls.Button> _monitorButtons = new();
        private readonly HashSet<string> _targetPaths = new();
        private readonly List<Window> _identifyWindows = new();
        private int _selectedIndex;
        private bool _spanMode;
        private bool _applying;

        private static readonly Brush UnselectedBlockBrush;

        static DisplaySettingsWindow()
        {
            var brush = new SolidColorBrush(Color.FromRgb(0x6B, 0x6B, 0x6B));
            brush.Freeze();
            UnselectedBlockBrush = brush;
        }

        private static WallpaperSettings? WallpaperConfig =>
            (Application.Current as App)?.GetAppSettings()?.Wallpaper;

        private static string GetString(string key) =>
            Application.Current.FindResource(key) as string ?? key;

        public DisplaySettingsWindow(WallpaperItem wallpaper, List<MonitorInfo> monitors)
        {
            InitializeComponent();

            _wallpaper = wallpaper;
            _displays = DisplayInfoService.GetDisplays(monitors);
            if (_displays.Count == 0)
                _displays = monitors
                    .Select(m => new DisplayInfo(m, "", "", 96, 0, 0))
                    .ToList();

            _spanMode = WallpaperConfig?.Mode == "span";
            _selectedIndex = Math.Max(0, _displays.FindIndex(d => d.Monitor.IsPrimary));
            WallpaperTitleText.Text = _wallpaper.Title;

            Closed += (_, _) => CloseIdentifyWindows();
        }

        /// <summary>
        /// 多屏时打开显示器设置弹窗（壁纸应用由弹窗内完成）；单屏返回 false，由调用方直接应用
        /// </summary>
        public static bool OpenForWallpaper(WallpaperItem wallpaper, Window? owner)
        {
            var monitors = MonitorLayoutService.Instance.GetMonitors();
            if (monitors.Count < 2) return false;

            var dialog = new DisplaySettingsWindow(wallpaper, monitors) { Owner = owner };
            dialog.ShowDialogQuiet();
            return true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RebuildAll();
            _ = LoadThumbnailAsync();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        #region 整体构建

        private void RebuildAll()
        {
            SyncTargets();
            RebuildDiagram();
            UpdateModeCombo();
            UpdateModeButtons();
            RebuildChips();
            SelectMonitor(_selectedIndex);
        }

        private void SyncTargets()
        {
            var paths = _displays.Select(d => d.Monitor.DevicePath).ToHashSet();
            _targetPaths.RemoveWhere(p => !paths.Contains(p));
            if (!_spanMode && _targetPaths.Count == 0)
                foreach (var p in paths)
                    _targetPaths.Add(p);
        }

        #endregion

        #region 显示器位置图

        private void RebuildDiagram()
        {
            MonitorCanvas.Children.Clear();
            _monitorButtons.Clear();
            if (_displays.Count == 0) return;

            var minX = _displays.Min(d => d.Monitor.Bounds.Left);
            var minY = _displays.Min(d => d.Monitor.Bounds.Top);
            var unionW = (double)(_displays.Max(d => d.Monitor.Bounds.Right) - minX);
            var unionH = (double)(_displays.Max(d => d.Monitor.Bounds.Bottom) - minY);
            if (unionW <= 0 || unionH <= 0) return;

            var availW = Math.Max(200.0, DiagramCard.ActualWidth - 24);
            // 缩放高度预算固定，画布增高只增加留白，不放大显示器块
            var scale = Math.Min(160.0 / unionH, availW / unionW);
            var boxH = unionH * scale;
            var offsetTop = (MonitorCanvas.Height - boxH) / 2;
            MonitorCanvas.Width = unionW * scale;

            for (var i = 0; i < _displays.Count; i++)
            {
                var info = _displays[i];
                var block = new System.Windows.Controls.Button
                {
                    Style = (Style)FindResource("MonitorBlockStyle"),
                    Width = info.Monitor.Width * scale,
                    Height = info.Monitor.Height * scale,
                    Tag = i,
                    Content = new TextBlock
                    {
                        Text = info.Monitor.Index.ToString(),
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        FontSize = Math.Max(12, Math.Min(28, info.Monitor.Height * scale * 0.35))
                    }
                };
                AutomationProperties.SetName(block,
                    string.Format(GetString("DisplayMonitorNameFallback"), info.Monitor.Index));
                Canvas.SetLeft(block, (info.Monitor.Bounds.Left - minX) * scale);
                Canvas.SetTop(block, offsetTop + (info.Monitor.Bounds.Top - minY) * scale);
                block.Click += MonitorBlock_Click;
                MonitorCanvas.Children.Add(block);
                _monitorButtons.Add(block);
            }

            UpdateDiagramSelection();
        }

        private void UpdateDiagramSelection()
        {
            var accent = FindResource("AccentBrush") as Brush ?? Brushes.DodgerBlue;
            for (var i = 0; i < _monitorButtons.Count; i++)
                _monitorButtons[i].Background = i == _selectedIndex ? accent : UnselectedBlockBrush;
        }

        private void MonitorBlock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: int index })
                SelectMonitor(index);
        }

        private void DiagramCard_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RebuildDiagram();
        }

        private void SelectMonitor(int index)
        {
            if (_displays.Count == 0) return;

            _selectedIndex = Math.Clamp(index, 0, _displays.Count - 1);
            UpdateDiagramSelection();
            UpdateScaleSection();
        }

        #endregion

        #region 只读信息区

        private void UpdateModeCombo()
        {
            var label = DisplayInfoService.GetTopologyKind(_displays) switch
            {
                "clone" => GetString("DisplayModeDuplicate"),
                "single" => GetString("DisplayModeSingle"),
                _ => GetString("DisplayModeExtend")
            };
            ModeCombo.Items.Clear();
            ModeCombo.Items.Add(new ComboBoxItem { Content = label, IsSelected = true });
        }

        private void UpdateScaleSection()
        {
            if (_displays.Count == 0) return;
            var d = _displays[_selectedIndex];

            ScaleCombo.Items.Clear();
            ScaleCombo.Items.Add(new ComboBoxItem
            {
                Content = $"{d.Dpi / 96.0 * 100:0}%",
                IsSelected = true
            });

            ResolutionCombo.Items.Clear();
            ResolutionCombo.Items.Add(new ComboBoxItem
            {
                Content = $"{d.Monitor.Width} × {d.Monitor.Height}",
                IsSelected = true
            });

            OrientationCombo.Items.Clear();
            OrientationCombo.Items.Add(new ComboBoxItem
            {
                Content = d.Orientation switch
                {
                    1 => GetString("DisplayOrientationPortrait"),
                    2 => GetString("DisplayOrientationLandscapeFlipped"),
                    3 => GetString("DisplayOrientationPortraitFlipped"),
                    _ => GetString("DisplayOrientationLandscape")
                },
                IsSelected = true
            });
        }

        #endregion

        #region 标识编号

        private void Identify_Click(object sender, RoutedEventArgs e)
        {
            CloseIdentifyWindows();

            foreach (var display in _displays)
            {
                var info = display;

                var number = new TextBlock
                {
                    Text = info.Monitor.Index.ToString()
                };
                number.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");

                var badge = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    BorderThickness = new Thickness(1),
                    Child = new Viewbox
                    {
                        Stretch = Stretch.Uniform,
                        Margin = new Thickness(20),
                        Child = number
                    }
                };
                badge.SetResourceReference(Border.BackgroundProperty, "CardBackgroundBrush");
                badge.SetResourceReference(Border.BorderBrushProperty, "DividerBrush");

                // GetAppSpaceBounds 对非系统缩放屏返回"物理原点 + 尺寸×(sysScale/monScale)"
                // 的混合空间（实测 2560 宽的 150% 副屏被报成 2133）：原点可直接当物理值，
                // 距原点的差值须×k 还原成物理像素
                var (monApp, workApp) = DisplayInfoService.GetAppSpaceBounds(info);
                var monScale = Math.Max(1.0, info.Dpi / 96.0);
                var sysScale = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                var k = monScale / sysScale;
                var monLeft = (double)monApp.Left;
                var monTop = (double)monApp.Top;
                var workBottomPhys = monTop + (workApp.Bottom - monTop) * k;
                var margin = 48 * monScale;
                var badgeDip = 300.0;
                var badgeSize = badgeDip * monScale;
                var tx = monLeft + margin;
                // 底边零间距贴住任务栏上沿（工作区底边），间距只留左侧
                var ty = workBottomPhys - badgeSize;

                var window = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    ResizeMode = ResizeMode.NoResize,
                    Background = Brushes.Transparent,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    Topmost = true,
                    Focusable = false,
                    Width = badgeDip,
                    Height = badgeDip,
                    // WPF-UI 隐式 Window 样式带 MinWidth=460/MinHeight=320，经属性强制转换
                    // 会把 Width/Height 钳到 460x320，必须本地置零夺回
                    MinWidth = 0,
                    MinHeight = 0,
                    Content = badge
                };
                window.MouseDown += (_, _) => window.Close();

                // 出生即精确：系统级 DPI 感知进程里，摆在非系统缩放屏上的窗口被 DWM 以显示器
                // 原点锚定做视觉缩放（×monDpi/sysDpi），按该模型从物理目标反解 WPF 记账坐标，
                // 两轴都必须以显示器原点为锚点
                window.Left = (monLeft + (tx - monLeft) * sysScale / monScale) / sysScale;
                window.Top = (monTop + (ty - monTop) * sysScale / monScale) / sysScale;
                window.Show();

                _identifyWindows.Add(window);
            }

            _ = AutoCloseIdentifyAsync();
        }

        private async Task AutoCloseIdentifyAsync()
        {
            await Task.Delay(3500);
            await Dispatcher.InvokeAsync(CloseIdentifyWindows);
        }

        private void CloseIdentifyWindows()
        {
            foreach (var window in _identifyWindows.ToList())
            {
                try { window.Close(); }
                catch { /* 忽略已关闭窗口 */ }
            }
            _identifyWindows.Clear();
        }

        #endregion

        #region 壁纸应用动作区

        private void UpdateModeButtons()
        {
            ModeIndependentButton.Appearance = _spanMode
                ? Wpf.Ui.Controls.ControlAppearance.Secondary
                : Wpf.Ui.Controls.ControlAppearance.Primary;
            ModeSpanButton.Appearance = _spanMode
                ? Wpf.Ui.Controls.ControlAppearance.Primary
                : Wpf.Ui.Controls.ControlAppearance.Secondary;
        }

        private void ModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Wpf.Ui.Controls.Button btn || btn.Tag is not string tag) return;
            var span = tag == "span";
            if (span == _spanMode) return;

            _spanMode = span;
            SyncTargets();
            UpdateModeButtons();
            RebuildChips();
        }

        private void RebuildChips()
        {
            MonitorChipPanel.Children.Clear();
            var primaryMark = GetString("WallpaperMonitorPrimary");

            foreach (var d in _displays)
            {
                var path = d.Monitor.DevicePath;
                var label = $"{d.Monitor.Index} · {d.Monitor.Width}x{d.Monitor.Height}";
                if (d.Monitor.IsPrimary)
                    label += $" ({primaryMark})";

                var chip = new Wpf.Ui.Controls.Button
                {
                    Content = label,
                    Tag = path,
                    Appearance = _targetPaths.Contains(path)
                        ? Wpf.Ui.Controls.ControlAppearance.Primary
                        : Wpf.Ui.Controls.ControlAppearance.Secondary,
                    Margin = new Thickness(0, 0, 4, 4),
                    Padding = new Thickness(16, 6, 16, 6),
                    FontSize = (double)FindResource("GlobalFontSizeSmall"),
                    IsEnabled = !_spanMode
                };
                chip.Click += TargetChip_Click;
                MonitorChipPanel.Children.Add(chip);
            }
        }

        private void TargetChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Wpf.Ui.Controls.Button btn || btn.Tag is not string path) return;

            if (_targetPaths.Contains(path))
                _targetPaths.Remove(path);
            else
                _targetPaths.Add(path);

            RebuildChips();
        }

        private async void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (_applying) return;

            if (!_spanMode && _targetPaths.Count == 0)
            {
                MessageBox.Show(GetString("DisplayNoTarget"), "Craft#", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _applying = true;
            ApplyButton.IsEnabled = false;
            ApplyButton.Content = GetString("WallpaperSetting");

            try
            {
                var settings = WallpaperConfig;
                if (settings != null)
                {
                    settings.Mode = _spanMode ? "span" : "independent";
                    (Application.Current as App)?.SaveSettings();
                }

                if (_spanMode)
                {
                    await WallpaperService.Instance.ApplySpanAsync(_wallpaper);
                }
                else
                {
                    foreach (var path in _targetPaths.ToList())
                        await WallpaperService.Instance.ApplyToMonitorAsync(_wallpaper, path);
                }

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Craft#", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _applying = false;
                ApplyButton.IsEnabled = true;
                ApplyButton.Content = GetString("WallpaperQuickSet");
            }
        }

        private async Task LoadThumbnailAsync()
        {
            var cached = WallpaperImageCache.Instance.GetFromCache(_wallpaper.ThumbnailUrl);
            if (cached != null)
            {
                ThumbImage.Source = cached;
                return;
            }

            var image = await WallpaperImageCache.Instance.GetAsync(_wallpaper.ThumbnailUrl);
            if (image != null)
                ThumbImage.Source = image;
        }

        #endregion
    }
}
