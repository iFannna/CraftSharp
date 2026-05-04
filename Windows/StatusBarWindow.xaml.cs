using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CraftSharp.Helpers;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 状态栏窗口主文件 - 构造函数、缩放、布局核心逻辑
    ///
    /// 布局规则：
    /// 1. 快捷栏是所有组件水平位置的基准点，始终在屏幕底部水平居中
    /// 2. 副手槽浮动在快捷栏左右两侧（间距6px），不影响快捷栏的基准位置
    /// 3. 垂直方向有重力特性：下方组件关闭后，上方组件自动下移填补空位
    /// </summary>
    public partial class StatusBarWindow : Window
    {
        // 基准分辨率：2560下放大6倍
        private const double BaseScreenWidth = 2560;
        private const double BaseScaleMultiplier = 6;

        private double _scaleFactor;
        private DispatcherTimer? _batteryTimer;
        private bool _isLocked = false;

        // 副手槽状态
        private bool _leftOffhandEnabled = false;
        private bool _rightOffhandEnabled = false;

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

            // 设置经验条
            SetupExperienceBar();

            // 设置快捷栏
            SetupHotbar();

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
        /// 获取快捷栏在窗口内的左边位置
        /// 快捷栏始终在窗口内固定位置（窗口尺寸固定，包含所有副手槽空间）
        /// </summary>
        private double GetHotbarLeft()
        {
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double spacing = _offhandSpacing * _scaleFactor;
            // 快捷栏左边始终是左副手槽宽度+间距（无论左副手槽是否显示）
            return offhandWidth + spacing;
        }

        /// <summary>
        /// 获取底部偏移量（快捷栏可见时返回快捷栏高度+间距，否则返回0）
        /// 用于重力布局：组件根据此偏移量决定Y位置
        /// </summary>
        private double GetBottomOffset()
        {
            if (_hotbarVisible)
            {
                double hotbarHeight = _originalHotbarHeight * _scaleFactor;
                double spacing = _spacing * _scaleFactor;
                return hotbarHeight + spacing;
            }
            return 0;
        }

        /// <summary>
        /// 设置窗口尺寸
        /// 窗口宽度固定，高度根据组件可见性动态调整（重力布局）
        /// </summary>
        private void SetWindowSize()
        {
            // 窗口宽度固定 = 快捷栏宽度 + 左副手槽空间 + 右副手槽空间 + 间距
            double hotbarWidth = _originalHotbarWidth * _scaleFactor;
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double spacing = _offhandSpacing * _scaleFactor;

            // 固定窗口宽度，包含所有副手槽空间
            double windowWidth = hotbarWidth + (offhandWidth + spacing) * 2;
            Width = windowWidth;

            // 窗口高度从下往上计算（重力布局）
            double height = 0;

            // 底层：快捷栏或副手槽（至少一个可见时）
            bool hasBottomRow = _hotbarVisible || _leftOffhandEnabled || _rightOffhandEnabled;
            if (hasBottomRow)
            {
                // 底层高度取快捷栏和副手槽的最大值
                double hotbarHeight = _originalHotbarHeight * _scaleFactor;
                double offhandHeight = _originalOffhandHeight * _scaleFactor;
                height += Math.Max(hotbarHeight, offhandHeight);
            }

            // 经验条：底层上方1px（如果有底层），否则在最底部
            if (hasBottomRow)
            {
                height += _spacing * _scaleFactor;
            }
            height += _originalExpBarHeight * _scaleFactor;

            // 生命值/饥饿值：经验条上方1px
            height += _heartSpacing * _scaleFactor + _originalHeartHeight * _scaleFactor;

            // 伤害吸收值/空气值：生命值/饥饿值上方1px
            int absorbingRows = GetMaxAbsorbingRows();
            int extraAbsorbingRows = Math.Max(0, absorbingRows - 1);
            double absorbingHeight = _originalAbsorbingFullHeight * _scaleFactor;
            double rowSpacing = _absorbingRowSpacing * _scaleFactor;
            double absorbingToHeartSpacing = _absorbingToHeartSpacing * _scaleFactor;
            double airHeight = _originalAirHeight * _scaleFactor;
            double airSpacing = _airSpacing * _scaleFactor;
            double absorbingExtent = extraAbsorbingRows * (absorbingHeight + rowSpacing) + absorbingHeight + absorbingToHeartSpacing;
            double airExtent = airHeight + airSpacing;
            height += Math.Max(absorbingExtent, airExtent);

            Height = height;
        }

        /// <summary>
        /// 获取经验条的Y位置（从窗口顶部往下计算）
        /// </summary>
        private double GetExpBarTopOffset()
        {
            double heartY = GetHeartY();
            double heartHeight = _originalHeartHeight * _scaleFactor;
            double heartSpacing = _heartSpacing * _scaleFactor;
            return heartY + heartHeight + heartSpacing;
        }

        /// <summary>
        /// 获取心形/饥饿值的Y位置（从窗口顶部往下计算）
        /// </summary>
        private double GetHeartY()
        {
            int absorbingRows = GetMaxAbsorbingRows();
            int extraRows = Math.Max(0, absorbingRows - 1);
            double absorbingHeight = _originalAbsorbingFullHeight * _scaleFactor;
            double rowSpacing = _absorbingRowSpacing * _scaleFactor;
            double absorbingToHeartSpacing = _absorbingToHeartSpacing * _scaleFactor;
            double airHeight = _originalAirHeight * _scaleFactor;
            double airSpacing = _airSpacing * _scaleFactor;

            double airExtent = airHeight + airSpacing;
            double absorbingExtent = absorbingHeight + extraRows * (absorbingHeight + rowSpacing) + absorbingToHeartSpacing;

            return Math.Max(airExtent, absorbingExtent);
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
        /// 定位窗口到屏幕底部，快捷栏水平居中
        /// 窗口尺寸固定，包含所有副手槽空间（无论是否显示）
        /// </summary>
        private void PositionWindow()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // 快捷栏始终居中于屏幕
            double hotbarCenterX = screenWidth / 2;
            double hotbarWidth = _originalHotbarWidth * _scaleFactor;
            double hotbarScreenLeft = hotbarCenterX - hotbarWidth / 2;

            // 快捷栏在窗口内的左边位置（固定）
            double hotbarWindowLeft = GetHotbarLeft();

            // 窗口左边位置 = 快捷栏屏幕位置 - 快捷栏窗口内位置
            Left = hotbarScreenLeft - hotbarWindowLeft;

            // 窗口顶部位置
            double desiredTop = screenHeight - Height - 10 * _scaleFactor;
            Top = Math.Max(0, desiredTop);
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
    }
}