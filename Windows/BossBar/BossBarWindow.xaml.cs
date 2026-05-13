using CraftSharp.Models;
using CraftSharp.Services;
using CraftSharp.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CraftSharp.Windows.BossBar
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

        // 原生拖放目标（支持 Windows 拖拽缩略图）
        private IDisposable? _nativeDropTarget;

        // 默认尺寸（当无法从图片读取时使用，与实际图片尺寸一致）
        private const double DefaultBossBarWidth = 182;
        private const double DefaultBossBarHeight = 5;

        public BossBarWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;

            // 设置窗口图标
            IconService.Instance.ApplyWindowIcon(this);

            // 注册原生拖放（仅显示缩略图，不接受文件） + 隐藏 Alt+Tab
            SourceInitialized += (s, e) =>
            {
                DesktopWindowHelper.HideFromAltTab(this);

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

            // 初始化缩放服务
            ScaleService.Instance.Initialize();
            _scaleFactor = ScaleService.Instance.ScaleFactor;

            // 初始化更新定时器
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += UpdateTimer_Tick;

            // 监听BossBars集合变化
            _settings.BossBars.CollectionChanged += BossBars_CollectionChanged;

            // 初始化数据映射服务
            DataMappingService.Instance.Initialize();

            // 初始化窗口位置
            UpdateWindowPosition();

            // 加载所有BOSS血条
            LoadAllBossBars();

            // 启动更新定时器
            _updateTimer.Start();
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

            // 计算正确的插入位置：找到该BOSS血条在BossBars中的索引，
            // 然后统计该索引之前有多少启用的BOSS血条，作为插入位置
            int bossBarIndex = _settings.BossBars.IndexOf(settings);
            int insertIndex = 0;
            for (int i = 0; i < bossBarIndex; i++)
            {
                if (_settings.BossBars[i].IsEnabled)
                {
                    insertIndex++;
                }
            }

            BossBarsContainer.Children.Insert(insertIndex, panel);
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
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move)
            {
                // 拖动排序：同步调整BossBarsContainer中panel的顺序
                // 注意：BossBarsContainer.Children只包含启用的panel，需要计算正确的插入索引
                var movedItem = _settings.BossBars[e.NewStartingIndex];
                if (_bossBarPanels.TryGetValue(movedItem.Id, out var panel) && movedItem.IsEnabled)
                {
                    // 计算在新位置之前有多少启用的BOSS血条
                    int enabledIndex = 0;
                    for (int i = 0; i < e.NewStartingIndex; i++)
                    {
                        if (_settings.BossBars[i].IsEnabled)
                        {
                            enabledIndex++;
                        }
                    }

                    BossBarsContainer.Children.Remove(panel);
                    BossBarsContainer.Children.Insert(enabledIndex, panel);
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

        protected override void OnClosed(EventArgs e)
        {
            _updateTimer.Stop();
            _settings.BossBars.CollectionChanged -= BossBars_CollectionChanged;

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

            // 读取BOSS血条背景图片尺寸
            var barBgPath = AssetPaths.GetBossBarPath(_settings.IconType, "background");
            var (barW, barH) = ImageService.Instance.GetImageDimensions(barBgPath);
            if (barW > 0 && barH > 0)
            {
                _originalWidth = barW;
                _originalHeight = barH;
            }

            // 读取Notch图片尺寸（如果启用）
            if (!string.IsNullOrEmpty(_settings.NotchType))
            {
                var notchBgPath = AssetPaths.GetNotchPath(_settings.NotchType, "background");
                var (notchW, notchH) = ImageService.Instance.GetImageDimensions(notchBgPath);
                if (notchW > 0 && notchH > 0)
                {
                    _originalNotchWidth = notchW;
                    _originalNotchHeight = notchH;
                }
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
            _barBackground.Source = ImageService.Instance.LoadBitmapImage(AssetPaths.GetBossBarPath(_settings.IconType, "background"));
            _barBackground.Width = width;
            _barBackground.Height = height;

            // BOSS血条进度
            _barProgress.Source = ImageService.Instance.LoadBitmapImage(AssetPaths.GetBossBarPath(_settings.IconType, "progress"));
            _barProgress.Width = width;
            _barProgress.Height = height;

            // Notch图层（如果启用）
            if (!string.IsNullOrEmpty(_settings.NotchType))
            {
                double notchWidth = _originalNotchWidth * _scaleFactor;
                double notchHeight = _originalNotchHeight * _scaleFactor;

                // Notch背景
                _notchBackground.Source = ImageService.Instance.LoadBitmapImage(AssetPaths.GetNotchPath(_settings.NotchType, "background"));
                _notchBackground.Width = notchWidth;
                _notchBackground.Height = notchHeight;
                _notchBackground.Visibility = Visibility.Visible;

                // Notch进度
                _notchProgress.Source = ImageService.Instance.LoadBitmapImage(AssetPaths.GetNotchPath(_settings.NotchType, "progress"));
                _notchProgress.Width = notchWidth;
                _notchProgress.Height = notchHeight;
                _notchProgress.Visibility = Visibility.Visible;
            }
            else
            {
                _notchBackground.Visibility = Visibility.Collapsed;
                _notchProgress.Visibility = Visibility.Collapsed;
            }
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
                return DataMappingService.Instance.GetValue(_settings.DataMappingType);
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