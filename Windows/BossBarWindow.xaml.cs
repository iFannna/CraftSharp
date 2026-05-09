using CraftSharp.Models;
using CraftSharp.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CraftSharp.Windows
{
    /// <summary>
    /// BOSS血条窗口
    ///
    /// 布局规则：
    /// 1. 窗口定位在桌面顶部水平居中
    /// 2. 多个BOSS血条依次向下排列
    /// 3. 每个BOSS血条包含：名称 + 血条图层堆叠
    ///
    /// 图层结构（从下到上）：
    /// - BOSS血条背景 ({color}_background.png)
    /// - Notch背景 (notched_{n}_background.png，如果启用)
    /// - BOSS血条进度 ({color}_progress.png，裁剪宽度)
    /// - Notch进度 (notched_{n}_progress.png，裁剪宽度，如果启用)
    /// </summary>
    public partial class BossBarWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly Dictionary<string, BossBarPanel> _bossBarPanels = new();
        private readonly DispatcherTimer _updateTimer;
        private double _scaleFactor;

        // 性能计数器（缓存以避免每次调用NextValue的延迟）
        private System.Diagnostics.PerformanceCounter? _cpuCounter;
        private System.Diagnostics.PerformanceCounter? _availableMemoryCounter;

        // 默认尺寸（当无法从图片读取时使用，与实际图片尺寸一致）
        private const double DefaultBossBarWidth = 182;
        private const double DefaultBossBarHeight = 5;

        // 缩放基准值（与StatusBarWindow一致）
        private const double BaseScreenWidth = 2560;
        private const double BaseScaleMultiplier = 6;

        public BossBarWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;

            // 设置窗口图标
            SetWindowIcon();

            // 根据屏幕分辨率计算缩放比例
            CalculateScale();

            // 初始化更新定时器
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += UpdateTimer_Tick;

            // 监听BossBars集合变化
            _settings.BossBars.CollectionChanged += BossBars_CollectionChanged;

            // 初始化性能计数器
            InitializePerformanceCounters();

            // 初始化窗口位置
            UpdateWindowPosition();

            // 加载所有BOSS血条
            LoadAllBossBars();

            // 启动更新定时器
            _updateTimer.Start();
        }

        /// <summary>
        /// 根据屏幕分辨率计算缩放比例
        /// </summary>
        private void CalculateScale()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            _scaleFactor = (screenWidth / BaseScreenWidth) * BaseScaleMultiplier;
        }

        private void SetWindowIcon()
        {
            var icon = IconService.Instance.GetWindowIcon();
            if (icon != null)
            {
                this.Icon = icon;
            }
        }

        /// <summary>
        /// 初始化性能计数器
        /// </summary>
        private void InitializePerformanceCounters()
        {
            try
            {
                _cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
                _availableMemoryCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");

                // 第一次调用NextValue返回0，需要预热
                _cpuCounter.NextValue();
                _availableMemoryCounter.NextValue();
            }
            catch
            {
                // 如果初始化失败，计数器将为null
            }
        }

        /// <summary>
        /// 更新窗口位置到桌面顶部水平居中
        /// </summary>
        private void UpdateWindowPosition()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            // 使用第一个启用的BOSS血条的实际宽度计算窗口宽度
            double barWidth = DefaultBossBarWidth * _scaleFactor;
            if (_bossBarPanels.Count > 0)
            {
                // 取第一个panel的实际宽度
                barWidth = _bossBarPanels.Values.First().GetActualWidth();
            }
            var windowHeight = this.ActualHeight > 0 ? this.ActualHeight : EstimateWindowHeight();

            // 窗口宽度设置为血条宽度，水平居中
            this.Left = (screenWidth - barWidth) / 2;
            this.Top = 0;
            this.Width = barWidth;
            this.Height = windowHeight;
        }

        /// <summary>
        /// 估算窗口高度
        /// </summary>
        private double EstimateWindowHeight()
        {
            int enabledCount = _settings.BossBars.Count(b => b.IsEnabled);
            if (enabledCount == 0) return 50;

            // 每个BOSS血条高度 = 名称高度(约20*scale) + 血条高度 + 间距(4*scale)
            // 血条高度取第一个panel的实际高度，或使用默认值
            double barHeight = DefaultBossBarHeight * _scaleFactor;
            double nameHeight = 20 * _scaleFactor;
            double spacing = 4 * _scaleFactor;
            if (_bossBarPanels.Count > 0)
            {
                barHeight = _bossBarPanels.Values.First().GetActualHeight();
            }
            return enabledCount * (nameHeight + barHeight + spacing) + spacing;
        }

        /// <summary>
        /// 加载所有BOSS血条
        /// </summary>
        private void LoadAllBossBars()
        {
            BossBarsContainer.Children.Clear();
            _bossBarPanels.Clear();

            foreach (var bossBar in _settings.BossBars)
            {
                // 监听每个BossBarSettings的PropertyChanged事件
                bossBar.PropertyChanged -= BossBarSettings_PropertyChanged;
                bossBar.PropertyChanged += BossBarSettings_PropertyChanged;

                if (bossBar.IsEnabled)
                {
                    AddBossBarPanel(bossBar);
                }
            }

            // 更新窗口高度
            UpdateWindowHeight();
            UpdateWindowPosition();

            // 根据是否有启用的BOSS血条决定窗口可见性
            if (_bossBarPanels.Count > 0)
                this.Show();
            else
                this.Hide();
        }

        /// <summary>
        /// 添加单个BOSS血条面板
        /// </summary>
        private void AddBossBarPanel(BossBarSettings settings)
        {
            if (_bossBarPanels.ContainsKey(settings.Id)) return; // 防止重复添加

            var panel = new BossBarPanel(settings, _scaleFactor, this);
            _bossBarPanels[settings.Id] = panel;
            BossBarsContainer.Children.Add(panel);
        }

        /// <summary>
        /// 移除单个BOSS血条面板
        /// </summary>
        private void RemoveBossBarPanel(BossBarSettings settings)
        {
            if (_bossBarPanels.TryGetValue(settings.Id, out var panel))
            {
                BossBarsContainer.Children.Remove(panel);
                _bossBarPanels.Remove(settings.Id);
            }
        }

        /// <summary>
        /// BossBarSettings属性变化处理
        /// </summary>
        private void BossBarSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is BossBarSettings settings)
            {
                // IsEnabled变化：添加/移除面板
                if (e.PropertyName == nameof(BossBarSettings.IsEnabled))
                {
                    if (settings.IsEnabled)
                    {
                        // 启用：添加面板，如果窗口隐藏则显示
                        AddBossBarPanel(settings);
                        if (this.Visibility != Visibility.Visible)
                            this.Show();
                    }
                    else
                    {
                        // 禁用：移除面板，如果没有启用项则隐藏
                        RemoveBossBarPanel(settings);
                        if (_bossBarPanels.Count == 0)
                            this.Hide();
                    }
                    UpdateWindowHeight();
                    UpdateWindowPosition();
                }
                // 其他属性变化：更新面板显示
                else if (_bossBarPanels.TryGetValue(settings.Id, out var panel))
                {
                    panel.UpdateFromSettings();
                }
            }
        }

        /// <summary>
        /// BossBars集合变化处理
        /// </summary>
        private void BossBars_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (BossBarSettings newItem in e.NewItems)
                {
                    // 监听PropertyChanged事件
                    newItem.PropertyChanged -= BossBarSettings_PropertyChanged;
                    newItem.PropertyChanged += BossBarSettings_PropertyChanged;

                    if (newItem.IsEnabled)
                    {
                        AddBossBarPanel(newItem);
                    }
                }
                // 如果添加了启用的BOSS血条且窗口隐藏，显示窗口
                if (e.NewItems.Cast<BossBarSettings>().Any(b => b.IsEnabled) && this.Visibility != Visibility.Visible)
                {
                    this.Show();
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (BossBarSettings oldItem in e.OldItems)
                {
                    // 取消监听PropertyChanged事件
                    oldItem.PropertyChanged -= BossBarSettings_PropertyChanged;

                    RemoveBossBarPanel(oldItem);
                }
                // 如果所有BOSS血条都被移除，隐藏窗口
                if (_bossBarPanels.Count == 0)
                {
                    this.Hide();
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace && e.NewItems != null)
            {
                foreach (BossBarSettings newItem in e.NewItems)
                {
                    if (_bossBarPanels.TryGetValue(newItem.Id, out var panel))
                    {
                        panel.UpdateSettings(newItem);
                    }
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                LoadAllBossBars();
            }

            UpdateWindowHeight();
            UpdateWindowPosition();
        }

        /// <summary>
        /// 更新窗口高度
        /// </summary>
        private void UpdateWindowHeight()
        {
            this.Height = EstimateWindowHeight();
        }

        /// <summary>
        /// 定时更新所有BOSS血条进度
        /// </summary>
        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            foreach (var panel in _bossBarPanels.Values)
            {
                panel.UpdateProgress();
            }
        }

        /// <summary>
        /// 设置缩放比例
        /// </summary>
        public void SetScaleFactor(double scaleFactor)
        {
            _scaleFactor = scaleFactor;
            foreach (var panel in _bossBarPanels.Values)
            {
                panel.SetScaleFactor(scaleFactor);
            }
            UpdateWindowHeight();
            UpdateWindowPosition();
        }

        /// <summary>
        /// 显示/隐藏窗口
        /// </summary>
        public void SetVisibility(bool visible)
        {
            this.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 获取数据映射值（百分比 0.0 - 1.0）
        /// </summary>
        public double GetDataMappingValue(string mappingType)
        {
            switch (mappingType)
            {
                case "电池电量":
                    var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
                    return powerStatus.BatteryLifePercent;

                case "内存占用率":
                    try
                    {
                        if (_availableMemoryCounter != null)
                        {
                            double availableMB = _availableMemoryCounter.NextValue();
                            double totalMB = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024.0 * 1024.0);
                            double usedPercent = (totalMB - availableMB) / totalMB;
                            return Math.Min(1.0, Math.Max(0.0, usedPercent));
                        }
                        return 0;
                    }
                    catch
                    {
                        return 0;
                    }

                case "CPU利用率":
                    try
                    {
                        if (_cpuCounter != null)
                        {
                            return Math.Min(1.0, _cpuCounter.NextValue() / 100.0);
                        }
                        return 0;
                    }
                    catch
                    {
                        return 0;
                    }

                case "GPU利用率":
                    // GPU利用率需要特殊API，暂时返回电池电量
                    var ps = System.Windows.Forms.SystemInformation.PowerStatus;
                    return ps.BatteryLifePercent;

                default:
                    return 0;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _updateTimer.Stop();
            _settings.BossBars.CollectionChanged -= BossBars_CollectionChanged;

            // 释放性能计数器
            _cpuCounter?.Dispose();
            _availableMemoryCounter?.Dispose();

            base.OnClosed(e);
        }
    }

    /// <summary>
    /// BOSS血条面板控件（单个BOSS血条）
    /// </summary>
    internal class BossBarPanel : StackPanel
    {
        private BossBarSettings _settings;
        private double _scaleFactor;
        private readonly BossBarWindow _parentWindow;

        // 从图片文件读取的原始尺寸
        private double _originalWidth;
        private double _originalHeight;

        // Notch原始尺寸
        private double _originalNotchWidth;
        private double _originalNotchHeight;

        // 名称文本容器（横向排列多个字符TextBlock）
        private readonly StackPanel _nameContainer;

        // 血条图层Grid
        private readonly Grid _barGrid;

        // 四个图层Image
        private readonly System.Windows.Controls.Image _barBackground;
        private readonly System.Windows.Controls.Image _notchBackground;
        private readonly System.Windows.Controls.Image _barProgress;
        private readonly System.Windows.Controls.Image _notchProgress;

        // 默认尺寸（当无法从图片读取时使用，与实际图片尺寸一致）
        private const double DefaultWidth = 182;
        private const double DefaultHeight = 5;

        public BossBarPanel(BossBarSettings settings, double scaleFactor, BossBarWindow parentWindow)
        {
            _settings = settings;
            _scaleFactor = scaleFactor;
            _parentWindow = parentWindow;

            // 垂直排列：名称 + 血条
            this.Orientation = System.Windows.Controls.Orientation.Vertical;
            this.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            this.Margin = new Thickness(0, 0, 0, 4);

            // 从图片文件读取原始尺寸
            LoadDimensions();

            // BOSS名称容器（横向排列，每个字符独立TextBlock以控制间距）
            _nameContainer = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 3 * scaleFactor, 0, 1 * scaleFactor)
            };
            CreateNameCharacters(settings.Name, scaleFactor);
            this.Children.Add(_nameContainer);

            // 血条图层Grid
            double width = _originalWidth * scaleFactor;
            double height = _originalHeight * scaleFactor;
            _barGrid = new Grid
            {
                Width = width,
                Height = height,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            // 图层1：BOSS血条背景
            _barBackground = new System.Windows.Controls.Image();
            _barBackground.Stretch = Stretch.Fill;
            RenderOptions.SetBitmapScalingMode(_barBackground, BitmapScalingMode.NearestNeighbor);
            _barBackground.UseLayoutRounding = true;
            _barBackground.SnapsToDevicePixels = true;

            // 图层2：Notch背景
            _notchBackground = new System.Windows.Controls.Image();
            _notchBackground.Stretch = Stretch.Fill;
            RenderOptions.SetBitmapScalingMode(_notchBackground, BitmapScalingMode.NearestNeighbor);
            _notchBackground.UseLayoutRounding = true;
            _notchBackground.SnapsToDevicePixels = true;
            _notchBackground.Visibility = Visibility.Collapsed;

            // 图层3：BOSS血条进度
            _barProgress = new System.Windows.Controls.Image();
            _barProgress.Stretch = Stretch.Fill;
            RenderOptions.SetBitmapScalingMode(_barProgress, BitmapScalingMode.NearestNeighbor);
            _barProgress.UseLayoutRounding = true;
            _barProgress.SnapsToDevicePixels = true;
            _barProgress.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

            // 图层4：Notch进度
            _notchProgress = new System.Windows.Controls.Image();
            _notchProgress.Stretch = Stretch.Fill;
            RenderOptions.SetBitmapScalingMode(_notchProgress, BitmapScalingMode.NearestNeighbor);
            _notchProgress.UseLayoutRounding = true;
            _notchProgress.SnapsToDevicePixels = true;
            _notchProgress.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            _notchProgress.Visibility = Visibility.Collapsed;

            _barGrid.Children.Add(_barBackground);
            _barGrid.Children.Add(_notchBackground);
            _barGrid.Children.Add(_barProgress);
            _barGrid.Children.Add(_notchProgress);

            this.Children.Add(_barGrid);

            // 加载图片
            LoadImages();

            // 更新进度
            UpdateProgress();
        }

        /// <summary>
        /// 从图片文件读取原始尺寸
        /// </summary>
        private void LoadDimensions()
        {
            // 设置默认值
            _originalWidth = DefaultWidth;
            _originalHeight = DefaultHeight;
            _originalNotchWidth = DefaultWidth;
            _originalNotchHeight = DefaultHeight;

            try
            {
                // 读取BOSS血条背景图片尺寸
                var barBgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    AssetPaths.GetBossBarPath(_settings.IconType, "background"));
                if (File.Exists(barBgPath))
                {
                    using (var stream = File.OpenRead(barBgPath))
                    {
                        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                        var frame = decoder.Frames[0];
                        _originalWidth = frame.PixelWidth;
                        _originalHeight = frame.PixelHeight;
                    }
                }
            }
            catch { }

            // 读取Notch图片尺寸（如果启用）
            if (!string.IsNullOrEmpty(_settings.NotchType))
            {
                try
                {
                    var notchBgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        AssetPaths.GetNotchPath(_settings.NotchType, "background"));
                    if (File.Exists(notchBgPath))
                    {
                        using (var stream = File.OpenRead(notchBgPath))
                        {
                            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                            var frame = decoder.Frames[0];
                            _originalNotchWidth = frame.PixelWidth;
                            _originalNotchHeight = frame.PixelHeight;
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 获取实际显示宽度（缩放后）
        /// </summary>
        public double GetActualWidth()
        {
            return _originalWidth * _scaleFactor;
        }

        /// <summary>
        /// 获取实际显示高度（缩放后）
        /// </summary>
        public double GetActualHeight()
        {
            return _originalHeight * _scaleFactor;
        }

        /// <summary>
        /// 加载所有图层图片
        /// </summary>
        private void LoadImages()
        {
            double width = _originalWidth * _scaleFactor;
            double height = _originalHeight * _scaleFactor;

            // BOSS血条背景
            var barBgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                AssetPaths.GetBossBarPath(_settings.IconType, "background"));
            if (File.Exists(barBgPath))
            {
                _barBackground.Source = LoadBitmapImage(barBgPath);
                _barBackground.Width = width;
                _barBackground.Height = height;
            }

            // BOSS血条进度
            var barProgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                AssetPaths.GetBossBarPath(_settings.IconType, "progress"));
            if (File.Exists(barProgPath))
            {
                _barProgress.Source = LoadBitmapImage(barProgPath);
                _barProgress.Width = width;
                _barProgress.Height = height;
            }

            // Notch图层（如果启用）
            if (!string.IsNullOrEmpty(_settings.NotchType))
            {
                double notchWidth = _originalNotchWidth * _scaleFactor;
                double notchHeight = _originalNotchHeight * _scaleFactor;

                // Notch背景
                var notchBgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    AssetPaths.GetNotchPath(_settings.NotchType, "background"));
                if (File.Exists(notchBgPath))
                {
                    _notchBackground.Source = LoadBitmapImage(notchBgPath);
                    _notchBackground.Width = notchWidth;
                    _notchBackground.Height = notchHeight;
                    _notchBackground.Visibility = Visibility.Visible;
                }

                // Notch进度
                var notchProgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    AssetPaths.GetNotchPath(_settings.NotchType, "progress"));
                if (File.Exists(notchProgPath))
                {
                    _notchProgress.Source = LoadBitmapImage(notchProgPath);
                    _notchProgress.Width = notchWidth;
                    _notchProgress.Height = notchHeight;
                    _notchProgress.Visibility = Visibility.Visible;
                }
            }
            else
            {
                _notchBackground.Visibility = Visibility.Collapsed;
                _notchProgress.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 加载BitmapImage
        /// </summary>
        private BitmapImage LoadBitmapImage(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// 更新进度显示
        /// </summary>
        public void UpdateProgress()
        {
            double width = _originalWidth * _scaleFactor;
            double height = _originalHeight * _scaleFactor;

            _barProgress.Width = width;
            _barProgress.Height = height;

            // 获取进度百分比
            double percent = GetPercent();

            // 裁剪进度条宽度
            var clipRect = new Rect(0, 0, width * percent, height);
            _barProgress.Clip = new RectangleGeometry(clipRect);

            // Notch进度也使用相同数值
            if (!string.IsNullOrEmpty(_settings.NotchType))
            {
                double notchWidth = _originalNotchWidth * _scaleFactor;
                double notchHeight = _originalNotchHeight * _scaleFactor;
                _notchProgress.Width = notchWidth;
                _notchProgress.Height = notchHeight;
                var notchClipRect = new Rect(0, 0, notchWidth * percent, notchHeight);
                _notchProgress.Clip = new RectangleGeometry(notchClipRect);
            }
        }

        /// <summary>
        /// 获取进度百分比（0.0 - 1.0）
        /// </summary>
        private double GetPercent()
        {
            // 如果启用自定义数值，使用配置的当前值（0-100）
            if (_settings.CustomValueEnabled)
            {
                return _settings.CustomCurrentValue / 100.0;
            }

            // 如果启用数据映射，使用映射数据
            if (_settings.DataMappingEnabled)
            {
                return _parentWindow.GetDataMappingValue(_settings.DataMappingType);
            }

            // 默认100%
            return 1.0;
        }

        /// <summary>
        /// 创建BOSS名称字符（逐字符实现字间距）
        /// </summary>
        private void CreateNameCharacters(string name, double scaleFactor)
        {
            _nameContainer.Children.Clear();

            var fontFamily = new System.Windows.Media.FontFamily(new Uri("pack://application:,,,/"), "/Fonts/unifont-16.0.04.ttf#Unifont");
            var shadowEffect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 315, // 45°右下角
                ShadowDepth = 0.75 * scaleFactor,
                BlurRadius = 0,
                Opacity = 1.0
            };

            foreach (char c in name)
            {
                var charBlock = new TextBlock
                {
                    Text = c.ToString(),
                    FontFamily = fontFamily,
                    FontSize = 8 * scaleFactor,
                    Foreground = System.Windows.Media.Brushes.White,
                    Effect = shadowEffect,
                    Margin = new Thickness(0, 0, 1 * scaleFactor, 0) // 字间距
                };
                _nameContainer.Children.Add(charBlock);
            }

            // 更新容器Margin
            _nameContainer.Margin = new Thickness(0, 3 * scaleFactor, 0, 1 * scaleFactor);
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        public void UpdateSettings(BossBarSettings newSettings)
        {
            _settings = newSettings;
            CreateNameCharacters(newSettings.Name, _scaleFactor);
            LoadDimensions();
            LoadImages();
            UpdateProgress();
            UpdateBarGridSize();
        }

        /// <summary>
        /// 从_settings更新显示（属性变化时调用）
        /// </summary>
        public void UpdateFromSettings()
        {
            CreateNameCharacters(_settings.Name, _scaleFactor);
            LoadDimensions();
            LoadImages();
            UpdateProgress();
            UpdateBarGridSize();
        }

        /// <summary>
        /// 更新Grid尺寸
        /// </summary>
        private void UpdateBarGridSize()
        {
            double width = _originalWidth * _scaleFactor;
            double height = _originalHeight * _scaleFactor;
            _barGrid.Width = width;
            _barGrid.Height = height;
        }

        /// <summary>
        /// 设置缩放比例
        /// </summary>
        public void SetScaleFactor(double scaleFactor)
        {
            _scaleFactor = scaleFactor;

            // 重新创建名称字符以应用新缩放
            CreateNameCharacters(_settings.Name, scaleFactor);

            double width = _originalWidth * scaleFactor;
            double height = _originalHeight * scaleFactor;

            _barGrid.Width = width;
            _barGrid.Height = height;

            _barBackground.Width = width;
            _barBackground.Height = height;
            _barProgress.Width = width;
            _barProgress.Height = height;

            // Notch图层（如果启用）
            if (!string.IsNullOrEmpty(_settings.NotchType))
            {
                double notchWidth = _originalNotchWidth * scaleFactor;
                double notchHeight = _originalNotchHeight * scaleFactor;
                _notchBackground.Width = notchWidth;
                _notchBackground.Height = notchHeight;
                _notchProgress.Width = notchWidth;
                _notchProgress.Height = notchHeight;
            }

            UpdateProgress();
        }
    }
}