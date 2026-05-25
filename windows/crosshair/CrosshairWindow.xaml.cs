using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CraftSharp.Models;
using CraftSharp.Helpers;
using CraftSharp.Services.Core;
using CraftSharp.Services.Hud;
using CraftSharp.Services.Slot;
using CraftSharp.Services.Resource;

namespace CraftSharp.Windows.Crosshair
{
    /// <summary>
    /// 准星窗口 - 显示准星和攻击指示器
    ///
    /// 布局规则：
    /// 1. 窗口覆盖整个屏幕（包括任务栏区域）
    /// 2. 准星显示在屏幕正中间
    /// 3. 攻击指示器显示在准星下方
    /// 4. 不允许拖动
    ///
    /// 显示逻辑：
    /// - 准星：固定图标显示
    /// - 攻击指示器：
    ///   - 进度 < 100%：显示 background + progress（progress裁剪显示进度）
    ///   - 进度 = 100%：只显示 full
    /// </summary>
    public partial class CrosshairWindow : Window
    {
        private double _scaleFactor;
        private double _screenWidth;  // WPF 逻辑像素
        private double _screenHeight; // WPF 逻辑像素
        private AppSettings? _appSettings;

        // 原始图片尺寸
        private double _originalCrosshairWidth;
        private double _originalCrosshairHeight;
        private double _originalAttackIndicatorWidth;
        private double _originalAttackIndicatorHeight;
        private double _originalAttackIndicatorFullWidth;
        private double _originalAttackIndicatorFullHeight;

        // 可见性状态
        private bool _crosshairVisible = false;
        private bool _attackIndicatorVisible = false;

        // 窗口置顶
        private bool _topMost = false;

        // 原生拖放目标（支持 Windows 拖拽缩略图）
        private IDisposable? _nativeDropTarget;

        public CrosshairWindow()
        {
            InitializeComponent();

            // 默认隐藏（准星和攻击指示器默认不显示）
            CrosshairGrid.Visibility = Visibility.Collapsed;
            AttackIndicatorGrid.Visibility = Visibility.Collapsed;

            // 窗口加载后设置位置和尺寸（此时可以获取 DPI 信息）
            Loaded += OnWindowLoaded;

            // 注册原生拖放（仅显示缩略图，不接受文件） + 隐藏 Alt+Tab
            SourceInitialized += (_, _) =>
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
            Closed += (_, _) =>
            {
                _nativeDropTarget?.Dispose();
                _nativeDropTarget = null;
            };
        }

        /// <summary>
        /// 窗口加载完成后设置
        /// </summary>
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // 初始化缩放服务（使用DPI缩放，用于覆盖全屏的窗口）
            var presentationSource = PresentationSource.FromVisual(this);
            ScaleService.Instance.InitializeWithDpi(presentationSource);
            _scaleFactor = ScaleService.Instance.ScaleFactor;
            _screenWidth = ScaleService.Instance.ScreenWidth;
            _screenHeight = ScaleService.Instance.ScreenHeight;

            // 加载图片尺寸
            LoadImageDimensions();

            // 设置窗口覆盖整个屏幕
            SetupWindow();

            // 设置准星位置（屏幕正中间）
            SetupCrosshair();

            // 设置攻击指示器位置（准星下方）
            SetupAttackIndicator();
        }

        /// <summary>
        /// 设置窗口覆盖整个屏幕
        /// </summary>
        private void SetupWindow()
        {
            // 窗口覆盖整个屏幕（包括任务栏）
            Top = 0;
            Left = 0;
            Width = _screenWidth;
            Height = _screenHeight;

            // Canvas 尺寸与窗口相同
            LayoutCanvas.Width = _screenWidth;
            LayoutCanvas.Height = _screenHeight;
        }

        /// <summary>
        /// 设置应用配置
        /// </summary>
        public void SetAppSettings(AppSettings settings)
        {
            _appSettings = settings;
            ApplySettings();
        }

        /// <summary>
        /// 应用配置
        /// </summary>
        private void ApplySettings()
        {
            if (_appSettings == null) return;

            // 准星配置
            var crosshairSettings = _appSettings.HudElements.FirstOrDefault(h => h.Id == "Crosshair");
            if (crosshairSettings != null)
            {
                _crosshairVisible = crosshairSettings.IsVisible;
                _topMost = crosshairSettings.TopMost;
                SetCrosshairVisible(_crosshairVisible);
                SetTopMost(_topMost);
            }

            // 攻击指示器配置
            var attackIndicatorSettings = _appSettings.HudElements.FirstOrDefault(h => h.Id == "AttackIndicator");
            if (attackIndicatorSettings != null)
            {
                _attackIndicatorVisible = attackIndicatorSettings.IsVisible;
                SetAttackIndicatorVisible(_attackIndicatorVisible);
            }
        }

        /// <summary>
        /// 加载图片尺寸
        /// </summary>
        private void LoadImageDimensions()
        {
            // 加载准星图片尺寸
            var (cw, ch) = ImageService.Instance.GetImageDimensions(AssetPaths.Crosshair);
            _originalCrosshairWidth = cw;
            _originalCrosshairHeight = ch;

            // 加载攻击指示器图片尺寸（background/progress: 16x4）
            var (bgw, bgh) = ImageService.Instance.GetImageDimensions(AssetPaths.CrosshairAttackIndicatorBackground);
            _originalAttackIndicatorWidth = bgw;
            _originalAttackIndicatorHeight = bgh;

            // 加载攻击指示器满进度图片尺寸（full: 16x16）
            var (fw, fh) = ImageService.Instance.GetImageDimensions(AssetPaths.CrosshairAttackIndicatorFull);
            _originalAttackIndicatorFullWidth = fw;
            _originalAttackIndicatorFullHeight = fh;
        }

        /// <summary>
        /// 设置准星位置（屏幕正中间）
        /// </summary>
        private void SetupCrosshair()
        {
            double width = _originalCrosshairWidth * _scaleFactor;
            double height = _originalCrosshairHeight * _scaleFactor;

            CrosshairImage.Source = LoadBitmapImage(AssetPaths.Crosshair);
            CrosshairImage.Width = width;
            CrosshairImage.Height = height;
            CrosshairGrid.Width = width;
            CrosshairGrid.Height = height;

            // 准星居中于屏幕
            double centerX = _screenWidth / 2 - width / 2;
            double centerY = _screenHeight / 2 - height / 2;
            CrosshairGrid.SetValue(Canvas.LeftProperty, centerX);
            CrosshairGrid.SetValue(Canvas.TopProperty, centerY);
        }

        /// <summary>
        /// 设置攻击指示器位置（准星下方）
        /// </summary>
        private void SetupAttackIndicator()
        {
            // background/progress 尺寸
            double bgWidth = _originalAttackIndicatorWidth * _scaleFactor;
            double bgHeight = _originalAttackIndicatorHeight * _scaleFactor;

            // full 尺寸
            double fullWidth = _originalAttackIndicatorFullWidth * _scaleFactor;
            double fullHeight = _originalAttackIndicatorFullHeight * _scaleFactor;

            // 准星尺寸
            double crosshairWidth = _originalCrosshairWidth * _scaleFactor;
            double crosshairHeight = _originalCrosshairHeight * _scaleFactor;

            // 间距
            double spacing = _scaleFactor;

            // 攻击指示器水平居中
            double attackIndicatorWidth = Math.Max(bgWidth, fullWidth);
            double centerX = _screenWidth / 2 - attackIndicatorWidth / 2;

            // 攻击指示器垂直位置：准星底部 + spacing
            // 准星中心 Y = screenHeight/2，准星底部 = screenHeight/2 + crosshairHeight/2
            double centerY = _screenHeight / 2 + crosshairHeight / 2 + spacing;

            AttackIndicatorGrid.SetValue(Canvas.LeftProperty, centerX);
            AttackIndicatorGrid.SetValue(Canvas.TopProperty, centerY);

            AttackIndicatorBackground.Source = LoadBitmapImage(AssetPaths.CrosshairAttackIndicatorBackground);
            AttackIndicatorProgress.Source = LoadBitmapImage(AssetPaths.CrosshairAttackIndicatorProgress);
            AttackIndicatorFull.Source = LoadBitmapImage(AssetPaths.CrosshairAttackIndicatorFull);

            // background 和 progress 使用相同尺寸
            AttackIndicatorBackground.Width = bgWidth;
            AttackIndicatorBackground.Height = bgHeight;
            AttackIndicatorProgress.Width = bgWidth;
            AttackIndicatorProgress.Height = bgHeight;

            // full 使用自己的尺寸
            AttackIndicatorFull.Width = fullWidth;
            AttackIndicatorFull.Height = fullHeight;

            // Grid 宽度取最大，高度不固定
            AttackIndicatorGrid.Width = attackIndicatorWidth;
            AttackIndicatorGrid.Height = double.NaN; // Auto

            UpdateAttackIndicatorProgress();
        }

        /// <summary>
        /// 更新攻击指示器进度显示
        /// </summary>
        private void UpdateAttackIndicatorProgress()
        {
            double percent = GetAttackIndicatorPercent();
            double width = _originalAttackIndicatorWidth * _scaleFactor;
            double height = _originalAttackIndicatorHeight * _scaleFactor;

            if (percent >= 1.0)
            {
                // 进度 = 100%：只显示 full
                AttackIndicatorBackground.Visibility = Visibility.Collapsed;
                AttackIndicatorProgress.Visibility = Visibility.Collapsed;
                AttackIndicatorFull.Visibility = Visibility.Visible;
            }
            else
            {
                // 进度 < 100%：显示 background + progress
                AttackIndicatorBackground.Visibility = Visibility.Visible;
                AttackIndicatorProgress.Visibility = Visibility.Visible;
                AttackIndicatorFull.Visibility = Visibility.Collapsed;

                // 裁剪 progress 显示进度部分
                var clipRect = new Rect(0, 0, width * percent, height);
                AttackIndicatorProgress.Clip = new RectangleGeometry(clipRect);
            }
        }

        /// <summary>
        /// 获取攻击指示器进度百分比（0.0 - 1.0）
        /// </summary>
        private double GetAttackIndicatorPercent()
        {
            var settings = _appSettings?.HudElements.FirstOrDefault(h => h.Id == "AttackIndicator");

            // 如果启用自定义数值，使用配置的当前值（0-100）
            if (settings?.CustomValueEnabled == true)
            {
                int currentValue = settings.CustomCurrentValue;
                return currentValue / 100.0;
            }

            // 否则使用数据映射
            string mappingType = settings?.DataMappingType ?? "BatteryLevel";
            return DataMappingService.Instance.GetValue(mappingType);
        }

        /// <summary>
        /// 设置准星可见性
        /// </summary>
        public void SetCrosshairVisible(bool visible)
        {
            _crosshairVisible = visible;
            CrosshairGrid.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            UpdateWindowVisibility();
        }

        /// <summary>
        /// 设置攻击指示器可见性
        /// </summary>
        public void SetAttackIndicatorVisible(bool visible)
        {
            _attackIndicatorVisible = visible;
            AttackIndicatorGrid.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            UpdateAttackIndicatorProgress();
            UpdateWindowVisibility();
        }

        /// <summary>
        /// 设置窗口置顶
        /// </summary>
        public void SetTopMost(bool topMost)
        {
            _topMost = topMost;
            Topmost = topMost;
        }

        /// <summary>
        /// 更新窗口可见性（准星或攻击指示器任一可见时显示窗口）
        /// </summary>
        private void UpdateWindowVisibility()
        {
            if (_crosshairVisible || _attackIndicatorVisible)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// 刷新攻击指示器显示
        /// </summary>
        public void RefreshAttackIndicator()
        {
            UpdateAttackIndicatorProgress();
        }

        /// <summary>
        /// 从相对路径加载 BitmapImage（使用 ImageService）
        /// </summary>
        private static BitmapImage LoadBitmapImage(string relativePath)
        {
            return ImageService.Instance.LoadBitmapImage(relativePath)!;
        }
    }
}