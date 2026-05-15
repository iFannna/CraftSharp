using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CraftSharp.Helpers;
using CraftSharp.Services;

#pragma warning disable CS8618 // 服务字段在 partial class 的 InitializeSlotServices 中初始化

namespace CraftSharp.Windows.StatusBar
{
    /// <summary>
    /// 状态栏窗口主文件 - 构造函数、缩放、布局核心逻辑
    ///
    /// 布局规则：
    /// 1. 整个状态栏水平居中、垂直紧贴窗口底部
    /// 2. 核心容器固定宽度182*scaleFactor（1092px基准在2560分辨率6倍放大）
    /// 3. 副手槽位于核心容器外部，间距42*scaleFactor
    /// 4. 使用StackPanel + VerticalAlignment="Bottom"实现从下往上堆叠
    /// 5. 隐藏组件使用Visibility.Collapsed，上方组件自动向下掉落
    /// 6. 全局垂直间距6px基准
    /// </summary>
    public partial class StatusBarWindow : Window
    {
        // 核心容器基准宽度：182像素（182×6=1092px）
        private const double BaseCoreContainerWidth = 182;

        private double _scaleFactor;
        private DispatcherTimer? _batteryTimer;
        private bool _isLocked = false;

        // 副手槽状态
        private bool _leftOffhandEnabled = false;
        private bool _rightOffhandEnabled = false;

        // 各组件可见性状态
        private bool _hotbarVisible = true;
        private bool _expBarVisible = true;
        private bool _healthVisible = true;
        private bool _foodVisible = true;
        private bool _airVisible = true;
        private bool _armorVisible = true;
        private bool _absorbingVisible = true;

        // 标记：是否跳过构造函数中的默认定位（用于"记住位置"功能）
        private bool _skipDefaultPositioning = false;

        // 拖动状态
        private bool _isDragging = false;
        private double _dragOffsetX;  // 鼠标相对于窗口左上角的偏移
        private double _dragOffsetY;

        // 文件名显示定时器
        private DispatcherTimer? _fileNameTimer;

        // 原生拖放目标（支持 Windows 拖拽缩略图）
        private IDisposable? _nativeDropTarget;

        // 应用设置
        private Models.AppSettings? _appSettings;

        /// <summary>
        /// 设置跳过默认定位（必须在构造函数之前通过静态方式设置）
        /// </summary>
        public static bool ShouldSkipDefaultPositioning { get; set; } = false;

        /// <summary>
        /// 窗口位置变化事件（用于即时保存位置）
        /// </summary>
        public event EventHandler? PositionChanged;

        public StatusBarWindow()
        {
            // 从静态属性读取是否跳过默认定位
            _skipDefaultPositioning = ShouldSkipDefaultPositioning;
            // 重置静态属性（避免影响下次创建）
            ShouldSkipDefaultPositioning = false;

            InitializeComponent();

            // 设置窗口到桌面层级 + 隐藏 Alt+Tab
            SourceInitialized += (s, e) =>
            {
                DesktopWindowHelper.SetWindowToDesktopLevelAndHideAltTab(this);

                // 注册原生拖放（支持 Windows 拖拽缩略图显示 + 处理文件放置）
                try
                {
                    _nativeDropTarget = NativeDropHelper.RegisterWithDropHandler(
                        this,
                        HandleNativeDrop,
                        CanDropAtPosition);
                }
                catch (Exception)
                {
                    _nativeDropTarget?.Dispose();
                    _nativeDropTarget = null;
                }
            };

            // 监听窗口位置变化（用于即时保存位置）
            LocationChanged += OnLocationChanged;

            // 监听窗口关闭事件（释放LibreHardwareMonitor资源）
            Closed += OnWindowClosed;

            // 监听窗口失焦事件（清除快捷栏格子选中状态）
            Deactivated += OnWindowDeactivated;

            // 使用 SlotDataService 单例（已在 Hotbar.cs 字段声明中初始化）

            // 初始化数据映射服务
            DataMappingService.Instance.Initialize();

            // 初始化缩放服务
            ScaleService.Instance.Initialize();

            // 获取缩放比例
            _scaleFactor = ScaleService.Instance.ScaleFactor;

            // 初始化格子相关服务（需要在 _scaleFactor 和 _appSettings 初始化后）
            InitializeSlotServices();

            // 初始化文件名显示样式
            InitializeFileNameDisplay();

            // 获取原始图片尺寸（调用各模块的加载方法）
            GetOriginalImageSize();

            // 设置窗口尺寸
            SetWindowSize();

            // 设置心形生命值
            SetupHearts();

            // 设置饥饿值
            SetupFood();

            // 设置空气值
            SetupAir();

            // 设置伤害吸收值
            SetupAbsorbing();

            // 设置护甲值
            SetupArmor();

            // 设置经验条
            SetupExperienceBar();

            // 设置快捷栏
            SetupHotbar();

            // 设置状态组行的宽度（核心容器宽度）
            StatusRowGrid.Width = GetCoreContainerWidth();
            // 与下方经验条间距：6px基准（Margin.Bottom在上层元素上）
            StatusRowGrid.Margin = new Thickness(0, 0, 0, BaseVerticalSpacing * _scaleFactor);

            // 设置副手槽
            SetupOffhandSlots();

            // 设置格子位置和大小
            SetupSlots();

            LoadSlots();

            // 默认定位到屏幕底部居中（如果未跳过）
            if (!_skipDefaultPositioning)
            {
                // 窗口尺寸在 Loaded 事件后才完全计算好，所以延迟定位
                Loaded += (s, e) => PositionWindow();
            }

            // 启动电量更新定时器
            StartBatteryTimer();
        }

        /// <summary>
        /// 设置跳过默认定位（实例方法，供外部调用）
        /// </summary>
        public void SetSkipDefaultPositioning(bool skip)
        {
            _skipDefaultPositioning = skip;
        }

        /// <summary>
        /// 窗口位置变化事件处理
        /// </summary>
        private void OnLocationChanged(object? sender, EventArgs e)
        {
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 窗口关闭事件处理（释放资源）
        /// </summary>
        private void OnWindowClosed(object? sender, EventArgs e)
        {
            // 停止定时器
            if (_batteryTimer != null)
            {
                _batteryTimer.Stop();
                _batteryTimer = null;
            }

            // 停止文件名显示定时器
            if (_fileNameTimer != null)
            {
                _fileNameTimer.Stop();
                _fileNameTimer = null;
            }

            // 释放原生拖放资源
            _nativeDropTarget?.Dispose();
            _nativeDropTarget = null;
        }

        /// <summary>
        /// 窗口失焦事件处理（清除快捷栏格子选中状态）
        /// </summary>
        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            ClearSelection();
        }

        /// <summary>
        /// 从文件路径加载 BitmapImage（使用 ImageService）
        /// </summary>
        protected static BitmapImage LoadBitmapImage(string relativePath)
        {
            return ImageService.Instance.LoadBitmapImage(relativePath)!;
        }

        /// <summary>
        /// 获取原始图片尺寸（调用各模块的加载方法）
        /// </summary>
        private void GetOriginalImageSize()
        {
            LoadHotbarDimensions();
            LoadOffhandDimensions();
            LoadExpBarDimensions();
            LoadHeartDimensions();
            LoadFoodDimensions();
            LoadSaturationDimensions();
            LoadAirDimensions();
            LoadAbsorbingDimensions();
            LoadArmorDimensions();
        }

        /// <summary>
        /// 获取核心容器宽度（182×缩放比例）
        /// </summary>
        private double GetCoreContainerWidth()
        {
            return BaseCoreContainerWidth * _scaleFactor;
        }

        /// <summary>
        /// 设置窗口尺寸
        /// 使用固定列宽度，避免Collapsed时列收缩导致位置偏移
        /// </summary>
        private void SetWindowSize()
        {
            // 核心容器宽度
            double coreWidth = GetCoreContainerWidth();
            CoreContainerGrid.Width = coreWidth;

            // 副手槽宽度和间距
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double offhandHeight = _originalOffhandHeight * _scaleFactor;
            double offhandSpacing = BaseOffhandSpacing * _scaleFactor; // 42px基准间距

            // 设置各列的固定宽度（使用GridLength）
            LeftOffhandColumn.Width = new GridLength(offhandWidth);
            LeftSpacingColumn.Width = new GridLength(offhandSpacing);
            CoreContainerColumn.Width = new GridLength(coreWidth);
            RightSpacingColumn.Width = new GridLength(offhandSpacing);
            RightOffhandColumn.Width = new GridLength(offhandWidth);

            // 设置副手槽尺寸
            LeftOffhandGrid.Width = offhandWidth;
            LeftOffhandGrid.Height = offhandHeight;
            RightOffhandGrid.Width = offhandWidth;
            RightOffhandGrid.Height = offhandHeight;

            // 设置副手槽可见性
            LeftOffhandGrid.Visibility = _leftOffhandEnabled ? Visibility.Visible : Visibility.Collapsed;
            RightOffhandGrid.Visibility = _rightOffhandEnabled ? Visibility.Visible : Visibility.Collapsed;

            // 窗口宽度 = 所有列宽度之和（固定宽度，不随副手槽可见性变化）
            Width = offhandWidth + offhandSpacing + coreWidth + offhandSpacing + offhandWidth;

            // 窗口高度：使用足够大的固定高度容纳所有可能的组件
            // 伤害吸收值最大情况：1024上限 = 512个图标，每行10个，最多52行
            Height = CalculateMaxWindowHeight();
        }

        /// <summary>
        /// 计算窗口最大高度（所有组件都显示时的高度）
        /// 伤害吸收值最大：1024上限 = 512个图标，每行10个，最多52行
        /// </summary>
        private double CalculateMaxWindowHeight()
        {
            double spacing = BaseVerticalSpacing * _scaleFactor;

            // 快捷栏高度
            double hotbarHeight = _originalHotbarHeight * _scaleFactor;

            // 经验条高度
            double expBarHeight = _originalExpBarHeight * _scaleFactor;

            // 生命值高度（一行10个心形）
            double heartHeight = _originalHeartHeight * _scaleFactor;

            // 饥饿值高度
            double foodHeight = _originalFoodHeight * _scaleFactor;

            // 空气值高度
            double airHeight = _originalAirHeight * _scaleFactor;

            // 护甲值高度（一行10个护甲图标）
            double armorHeight = _originalArmorHeight * _scaleFactor;

            // 伤害吸收值最大高度计算
            // maxValue上限1024，每2点一个完整图标 = 512个图标
            // 每行10个图标 = 最多52行
            // 行间距最多减7（BaseVerticalSpacing - 7）
            int maxAbsorbingSlots = 512;
            int maxAbsorbingRows = (maxAbsorbingSlots - 1) / AbsorbingIconsPerRow + 1; // 52行
            int maxGapAdjustment = Math.Min(maxAbsorbingRows - 1, 7); // 7
            double absorbingHeight = _originalAbsorbingFullHeight * _scaleFactor;
            double absorbingRowGap = (BaseVerticalSpacing - maxGapAdjustment) * _scaleFactor; // 最小间距
            double maxAbsorbingHeight = maxAbsorbingRows * absorbingHeight + (maxAbsorbingRows - 1) * absorbingRowGap;

            // 左列高度：护甲 + 护甲间距 + 吸收 + 吸收间距 + 生命值
            // 护甲与吸收间距：BaseVerticalSpacing
            // 吸收与生命值间距：BaseVerticalSpacing - gapAdjustment
            double leftColumnHeight = armorHeight + spacing + maxAbsorbingHeight + ((BaseVerticalSpacing - maxGapAdjustment) * _scaleFactor) + heartHeight;

            // 右列高度：空气 + 空气间距 + 饥饿
            double rightColumnHeight = airHeight + spacing + foodHeight;

            // 状态行高度
            double statusRowHeight = Math.Max(leftColumnHeight, rightColumnHeight);

            // 总高度：状态行 + 状态行间距 + 经验条 + 经验条间距 + 快捷栏
            double totalHeight = statusRowHeight + spacing + expBarHeight + spacing + hotbarHeight;

            return totalHeight;
        }

        /// <summary>
        /// 启动电量更新定时器
        /// </summary>
        private void StartBatteryTimer()
        {
            _batteryTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _batteryTimer.Tick += (s, e) =>
            {
                UpdateExpBarProgress();
                UpdateHeartLevel();
                UpdateFoodLevel();
                UpdateAirLevel();
            };
            _batteryTimer.Start();
        }

        /// <summary>
        /// 初始化文件名显示样式
        /// </summary>
        private void InitializeFileNameDisplay()
        {
            // 设置字体样式
            var fontFamily = new System.Windows.Media.FontFamily(new Uri("pack://application:,,,/"), "/Fonts/unifont-16.0.04.ttf#Unifont");
            FileNameTextBlock.FontFamily = fontFamily;
            FileNameTextBlock.FontSize = 8 * _scaleFactor;

            // 从配置读取颜色并设置
            string colorHex = _appSettings?.Hotbar.FileNameColor ?? "#FFFFFF";
            var textColor = ParseColorFromHex(colorHex);
            FileNameTextBlock.Foreground = new SolidColorBrush(textColor);

            // 根据文本颜色计算阴影颜色（加深）
            var shadowColor = CalculateShadowColor(textColor);
            var shadowEffect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = shadowColor,
                Direction = 315,
                ShadowDepth = 0.75 * _scaleFactor,
                BlurRadius = 0,
                Opacity = 1.0
            };
            FileNameTextBlock.Effect = shadowEffect;

            // 设置 Grid 底部距离（距离窗口底部 50*scaleFactor）
            FileNameGrid.Margin = new Thickness(0, 0, 0, 50 * _scaleFactor);

            // 初始化文件名显示定时器
            _fileNameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2000)
            };
            _fileNameTimer.Tick += (s, e) =>
            {
                HideFileName();
            };
        }

        /// <summary>
        /// 刷新文件名显示颜色
        /// </summary>
        public void RefreshFileNameColor()
        {
            string colorHex = _appSettings?.Hotbar.FileNameColor ?? "#FFFFFF";
            var textColor = ParseColorFromHex(colorHex);
            FileNameTextBlock.Foreground = new SolidColorBrush(textColor);

            // 同步更新阴影颜色
            var shadowColor = CalculateShadowColor(textColor);
            if (FileNameTextBlock.Effect is System.Windows.Media.Effects.DropShadowEffect shadowEffect)
            {
                shadowEffect.Color = shadowColor;
            }
        }

        /// <summary>
        /// 解析十六进制颜色字符串（支持 #RRGGBB 和 #AARRGGBB 格式）
        /// </summary>
        private System.Windows.Media.Color ParseColorFromHex(string hex)
        {
            hex = hex.TrimStart('#');

            try
            {
                if (hex.Length == 8)
                {
                    byte a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                    return System.Windows.Media.Color.FromArgb(a, r, g, b);
                }
                else if (hex.Length == 6)
                {
                    byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    return System.Windows.Media.Color.FromRgb(r, g, b);
                }
            }
            catch
            {
            }

            return System.Windows.Media.Colors.White;
        }

        /// <summary>
        /// 根据文本颜色计算加深后的阴影颜色
        /// </summary>
        private System.Windows.Media.Color CalculateShadowColor(System.Windows.Media.Color textColor)
        {
            // 将 RGB 值乘以系数加深（系数越小越深）
            double darkenFactor = 0.5; // 加深到50%，阴影与文本颜色协调
            byte r = (byte)Math.Round(textColor.R * darkenFactor);
            byte g = (byte)Math.Round(textColor.G * darkenFactor);
            byte b = (byte)Math.Round(textColor.B * darkenFactor);
            return System.Windows.Media.Color.FromRgb(r, g, b);
        }

        /// <summary>
        /// 从十六进制字符串创建 SolidColorBrush（支持 #RRGGBB 和 #AARRGGBB 格式）
        /// </summary>
        private System.Windows.Media.SolidColorBrush CreateBrushFromHex(string hex)
        {
            hex = hex.TrimStart('#');

            try
            {
                if (hex.Length == 8)
                {
                    // 8 位格式（#AARRGGBB），使用 Alpha
                    byte a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(a, r, g, b));
                }
                else if (hex.Length == 6)
                {
                    // 6 位格式（#RRGGBB），Alpha 默认 255
                    byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
                }
            }
            catch
            {
            }

            return System.Windows.Media.Brushes.White;
        }

        /// <summary>
        /// 显示文件名（选中格子时调用）
        /// </summary>
        public void ShowFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                HideFileName();
                return;
            }

            // 立即清空之前的显示（停止任何正在进行的动画，无动画切换）
            _fileNameTimer?.Stop();
            FileNameTextBlock.BeginAnimation(System.Windows.UIElement.OpacityProperty, null);
            FileNameTextBlock.Opacity = 1.0;

            // 设置文件名（Grid 会自动居中）
            FileNameTextBlock.Text = fileName;
            FileNameTextBlock.Visibility = Visibility.Visible;

            // 启动定时器（2000ms后触发渐隐动画）
            _fileNameTimer?.Start();
        }

        /// <summary>
        /// 隐藏文件名（带渐隐动画）
        /// </summary>
        public void HideFileName()
        {
            _fileNameTimer?.Stop();

            // 创建渐隐动画（500ms）
            var fadeAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(500)
            };

            // 动画完成后隐藏
            fadeAnimation.Completed += (s, e) =>
            {
                FileNameTextBlock.Visibility = Visibility.Collapsed;
                FileNameTextBlock.Text = string.Empty;
                FileNameTextBlock.Opacity = 1.0; // 重置以便下次使用
            };

            FileNameTextBlock.BeginAnimation(System.Windows.UIElement.OpacityProperty, fadeAnimation);
        }

        /// <summary>
        /// 立即隐藏文件名（无动画，用于切换选中时）
        /// </summary>
        public void HideFileNameImmediately()
        {
            _fileNameTimer?.Stop();
            FileNameTextBlock.BeginAnimation(System.Windows.UIElement.OpacityProperty, null);
            FileNameTextBlock.Visibility = Visibility.Collapsed;
            FileNameTextBlock.Text = string.Empty;
            FileNameTextBlock.Opacity = 1.0;
        }

        /// <summary>
        /// 定位窗口到屏幕底部，窗口水平居中
        /// 窗口宽度固定（包含两侧副手槽空间），核心容器居中于窗口
        /// </summary>
        private void PositionWindow()
        {
            // 先让窗口计算实际尺寸
            UpdateLayout();

            // 窗口水平居中
            CenterWindowHorizontally();

            // 状态栏垂直定位到屏幕底部（贴着任务栏上方）
            PositionWindowToBottom();
        }

        /// <summary>
        /// 窗口水平居中
        /// </summary>
        private void CenterWindowHorizontally()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            double actualWidth = ActualWidth > 0 ? ActualWidth : Width;

            if (actualWidth > 0 && !double.IsNaN(actualWidth))
            {
                Left = (screenWidth - actualWidth) / 2;
            }
            else
            {
                // 窗口还没计算好尺寸，使用估算宽度
                double coreWidth = GetCoreContainerWidth();
                double offhandWidth = _originalOffhandWidth * _scaleFactor;
                double offhandSpacing = BaseOffhandSpacing * _scaleFactor;
                double estimatedWidth = coreWidth + (offhandWidth + offhandSpacing) * 2;
                Left = (screenWidth - estimatedWidth) / 2;
            }
        }

        /// <summary>
        /// 窗口垂直定位到屏幕底部（贴着任务栏上方）
        /// </summary>
        private void PositionWindowToBottom()
        {
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var workingAreaHeight = SystemParameters.WorkArea.Height; // 工作区高度（排除任务栏）
            var workingAreaTop = SystemParameters.WorkArea.Top;       // 工作区顶部Y坐标

            double actualHeight = ActualHeight > 0 ? ActualHeight : Height;

            // 计算窗口底部应该贴着工作区底部（即任务栏上方）
            // 工作区底部Y坐标 = workingAreaTop + workingAreaHeight
            // 窗口Top = 工作区底部Y - 窗口高度 - 边距
            

            if (actualHeight > 0 && !double.IsNaN(actualHeight))
            {
                Top = workingAreaTop + workingAreaHeight - actualHeight ;
            }
            else
            {
                // 估算高度
                Top = workingAreaTop + workingAreaHeight - 200 * _scaleFactor;
            }

            Top = Math.Max(workingAreaTop, Top);
        }

        /// <summary>
        /// 公开方法：定位到屏幕底部水平居中（供 StatusBarService 调用）
        /// </summary>
        public void PositionToScreenBottomCenter()
        {
            // 确保尺寸已计算
            UpdateLayout();
            CenterWindowHorizontally();
            PositionWindowToBottom();
        }

        /// <summary>
        /// 设置窗口锁定状态
        /// </summary>
        public void SetLocked(bool locked)
        {
            _isLocked = locked;
        }

        /// <summary>
        /// 设置应用配置
        /// </summary>
        public void SetAppSettings(Models.AppSettings settings)
        {
            _appSettings = settings;

            // 重新创建 _iconService 以使用最新的配置（特别是 ShowTargetIcon）
            // 先取消旧事件订阅
            if (_iconService != null)
            {
                _iconService.IconNeedsUpdate -= OnIconNeedsUpdate;
            }

            var fileValidator = SlotFileValidator.Instance;
            _iconService = new SlotIconService(fileValidator, _appSettings, _scaleFactor);
            _iconService.IconNeedsUpdate += OnIconNeedsUpdate;

            // 配置设置后重新Setup所有HUD元素（使用配置文件中的值）
            SetupHearts();
            SetupFood();
            SetupAir();
            SetupArmor();
            SetupAbsorbing();
            SetupExperienceBar();

            // 配置设置后刷新文件名颜色
            RefreshFileNameColor();
        }

        /// <summary>
        /// 获取HUD元素配置
        /// </summary>
        private Models.HudElementSettings? GetHudElementSettings(string id)
        {
            return _appSettings?.HudElements.FirstOrDefault(h => h.Id == id);
        }

        /// <summary>
        /// 判断鼠标位置是否可以接受文件放置（用于设置拖拽光标）
        /// </summary>
        private bool CanDropAtPosition(System.Windows.Point screenPoint)
        {
            // 将屏幕坐标转换为窗口坐标
            var mousePos = PointFromScreen(screenPoint);
            // 判断是否在格子区域内
            return GetSlotIndexAtPosition(mousePos) >= 0;
        }

        /// <summary>
        /// 处理原生拖放回调（Windows 拖拽缩略图支持）
        /// 根据鼠标位置判断落在哪个格子，处理文件放置
        /// 仅处理外部文件拖入
        /// </summary>
        private void HandleNativeDrop(IReadOnlyList<string> paths, System.Windows.Point screenPoint)
        {
            if (paths.Count == 0) return;

            // 将屏幕坐标转换为窗口坐标
            var mousePos = PointFromScreen(screenPoint);

            // 判断鼠标落在哪个格子
            int slotIndex = GetSlotIndexAtPosition(mousePos);

            if (slotIndex >= 0)
            {
                // 外部文件拖入：添加到格子
                var filePath = paths[0];
                ProcessFileDrop(slotIndex, filePath);
            }
        }

        /// <summary>
        /// 根Grid鼠标按下事件 - 开始自定义拖动 + 清除格子选中状态
        /// </summary>
        private void RootGrid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 检查点击目标是否是格子（如果是格子，不清除选中也不捕获鼠标）
            if (IsClickOnSlot(e.OriginalSource))
            {
                return; // 让格子处理点击事件
            }

            // 清除快捷栏格子选中状态（点击空白区域）
            ClearSelection();

            if (!_isLocked)
            {
                _isDragging = true;
                // 记录鼠标相对于窗口左上角的偏移（WPF逻辑坐标）
                System.Windows.Point mousePos = e.GetPosition(this);
                _dragOffsetX = mousePos.X;
                _dragOffsetY = mousePos.Y;
                CaptureMouse();
            }
        }

        /// <summary>
        /// 检查点击目标是否是格子
        /// </summary>
        private bool IsClickOnSlot(object originalSource)
        {
            // 检查是否是 Border（格子）
            if (originalSource is Border border)
            {
                // 检查是否是主快捷栏格子
                for (int i = 0; i < 9; i++)
                {
                    if (border.Name == $"Slot{i}")
                        return true;
                }
                // 检查是否是副手槽格子
                if (border.Name == "SlotLeftOffhand" || border.Name == "SlotRightOffhand")
                    return true;
            }

            // 检查是否是格子内的 Image（图标）
            if (originalSource is System.Windows.Controls.Image image)
            {
                // 检查父元素是否是格子 Border
                var parent = VisualTreeHelper.GetParent(image);
                while (parent != null)
                {
                    if (parent is Border parentBorder)
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            if (parentBorder.Name == $"Slot{i}")
                                return true;
                        }
                        if (parentBorder.Name == "SlotLeftOffhand" || parentBorder.Name == "SlotRightOffhand")
                            return true;
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }
            }

            return false;
        }

        /// <summary>
        /// 鼠标移动事件 - 执行自定义拖动 + 处理格子拖动
        /// </summary>
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            // 处理格子拖动（优先级高于窗口拖动）
            if (_dragService.IsDragging)
            {
                var mousePos = e.GetPosition(this);
                UpdateDragIconPosition(mousePos);
            }
            // 处理窗口拖动
            else if (_isDragging && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                // 获取鼠标在屏幕上的位置（WPF单位）
                System.Windows.Point screenPoint = PointToScreen(new System.Windows.Point(0, 0));
                System.Windows.Point mouseScreenPoint = e.GetPosition(null);

                // 直接获取鼠标相对于窗口的位置，更新窗口位置使鼠标保持在原位置
                System.Windows.Point currentMousePos = e.GetPosition(this);
                Left += currentMousePos.X - _dragOffsetX;
                Top += currentMousePos.Y - _dragOffsetY;
            }
            base.OnMouseMove(e);
        }

        /// <summary>
        /// 鼠标左键释放事件 - 结束拖动（窗口拖动或格子拖动）
        /// </summary>
        protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            // 处理格子拖动结束
            if (_dragService.IsDragging)
            {
                EndSlotDrag();
            }
            // 处理窗口拖动结束
            else if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();
            }
            base.OnMouseLeftButtonUp(e);
        }

        /// <summary>
        /// 设置快捷栏可见性
        /// 使用Visibility.Collapsed实现重力布局：上方组件自动向下掉落
        /// </summary>
        public void SetHotbarVisible(bool visible)
        {
            _hotbarVisible = visible;
            HotbarGrid.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            // 同步控制主快捷栏格子显示
            for (int i = 0; i < 9; i++)
            {
                var border = GetSlotBorder(i);
                if (border != null)
                {
                    border.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            // 窗口高度自动适应（无需手动计算）
        }

        /// <summary>
        /// 设置经验条可见性
        /// </summary>
        public void SetExpBarVisible(bool visible)
        {
            _expBarVisible = visible;
            ExperienceBarGrid.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 设置生命值可见性
        /// </summary>
        public void SetHealthVisible(bool visible)
        {
            _healthVisible = visible;
            HeartGrid.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 设置饥饿值可见性
        /// </summary>
        public void SetFoodVisible(bool visible)
        {
            _foodVisible = visible;
            FoodGrid.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 设置空气值可见性
        /// </summary>
        public void SetAirVisible(bool visible)
        {
            _airVisible = visible;
            AirGrid.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 设置护甲值可见性
        /// </summary>
        public void SetArmorVisible(bool visible)
        {
            _armorVisible = visible;
            ArmorGrid.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 设置伤害吸收值可见性
        /// </summary>
        public void SetAbsorbingVisible(bool visible)
        {
            _absorbingVisible = visible;
            AbsorbingGrid.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 刷新指定HUD元素的显示（重新创建图标并更新显示）
        /// </summary>
        public void RefreshHudElement(string id)
        {
            switch (id)
            {
                case "health":
                    SetupHearts();
                    break;
                case "food":
                    SetupFood();
                    break;
                case "armor":
                    SetupArmor();
                    break;
                case "absorbing":
                    SetupAbsorbing();
                    break;
                case "air":
                    SetupAir();
                    break;
                case "expbar":
                    SetupExperienceBar();
                    break;
            }
        }
    }
}

#pragma warning restore CS8618