using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CraftSharp.Helpers;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 状态栏窗口主文件 - 构造函数、缩放、布局核心逻辑
    /// </summary>
    public partial class StatusBarWindow : Window
    {
        // 基准分辨率：2560下放大6倍
        private const double BaseScreenWidth = 2560;
        private const double BaseScaleMultiplier = 6;

        private double _scaleFactor;
        private DispatcherTimer? _batteryTimer;
        private bool _isLocked = false;

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

            // 设置快捷栏位置
            SetupHotbar();

            // 设置副手槽位置
            SetupOffhandSlot();

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

            // 基于屏幕宽度与2560的比例，乘以基准倍数6
            _scaleFactor = (screenWidth / BaseScreenWidth) * BaseScaleMultiplier;
        }

        /// <summary>
        /// 设置窗口尺寸（从下往上布局：快捷栏底部，往上依次叠加）
        /// 布局顺序（从窗口顶部到底部）：
        /// 1. 伤害吸收值额外行（如果有超过1行）
        /// 2. 伤害吸收值第一行 + 空气值（同一行）
        /// 3. 心形/饥饿值
        /// 4. 经验条
        /// 5. 快捷栏
        /// </summary>
        private void SetWindowSize()
        {
            // 窗口宽度 = 副手槽宽度 + 间距 + 快捷栏宽度
            Width = (_originalOffhandWidth + _offhandSpacing + _originalHotbarWidth) * _scaleFactor;

            // 计算伤害吸收值需要的行数
            int absorbingRows = GetMaxAbsorbingRows();

            // 伤害吸收值和空气值不在同一行，各自有独立的间距
            int extraAbsorbingRows = Math.Max(0, absorbingRows - 1);

            // 伤害吸收值从顶部到心形的距离
            double absorbingExtent = extraAbsorbingRows * (_originalAbsorbingFullHeight + _absorbingRowSpacing) + _originalAbsorbingFullHeight + _absorbingToHeartSpacing;

            // 空气值从顶部到心形的距离（空气值独立一行）
            double airExtent = _originalAirHeight + _airSpacing;

            // 窗口顶部到心形的高度（取两者最大值）
            double topToHeartHeight = Math.Max(absorbingExtent, airExtent);

            // 窗口高度 = 顶部到心形高度 + 心形高度 + 心形与经验条间距 + 经验条高度 + 经验条与快捷栏间距 + 快捷栏高度
            Height = (topToHeartHeight + _originalHeartHeight + _heartSpacing + _originalExpBarHeight + _spacing + _originalHotbarHeight) * _scaleFactor;
        }

        /// <summary>
        /// 获取经验条的Y位置（从窗口顶部往下计算）
        /// 经验条Y = 心形Y + 心形高度 + 心形与经验条间距
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
        /// 心形Y由窗口顶部到心形的距离决定，取决于两条路径的最大值
        /// </summary>
        private double GetHeartY()
        {
            // 计算两条路径从心形往上延伸的距离
            int absorbingRows = GetMaxAbsorbingRows();
            int extraRows = Math.Max(0, absorbingRows - 1);
            double absorbingHeight = _originalAbsorbingFullHeight * _scaleFactor;
            double rowSpacing = _absorbingRowSpacing * _scaleFactor;
            double absorbingToHeartSpacing = _absorbingToHeartSpacing * _scaleFactor;
            double airHeight = _originalAirHeight * _scaleFactor;
            double airSpacing = _airSpacing * _scaleFactor;

            // 路径A：空气值往上延伸的距离（空气值高度 + 空气值与饥饿值间距）
            double airExtent = airHeight + airSpacing;

            // 路径B：伤害吸收值往上延伸的距离（总高度 + 与心形间距）
            double absorbingExtent = absorbingHeight + extraRows * (absorbingHeight + rowSpacing) + absorbingToHeartSpacing;

            // 心形Y位置 = 两条路径的最大值（确保窗口顶部Y=0能容纳更高的元素）
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
        /// </summary>
        private void PositionWindow()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // 快捷栏居中位置
            double hotbarCenterX = screenWidth / 2;
            double hotbarWidth = _originalHotbarWidth * _scaleFactor;
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double spacing = _offhandSpacing * _scaleFactor;

            // 窗口位置：根据副手槽位置调整，使快捷栏居中
            if (_offhandOnRight)
            {
                // 副手在右边：窗口起始位置 = 快捷栏居中起点
                Left = hotbarCenterX - hotbarWidth / 2;
            }
            else
            {
                // 副手在左边（默认）：窗口起始位置向左偏移副手槽宽度
                Left = hotbarCenterX - hotbarWidth / 2 - offhandWidth - spacing;
            }

            // 窗口顶部位置：正常情况下在屏幕底部，如果窗口高度超过屏幕则从顶部开始
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