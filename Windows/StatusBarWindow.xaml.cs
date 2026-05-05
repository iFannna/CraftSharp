using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CraftSharp.Helpers;

namespace CraftSharp.Windows
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
        // 基准分辨率：2560下放大6倍
        private const double BaseScreenWidth = 2560;
        private const double BaseScaleMultiplier = 6;

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

        public StatusBarWindow()
        {
            InitializeComponent();

            // 设置窗口到桌面层级
            SourceInitialized += (s, e) => DesktopWindowHelper.SetWindowToDesktopLevel(this);

            // 初始化槽位数据服务
            _slotService = new Services.SlotDataService();

            // 获取原始图片尺寸（调用各模块的加载方法）
            GetOriginalImageSize();

            // 计算缩放比例
            CalculateScale();

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
            PositionWindow();

            // 启动电量更新定时器
            StartBatteryTimer();
        }

        /// <summary>
        /// 从文件路径加载 BitmapImage（用于 Content 配置的资源）
        /// </summary>
        protected static BitmapImage LoadBitmapImage(string relativePath)
        {
            var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
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
            LoadAirDimensions();
            LoadAbsorbingDimensions();
            LoadArmorDimensions();
        }

        /// <summary>
        /// 根据屏幕分辨率计算缩放比例
        /// </summary>
        private void CalculateScale()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            _scaleFactor = (screenWidth / BaseScreenWidth) * BaseScaleMultiplier;
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

            // 窗口高度：根据可见组件动态计算
            Height = double.NaN; // 自动高度
        }

        /// <summary>
        /// 启动电量更新定时器
        /// </summary>
        private void StartBatteryTimer()
        {
            _batteryTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _batteryTimer.Tick += (s, e) =>
            {
                UpdateBatteryLevel();
                UpdateHeartLevel();
                UpdateFoodLevel();
                UpdateAirLevel();
            };
            _batteryTimer.Start();
        }

        /// <summary>
        /// 定位窗口到屏幕底部，窗口水平居中
        /// 窗口宽度固定（包含两侧副手槽空间），核心容器居中于窗口
        /// </summary>
        private void PositionWindow()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // 先让窗口计算实际尺寸
            UpdateLayout();

            // 窗口水平居中（窗口宽度已固定，包含两侧副手槽空间）
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

            // 状态栏垂直紧贴窗口底部（留10*scaleFactor的边距）
            double actualHeight = ActualHeight > 0 ? ActualHeight : Height;
            if (actualHeight > 0 && !double.IsNaN(actualHeight))
            {
                Top = screenHeight - actualHeight - 10 * _scaleFactor;
            }
            else
            {
                // 估算高度
                Top = screenHeight - 200 * _scaleFactor;
            }
            Top = Math.Max(0, Top);
        }

        /// <summary>
        /// 设置窗口锁定状态
        /// </summary>
        public void SetLocked(bool locked)
        {
            _isLocked = locked;
        }

        /// <summary>
        /// 根Grid鼠标按下事件 - 实现窗口拖动
        /// </summary>
        private void RootGrid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isLocked)
            {
                this.DragMove();
            }
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
    }
}