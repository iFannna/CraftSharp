using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CraftSharp.Services.Core;
using CraftSharp.Services.Hud;
using CraftSharp.Services.Slot;
using CraftSharp.Services.Resource;
using CraftSharp.Helpers;
using CraftSharp.Models;
using CraftSharp.Windows.Dialogs;
using System.Text.Json;

namespace CraftSharp.Windows.Inventory
{
    public partial class InventoryWindow : Window
    {
        private readonly SlotDataService _slotService = SlotDataService.Instance;
        private readonly AppSettings? _settings;

        private double _scaleFactor;
        private double _originalImageWidth = 176;
        private double _originalImageHeight = 166;

        // 当前样式文件名（如 inventory.png）
        private string _currentStyle = "inventory.png";

        // 是否共享数据（从配置读取）
        private bool _sharedData = true;

        // 格子坐标数据
        private List<SlotCoord>? _slotCoords;

        // 格子控件字典（Key: slotId, Value: Border）
        private Dictionary<string, Border> _slotBorders = new();
        private Dictionary<string, System.Windows.Controls.Image> _slotIcons = new();
        private Dictionary<string, Border> _slotHoverOverlays = new(); // hover 白色蒙版

        // 悬浮效果配置（从设置读取）
        private bool _hoverEffect = true;

        // Tooltip 配置（从设置读取）
        private bool _showTooltip = true;

        // hover 长按计时器（300ms后切换为绿色蒙版）
        private DispatcherTimer? _hoverTimer;
        private string? _currentHoverSlotId = null;

        // hover 蒙版颜色
        private static readonly System.Windows.Media.Color WhiteOverlayColor = System.Windows.Media.Color.FromArgb(128, 255, 255, 255); // 50%不透明度白色
        private static readonly System.Windows.Media.Color GreenOverlayColor = System.Windows.Media.Color.FromArgb(112, 75, 255, 84); // 44%不透明度

        // 服务实例
        private SlotIconService? _iconService;
        private SlotDragService? _dragService;

        // 长按开始时的鼠标位置
        private System.Windows.Point _longPressStartPoint;

        // 原生拖放目标（支持 Windows 拖拽缩略图）
        private IDisposable? _nativeDropTarget;

        // 灰色蒙版窗口
        private GrayOverlayWindow? _grayOverlayWindow;

        // 状态栏隐藏前的可见性状态
        private bool _statusBarWasVisible = false;

        // 点击模式（"single"单击/"double"双击）
        private string _clickMode = "single";

        // Tooltip 窗口
        private InventoryTooltipWindow? _tooltipWindow;

        // 玩家预览控件
        private PlayerPreviewControl? _playerPreviewControl;

        // 双击检测：上次点击的格子ID和时间
        private string? _lastClickedSlotId = null;
        private DateTime _lastClickTime = DateTime.MinValue;
        private const int DoubleClickThresholdMs = 500;

        // 窗口锁定和拖动状态
        private bool _isLocked = false;
        private bool _isDragging = false;
        private double _dragOffsetX = 0;
        private double _dragOffsetY = 0;
        private bool _skipDefaultPositioning = false;

        // Shift 模式批量移动状态
        private bool _isShiftMoveMode = false;
        private string? _lastShiftMoveSlotId = null;

        // Q 键丢弃相关状态
        private bool _isQKeyDown = false;
        private string? _lastDroppedSlotId = null;

        // 中键分发模式状态
        private bool _isDistributeMode = false;
        private string? _distributeSourceSlotId = null;
        private string? _distributeFilePath = null;
        private string? _lastDistributeSlotId = null;

        // 静态属性：跳过默认定位（必须在构造函数之前设置）
        public static bool ShouldSkipDefaultPositioning { get; set; } = false;

        // 位置变化事件（用于通知外部即时保存）
        public event EventHandler? PositionChanged;

        // 格子坐标数据结构
        public class SlotCoord
        {
            public string slot_id { get; set; } = "";
            public int x { get; set; }
            public int y { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

        // Shift 模式格子类型枚举
        private enum SlotCategory
        {
            Hotbar,      // 快捷栏 hotbar_0 ~ hotbar_8
            Inventory,   // 物品栏 inventory_0 ~ inventory_26
            Armor,       // 护甲 helmet, chestplate, leggings, boots
            Craft,       // 合成 craft_0 ~ craft_3, craft_result
            Offhand,     // 副手 offhand
            Other        // 其他（如 player，不参与移动）
        }

        public InventoryWindow(AppSettings? settings = null)
        {
            // 从静态属性读取是否跳过默认定位
            _skipDefaultPositioning = ShouldSkipDefaultPositioning;
            // 重置静态属性（避免影响下次创建）
            ShouldSkipDefaultPositioning = false;

            InitializeComponent();

            _settings = settings;

            // 读取锁定状态
            _isLocked = _settings?.Inventory.Locked ?? false;

            // 注册原生拖放
            SourceInitialized += (_, _) =>
            {
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

            Closed += (_, _) =>
            {
                _nativeDropTarget?.Dispose();
                _nativeDropTarget = null;
            };

            // 监听窗口位置变化（用于即时保存位置）
            LocationChanged += OnLocationChanged;

            // 使用 SlotDataService 单例（已在字段声明中初始化）

            // 初始化缩放服务
            ScaleService.Instance.Initialize();
            _scaleFactor = ScaleService.Instance.ScaleFactor;

            // 读取当前样式配置
            _currentStyle = _settings?.Inventory.StylePath ?? "inventory.png";
            _sharedData = _settings?.Inventory.SharedData ?? true;
            _hoverEffect = _settings?.Inventory.HoverEffect ?? true;
            _showTooltip = _settings?.Inventory.ShowTooltip ?? true;

            // 加载格子坐标数据
            LoadSlotCoords();

            // 加载背景图片（使用当前样式）
            LoadStyleImage();

            // 设置窗口尺寸
            SetWindowSize();

            // 动态创建和设置格子
            SetupSlots();

            // 初始化服务
            InitializeServices();

            // 加载已保存的格子数据
            LoadSlots();

            PositionWindow();
        }

        /// <summary>
        /// 初始化服务实例
        /// </summary>
        private void InitializeServices()
        {
            // 使用 SlotFileValidator 单例
            var fileValidator = SlotFileValidator.Instance;
            _iconService = new SlotIconService(fileValidator, _settings, _scaleFactor);
            _dragService = new SlotDragService(_slotService);

            // 读取点击模式配置
            _clickMode = _settings?.Inventory.ClickMode ?? "single";

            // 设置格子ID映射
            _dragService.SlotIdMapper = GetSlotIdFromIndex;

            // 设置共享数据配置和当前样式
            _dragService.SharedData = _sharedData;
            _dragService.CurrentStyle = _currentStyle;

            // 订阅事件
            _iconService.IconNeedsUpdate += OnIconNeedsUpdate;
            _dragService.DragStarted += OnDragStarted;
            _dragService.DragEnded += OnDragEnded;
            _dragService.SwapCompleted += OnSwapCompleted;

            // 订阅剪切状态变化事件
            SlotClipboardService.Instance.CutStateChanged += OnCutStateChanged;
        }

        /// <summary>
        /// 索引转 SlotId
        /// </summary>
        private string GetSlotIdFromIndex(int index)
        {
            if (_slotCoords == null || index < 0 || index >= _slotCoords.Count) return "";
            return _slotCoords[index].slot_id;
        }

        /// <summary>
        /// SlotId 转 索引
        /// </summary>
        private int GetIndexFromSlotId(string slotId)
        {
            if (_slotCoords == null) return -1;
            for (int i = 0; i < _slotCoords.Count; i++)
            {
                if (_slotCoords[i].slot_id == slotId)
                    return i;
            }
            return -1;
        }

        #region 服务事件处理

        /// <summary>
        /// 图标需要更新事件处理（文件丢失或恢复时）
        /// </summary>
        private void OnIconNeedsUpdate(object? sender, SlotIconService.IconUpdateEventArgs e)
        {
            if (e.IsPlaceholder)
            {
                UpdateSlotsToPlaceholder(e.FilePath);
            }
            else
            {
                UpdateSlotsToNormal(e.FilePath);
            }
        }

        /// <summary>
        /// 拖动开始事件处理
        /// </summary>
        private void OnDragStarted(object? sender, SlotDragService.DragStartedEventArgs e)
        {
            var slotId = GetSlotIdFromIndex(e.SourceSlotIndex);
            if (!_slotIcons.TryGetValue(slotId, out var sourceIcon)) return;

            // 隐藏源格子图标
            sourceIcon.Visibility = Visibility.Collapsed;

            // 检查文件是否丢失
            var item = _slotService.GetSlot(slotId);
            bool isMissing = !item.IsEmpty && SlotFileValidator.Instance.IsMissing(item.FilePath);

            // 显示拖动图标副本
            if (isMissing)
            {
                // 文件丢失：使用占位图
                var placeholder = _iconService?.LoadPlaceholderIcon();
                if (placeholder != null)
                {
                    DragIconImage.Source = placeholder;
                    DragIconImage.Width = 16 * _scaleFactor;
                    DragIconImage.Height = 16 * _scaleFactor;
                    DragIconImage.Visibility = Visibility.Visible;
                    RenderOptions.SetBitmapScalingMode(DragIconImage, BitmapScalingMode.NearestNeighbor);

                    var mousePos = Mouse.GetPosition(this);
                    Canvas.SetLeft(DragIconImage, mousePos.X - DragIconImage.Width / 2);
                    Canvas.SetTop(DragIconImage, mousePos.Y - DragIconImage.Height / 2);
                }
            }
            else if (sourceIcon.Source != null)
            {
                // 文件正常：使用格子图标
                DragIconImage.Source = sourceIcon.Source;
                DragIconImage.Width = 16 * _scaleFactor;
                DragIconImage.Height = 16 * _scaleFactor;
                DragIconImage.Visibility = Visibility.Visible;
                RenderOptions.SetBitmapScalingMode(DragIconImage, RenderOptions.GetBitmapScalingMode(sourceIcon));

                var mousePos = Mouse.GetPosition(this);
                Canvas.SetLeft(DragIconImage, mousePos.X - DragIconImage.Width / 2);
                Canvas.SetTop(DragIconImage, mousePos.Y - DragIconImage.Height / 2);
            }
            else
            {
                DragIconImage.Visibility = Visibility.Collapsed;
            }

            // 捕获鼠标，确保拖动过程中事件不丢失
            CaptureMouse();
        }

        /// <summary>
        /// 拖动结束事件处理
        /// </summary>
        private void OnDragEnded(object? sender, SlotDragService.DragEndedEventArgs e)
        {
            DragIconImage.Visibility = Visibility.Collapsed;

            // 释放鼠标捕获
            ReleaseMouseCapture();

            if (e.ShouldRestore)
            {
                var slotId = GetSlotIdFromIndex(e.SourceSlotIndex);
                if (_slotIcons.TryGetValue(slotId, out var sourceIcon))
                {
                    var item = _slotService.GetSlot(slotId);
                    if (!item.IsEmpty)
                    {
                        sourceIcon.Visibility = Visibility.Visible;
                    }
                }
            }

            // 没有发生交换时，延迟触发hover效果（图标恢复已完成）
            // 有交换时，hover效果会在OnSwapCompleted中触发
            if (!e.HasSwap)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    TriggerHoverAtMousePosition();
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }

        /// <summary>
        /// 在鼠标当前位置手动触发 hover 效果
        /// </summary>
        private void TriggerHoverAtMousePosition()
        {
            if (!_hoverEffect) return;

            var mousePos = Mouse.GetPosition(SlotCanvas);
            var targetSlotId = GetSlotIdAtPosition(mousePos);

            if (targetSlotId == null) return;

            // 检查格子是否有文件
            bool hasFile = _slotIcons.TryGetValue(targetSlotId, out var icon) && icon.Visibility == Visibility.Visible;

            if (_slotHoverOverlays.TryGetValue(targetSlotId, out var hoverOverlay))
            {
                // 显示白色蒙版
                hoverOverlay.Background = new SolidColorBrush(WhiteOverlayColor);
                hoverOverlay.Visibility = Visibility.Visible;

                if (hasFile)
                {
                    // 有文件：启动计时器，之后切换为绿色蒙版
                    _currentHoverSlotId = targetSlotId;
                    if (_hoverTimer == null)
                    {
                        _hoverTimer = new DispatcherTimer();
                        _hoverTimer.Interval = TimeSpan.FromMilliseconds(250);
                        _hoverTimer.Tick += HoverTimer_Tick;
                    }
                    _hoverTimer.Stop();
                    _hoverTimer.Start();

                    // 显示 Tooltip
                    ShowTooltip(targetSlotId);
                }
                else
                {
                    // 空格子：不启动计时器，保持白色蒙版
                    _hoverTimer?.Stop();
                    _currentHoverSlotId = null;
                }
            }
        }

        /// <summary>
        /// 格子交换完成事件处理
        /// 直接交换图标Source和渲染模式，不重新加载（参考快捷栏实现）
        /// </summary>
        private void OnSwapCompleted(object? sender, SlotDragService.SwapCompletedEventArgs e)
        {
            var sourceSlotId = GetSlotIdFromIndex(e.SourceSlotIndex);
            var targetSlotId = GetSlotIdFromIndex(e.TargetSlotIndex);

            if (sourceSlotId == "" || targetSlotId == "") return;

            // 获取缓存的图标Source和渲染模式
            ImageSource? sourceIconSource = null;
            ImageSource? targetIconSource = null;
            BitmapScalingMode sourceRenderMode = BitmapScalingMode.HighQuality;
            BitmapScalingMode targetRenderMode = BitmapScalingMode.HighQuality;

            if (_slotIcons.TryGetValue(sourceSlotId, out var sourceIcon))
            {
                sourceIconSource = sourceIcon.Source;
                sourceRenderMode = RenderOptions.GetBitmapScalingMode(sourceIcon);
            }

            if (_slotIcons.TryGetValue(targetSlotId, out var targetIcon))
            {
                targetIconSource = targetIcon.Source;
                targetRenderMode = RenderOptions.GetBitmapScalingMode(targetIcon);
            }

            // 交换图标显示（使用缓存，不重新加载）
            if (_slotIcons.TryGetValue(sourceSlotId, out var sourceIconImg))
            {
                if (!e.TargetItem.IsEmpty && targetIconSource != null)
                {
                    sourceIconImg.Source = targetIconSource;
                    sourceIconImg.Visibility = Visibility.Visible;
                    RenderOptions.SetBitmapScalingMode(sourceIconImg, targetRenderMode);
                }
                else
                {
                    sourceIconImg.Source = null;
                    sourceIconImg.Visibility = Visibility.Collapsed;
                }
            }

            if (_slotIcons.TryGetValue(targetSlotId, out var targetIconImg))
            {
                if (!e.SourceItem.IsEmpty && sourceIconSource != null)
                {
                    targetIconImg.Source = sourceIconSource;
                    targetIconImg.Visibility = Visibility.Visible;
                    RenderOptions.SetBitmapScalingMode(targetIconImg, sourceRenderMode);
                }
                else
                {
                    targetIconImg.Source = null;
                    targetIconImg.Visibility = Visibility.Collapsed;
                }
            }

            // 如果涉及 hotbar 格子，通知 StatusBarService 刷新
            if (sourceSlotId.StartsWith("hotbar_") || targetSlotId.StartsWith("hotbar_"))
            {
                StatusBarService.Instance.RefreshHotbarIcons();
            }

            // 交换完成后，延迟触发目标格子的 hover 效果
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TriggerHoverAtMousePosition();
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        /// <summary>
        /// 剪切状态变化事件处理（null=恢复透明度，非null=半透明化）
        /// </summary>
        private void OnCutStateChanged(object? sender, string? cutSlotId)
        {
            if (cutSlotId != null)
            {
                // 对被剪切的格子图标应用半透明效果
                if (_slotIcons.TryGetValue(cutSlotId, out var icon))
                {
                    icon.Opacity = 0.5;
                }
            }
            else
            {
                // 恢复所有格子图标透明度
                foreach (var kvp in _slotIcons)
                {
                    kvp.Value.Opacity = 1.0;
                }
            }
        }
        /// <summary>
        /// 更新指定文件路径的所有格子为占位图
        /// 检查当前格子实际使用的数据源
        /// </summary>
        private void UpdateSlotsToPlaceholder(string filePath)
        {
            var placeholder = _iconService?.LoadPlaceholderIcon();
            if (placeholder == null) return;

            foreach (var kvp in _slotBorders)
            {
                var slotId = kvp.Key;
                // 根据 SharedData 配置获取格子数据（统一逻辑）
                var displayItem = _slotService.GetSlot(slotId, _currentStyle, _sharedData);

                // 只检查当前格子实际使用的数据源
                bool displayHasPath = !displayItem.IsEmpty && displayItem.FilePath == filePath;

                if (displayHasPath)
                {
                    var icon = _slotIcons[slotId];
                    if (icon != null)
                    {
                        icon.Source = placeholder;
                        icon.Visibility = Visibility.Visible;
                        RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.NearestNeighbor);
                    }
                }
            }
        }

        /// <summary>
        /// 更新指定文件路径的所有格子为正常图标
        /// 检查当前格子实际使用的数据源
        /// </summary>
        private void UpdateSlotsToNormal(string filePath)
        {
            foreach (var kvp in _slotBorders)
            {
                var slotId = kvp.Key;
                // 根据 SharedData 配置获取格子数据（统一逻辑）
                var displayItem = _slotService.GetSlot(slotId, _currentStyle, _sharedData);

                // 只检查当前格子实际使用的数据源
                bool displayHasPath = !displayItem.IsEmpty && displayItem.FilePath == filePath;

                if (displayHasPath)
                {
                    SetSlotIcon(slotId, displayItem.FilePath);
                }
            }
        }

        #endregion

        /// <summary>
        /// 加载格子坐标数据（从 Assets 目录读取，根据当前样式动态加载）
        /// </summary>
        private void LoadSlotCoords()
        {
            try
            {
                // 根据样式文件名推导坐标文件名：inventory.png → inventory.json
                string coordFileName = System.IO.Path.GetFileNameWithoutExtension(_currentStyle) + ".json";
                var coordsPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    $"assets/minecraft/textures/gui/container/coordinate/{coordFileName}");

                if (File.Exists(coordsPath))
                {
                    var json = File.ReadAllText(coordsPath);
                    _slotCoords = JsonSerializer.Deserialize<List<SlotCoord>>(json);
                }
                else
                {
                    // 回退到默认坐标文件
                    var defaultCoordsPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "assets/minecraft/textures/gui/container/coordinate/inventory.json");
                    if (File.Exists(defaultCoordsPath))
                    {
                        var json = File.ReadAllText(defaultCoordsPath);
                        _slotCoords = JsonSerializer.Deserialize<List<SlotCoord>>(json);
                    }
                    else
                    {
                        _slotCoords = new List<SlotCoord>();
                    }
                }
            }
            catch
            {
                _slotCoords = new List<SlotCoord>();
            }
        }

        /// <summary>
        /// 加载位图图片（使用 ImageService）
        /// </summary>
        protected static BitmapImage LoadBitmapImage(string relativePath)
        {
            return ImageService.Instance.LoadBitmapImage(relativePath)!;
        }

        /// <summary>
        /// 设置窗口尺寸
        /// </summary>
        private void SetWindowSize()
        {
            Width = _originalImageWidth * _scaleFactor;
            Height = _originalImageHeight * _scaleFactor;
        }

        /// <summary>
        /// 动态创建和设置格子位置和大小
        /// 支持任意尺寸格子（如 16x16、24x24）
        /// </summary>
        private void SetupSlots()
        {
            if (_slotCoords == null) return;

            // 清空现有格子控件和字典
            SlotCanvas.Children.Clear();
            _slotBorders.Clear();
            _slotIcons.Clear();
            _slotHoverOverlays.Clear();

            // 清理旧的玩家预览控件
            if (_playerPreviewControl != null)
            {
                RootGrid.Children.Remove(_playerPreviewControl);
                _playerPreviewControl = null;
            }

            foreach (var coord in _slotCoords)
            {
                var slotId = coord.slot_id;

                // player 格子作为玩家预览区域，不创建格子控件
                if (slotId == "player")
                {
                    SetupPlayerPreview(coord);
                    continue;
                }

                // 根据配置尺寸缩放
                double slotWidth = coord.width * _scaleFactor;
                double slotHeight = coord.height * _scaleFactor;

                // 创建容器 Grid（用于叠加图标和 hover 蒙版）
                var grid = new Grid
                {
                    Name = $"SlotGrid_{slotId}",
                    Width = slotWidth,
                    Height = slotHeight
                };

                var border = new Border
                {
                    Name = $"Slot_{slotId}",
                    Background = System.Windows.Media.Brushes.Transparent,
                    Width = slotWidth,
                    Height = slotHeight
                };

                border.MouseLeftButtonDown += Slot_MouseLeftButtonDown;
                border.AddHandler(Mouse.MouseDownEvent, new MouseButtonEventHandler(Slot_MouseMiddleButtonDown));
                border.MouseRightButtonDown += Slot_MouseRightButtonDown;

                var icon = new System.Windows.Controls.Image
                {
                    Name = $"Icon_{slotId}",
                    Stretch = Stretch.Uniform,
                    Width = slotWidth,
                    Height = slotHeight,
                    Visibility = Visibility.Collapsed
                };
                RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);

                // 创建 hover 白色蒙版（50% 不透明度）
                var hoverOverlay = new Border
                {
                    Name = $"HoverOverlay_{slotId}",
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 255, 255, 255)),
                    Width = slotWidth,
                    Height = slotHeight,
                    Visibility = Visibility.Collapsed
                };

                grid.Children.Add(icon);
                grid.Children.Add(hoverOverlay);
                border.Child = grid;

                // 添加 hover 事件
                border.MouseEnter += Slot_MouseEnter;
                border.MouseLeave += Slot_MouseLeave;

                Canvas.SetLeft(border, coord.x * _scaleFactor);
                Canvas.SetTop(border, coord.y * _scaleFactor);

                SlotCanvas.Children.Add(border);

                _slotBorders[slotId] = border;
                _slotIcons[slotId] = icon;
                _slotHoverOverlays[slotId] = hoverOverlay;
            }
        }

        /// <summary>
        /// 设置玩家预览控件
        /// </summary>
        private void SetupPlayerPreview(SlotCoord coord)
        {
            double previewWidth = coord.width * _scaleFactor;
            double previewHeight = coord.height * _scaleFactor;
            double previewX = coord.x * _scaleFactor;
            double previewY = coord.y * _scaleFactor;

            _playerPreviewControl = new PlayerPreviewControl
            {
                Width = previewWidth,
                Height = previewHeight,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(previewX, previewY, 0, 0)
            };

            RootGrid.Children.Add(_playerPreviewControl);

            // 在 SourceInitialized 后更新预览位置（确保窗口位置已确定）
            SourceInitialized += (_, _) => UpdatePlayerPreviewPosition();
            LocationChanged += (_, _) => UpdatePlayerPreviewPosition();
        }

        /// <summary>
        /// 更新玩家预览控件的位置信息
        /// </summary>
        private void UpdatePlayerPreviewPosition()
        {
            if (_playerPreviewControl == null) return;

            // 获取预览控件在屏幕上的位置
            var previewPosition = _playerPreviewControl.PointToScreen(new Point(0, 0));
            _playerPreviewControl.UpdatePreviewPosition(previewPosition, _playerPreviewControl.ActualWidth, _playerPreviewControl.ActualHeight);
        }

        /// <summary>
        /// 定位窗口到屏幕居中（如果未跳过默认定位）
        /// </summary>
        private void PositionWindow()
        {
            // 如果跳过默认定位，则不执行默认居中（位置由 App.xaml.cs 恢复保存的位置）
            if (_skipDefaultPositioning) return;

            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            Left = (screenWidth - Width) / 2;
            Top = (screenHeight - Height) / 2;
        }

        /// <summary>
        /// 窗口位置变化事件处理
        /// </summary>
        private void OnLocationChanged(object? sender, EventArgs e)
        {
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 设置窗口锁定状态
        /// </summary>
        public void SetLocked(bool locked)
        {
            _isLocked = locked;
        }

        /// <summary>
        /// 设置跳过默认定位（实例方法，供外部调用）
        /// </summary>
        public void SetSkipDefaultPositioning(bool skip)
        {
            _skipDefaultPositioning = skip;
        }

        /// <summary>
        /// 窗口鼠标左键按下事件（用于窗口拖动）
        /// </summary>
        /// <summary>
        /// 键盘按下 - Q键丢弃鼠标悬浮的格子内容
        /// </summary>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Q)
            {
                // 首次按下：丢弃当前悬浮格子
                if (!_isQKeyDown)
                {
                    _isQKeyDown = true;
                    _lastDroppedSlotId = null;
                    DropHoverSlot();
                }
                // 长按重复触发（WPF PreviewKeyDown 默认重复）：丢弃当前悬浮格子
                else
                {
                    DropHoverSlot();
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// 键盘释放 - 重置Q键状态
        /// </summary>
        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Q)
            {
                _isQKeyDown = false;
                _lastDroppedSlotId = null;
                e.Handled = true;
            }
        }

        /// <summary>
        /// 丢弃当前鼠标悬浮的格子内容
        /// </summary>
        private void DropHoverSlot()
        {
            var mousePos = Mouse.GetPosition(SlotCanvas);
            var slotId = GetSlotIdAtPosition(mousePos);
            if (slotId == null) return;

            // 避免重复丢弃同一个格子（长按扫过场景）
            if (slotId == _lastDroppedSlotId) return;

            var item = _slotService.GetSlot(slotId, _currentStyle, _sharedData);
            if (item.IsEmpty) { _lastDroppedSlotId = slotId; return; }

            _slotService.ClearSlot(slotId, _currentStyle, _sharedData);
            ClearSlotIcon(slotId);
            _lastDroppedSlotId = slotId;

            // 如果涉及 hotbar 格子，通知 StatusBarService 刷新
            if (slotId.StartsWith("hotbar_"))
            {
                StatusBarService.Instance.RefreshHotbarIcons();
            }
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Shift 模式检测：从非格子区域开始
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                _isShiftMoveMode = true;
                _lastShiftMoveSlotId = null;
                CaptureMouse();
                return;
            }

            // 检查点击目标是否是格子（格子有自己的拖动逻辑）
            if (IsClickOnSlot(e.OriginalSource))
                return;

            // 未锁定时允许窗口拖动
            if (!_isLocked)
            {
                _isDragging = true;
                var mousePos = e.GetPosition(this);
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
                return border.Name.StartsWith("Slot_");
            }

            // 检查是否是格子内的 Image（图标）
            if (originalSource is System.Windows.Controls.Image image)
            {
                return image.Name.StartsWith("Icon_");
            }

            // 检查是否是 Canvas
            if (originalSource is Canvas canvas)
            {
                return canvas.Name == "SlotCanvas";
            }

            return false;
        }

        /// <summary>
        /// 加载已保存的格子数据
        /// </summary>
        private void LoadSlots()
        {
            // 程序启动时已执行全量检查，此处直接根据丢失状态显示图标

            foreach (var slotId in _slotBorders.Keys)
            {
                var item = _slotService.GetSlot(slotId, _currentStyle, _sharedData);
                if (!item.IsEmpty)
                {
                    SetSlotIcon(slotId, item.FilePath);
                }
                else
                {
                    // 格子为空时清除图标
                    ClearSlotIcon(slotId);
                }
            }
        }

        /// <summary>
        /// 设置格子图标
        /// </summary>
        private void SetSlotIcon(string slotId, string filePath)
        {
            if (!_slotIcons.TryGetValue(slotId, out var icon) || _iconService == null) return;

            var result = _iconService.GetIconWithRenderMode(filePath);
            if (result.IconSource != null)
            {
                icon.Source = result.IconSource;
                icon.Visibility = Visibility.Visible;
                RenderOptions.SetBitmapScalingMode(icon, result.RenderMode);
            }
        }

        /// <summary>
        /// 清除格子图标
        /// </summary>
        private void ClearSlotIcon(string slotId)
        {
            if (!_slotIcons.TryGetValue(slotId, out var icon)) return;
            icon.Source = null;
            icon.Visibility = Visibility.Collapsed;
        }

        // ==================== 鼠标事件处理 ====================

        // 窗口级别的鼠标事件，用于拖动过程中持续更新

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            // 中键分发模式处理（最高优先级）
            if (_isDistributeMode)
            {
                var dMousePos = e.GetPosition(this);
                Canvas.SetLeft(DragIconImage, dMousePos.X - DragIconImage.Width / 2);
                Canvas.SetTop(DragIconImage, dMousePos.Y - DragIconImage.Height / 2);

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var canvasPos = e.GetPosition(SlotCanvas);
                    var targetSlotId = GetSlotIdAtPosition(canvasPos);
                    if (targetSlotId != null && targetSlotId != _distributeSourceSlotId && targetSlotId != _lastDistributeSlotId)
                    {
                        var targetItem = _slotService.GetSlot(targetSlotId, _currentStyle, _sharedData);
                        if (targetItem.IsEmpty)
                        {
                            var newItem = new SlotItem { FilePath = _distributeFilePath!, DisplayName = "" };
                            _slotService.SetSlot(targetSlotId, newItem, _currentStyle, _sharedData);
                            SetSlotIcon(targetSlotId, _distributeFilePath!);
                            if (targetSlotId.StartsWith("hotbar_")) StatusBarService.Instance.RefreshHotbarIcons();
                        }
                        _lastDistributeSlotId = targetSlotId;
                    }
                }
                return;
            }

            // Shift 模式处理（最高优先级）
            if (_isShiftMoveMode && e.LeftButton == MouseButtonState.Pressed)
            {
                var canvasPos = e.GetPosition(SlotCanvas);
                var currentSlotId = GetSlotIdAtPosition(canvasPos);

                if (currentSlotId != null && currentSlotId != _lastShiftMoveSlotId)
                {
                    ExecuteShiftMove(currentSlotId);
                }
                return;
            }

            // 窗口拖动（使用增量方式，参考 StatusBarWindow）
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point currentMousePos = e.GetPosition(this);
                Left += currentMousePos.X - _dragOffsetX;
                Top += currentMousePos.Y - _dragOffsetY;
                return;
            }

            if (_dragService == null) return;

            if (_dragService.IsDragging)
            {
                // 更新拖动图标位置
                var mousePos = e.GetPosition(this);
                Canvas.SetLeft(DragIconImage, mousePos.X - DragIconImage.Width / 2);
                Canvas.SetTop(DragIconImage, mousePos.Y - DragIconImage.Height / 2);

                // 更新拖动目标
                var canvasPos = e.GetPosition(SlotCanvas);
                var targetSlotId = GetSlotIdAtPosition(canvasPos);
                var targetIndex = targetSlotId != null ? GetIndexFromSlotId(targetSlotId) : -1;
                _dragService.UpdateDragTarget(targetIndex);
            }
            else if (e.LeftButton == MouseButtonState.Pressed && _dragService.IsDragReady)
            {
                // 长按等待中，检测移动阈值
                var currentPoint = e.GetPosition(this);
                var distance = (currentPoint - _longPressStartPoint).Length;
                _dragService.HandleMouseMove(distance);
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            // Shift 模式结束
            if (_isShiftMoveMode)
            {
                _isShiftMoveMode = false;
                _lastShiftMoveSlotId = null;
                ReleaseMouseCapture();
                return;
            }

            // 中键分发模式结束
            if (_isDistributeMode)
            {
                _isDistributeMode = false;
                _distributeSourceSlotId = null;
                _distributeFilePath = null;
                _lastDistributeSlotId = null;
                DragIconImage.Visibility = Visibility.Collapsed;
                ReleaseMouseCapture();
                return;
            }

            // 窗口拖动结束
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();
                return;
            }

            if (_dragService != null && _dragService.IsDragging)
            {
                _dragService.EndDrag();
            }
            else if (_dragService != null && _dragService.IsTimerRunning)
            {
                // 长按定时器还在运行，取消长按检测
                _dragService.CancelLongPress();

                // 执行点击打开
                var mousePos = e.GetPosition(SlotCanvas);
                var slotId = GetSlotIdAtPosition(mousePos);
                if (slotId != null)
                {
                    HandleSlotClick(slotId);
                }
            }
        }

        /// <summary>
        /// 尝试打开文件/程序（不自动标记丢失，参考快捷栏 TryExecuteFile）
        /// </summary>
        private bool TryExecuteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            try
            {
                if (File.Exists(filePath) || Directory.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 处理格子点击（支持单击/双击模式）
        /// </summary>
        private void HandleSlotClick(string slotId)
        {
            // 点击前执行全量检查
            if (System.Windows.Application.Current is App app)
            {
                app.ValidateAllSlots();
            }

            // 根据 SharedData 配置获取格子数据
            var item = _slotService.GetSlot(slotId, _currentStyle, _sharedData);
            if (item.IsEmpty) return;

            bool isMissing = SlotFileValidator.Instance.IsMissing(item.FilePath);

            if (_clickMode == "single")
            {
                // 单击模式
                if (isMissing)
                {
                    // 丢失文件：显示确认对话框
                    HandleMissingFileSlot(slotId, item.FilePath);
                    return;
                }

                // 尝试打开文件（仅打开，不判断丢失）
                TryExecuteFile(item.FilePath);
            }
            else // double
            {
                // 双击模式：检测是否是双击
                var now = DateTime.Now;
                bool isDoubleClick = _lastClickedSlotId == slotId &&
                    (now - _lastClickTime).TotalMilliseconds < DoubleClickThresholdMs;

                if (isDoubleClick)
                {
                    // 双击
                    if (isMissing)
                    {
                        // 丢失文件：显示确认对话框
                        HandleMissingFileSlot(slotId, item.FilePath);
                        _lastClickedSlotId = null;
                        _lastClickTime = DateTime.MinValue;
                        return;
                    }

                    // 尝试打开文件（仅打开，不判断丢失）
                    TryExecuteFile(item.FilePath);
                    // 清除双击检测状态
                    _lastClickedSlotId = null;
                    _lastClickTime = DateTime.MinValue;
                }
                else
                {
                    // 记录第一次点击
                    _lastClickedSlotId = slotId;
                    _lastClickTime = now;
                }
            }
        }

        /// <summary>
        /// 处理文件丢失的格子点击
        /// </summary>
        private void HandleMissingFileSlot(string slotId, string filePath)
        {
            var confirmWindow = new SlotMissingConfirmWindow(filePath);
            confirmWindow.Owner = Window.GetWindow(this);
            confirmWindow.ShowDialog();

            if (confirmWindow.IsConfirmed)
            {
                if (_sharedData)
                {
                    // 共享数据模式：清除所有使用相同路径的格子（跨快捷栏+物品栏）
                    SlotFileValidator.Instance.ClearAllSlotsByPath(
                        (System.Windows.Application.Current as App)?.GetAppSettings(), filePath);
                }
                else
                {
                    // 独立数据模式：只清除当前样式的格子数据
                    var settings = (System.Windows.Application.Current as App)?.GetAppSettings();
                    if (settings != null)
                    {
                        // 从 StyleSlots 中清除使用该路径的格子
                        if (settings.StyleSlots.TryGetValue(_currentStyle, out var styleSlots))
                        {
                            var slotsToRemove = new List<string>();
                            foreach (var kvp in styleSlots)
                            {
                                if (!kvp.Value.IsEmpty && kvp.Value.FilePath == filePath)
                                {
                                    slotsToRemove.Add(kvp.Key);
                                }
                            }
                            foreach (var key in slotsToRemove)
                            {
                                styleSlots.Remove(key);
                            }
                        }
                        // 清除丢失标记
                        SlotFileValidator.Instance.UnmarkMissing(filePath);
                        // 保存配置
                        if (App.Current is App app)
                        {
                            app.SaveSettings();
                        }
                    }
                }

                // 刷新图标显示
                RefreshIcons();

                // 通知快捷栏刷新（如果快捷栏窗口存在）
                if (System.Windows.Application.Current is App app2)
                {
                    StatusBarService.Instance.RefreshHotbarIcons();
                }
            }
        }

        private void Slot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDistributeMode) { e.Handled = true; return; }
            // 拖拽前执行全量检查
            if (System.Windows.Application.Current is App app)
            {
                app.ValidateAllSlots();
            }

            var border = (Border)sender;
            var slotId = border.Name.Replace("Slot_", "");
            var slotIndex = GetIndexFromSlotId(slotId);

            if (slotIndex < 0 || _dragService == null) return;

            // Shift 模式检测：从格子开始
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                _isShiftMoveMode = true;
                _lastShiftMoveSlotId = null;
                CaptureMouse();
                ExecuteShiftMove(slotId);
                e.Handled = true;
                return;
            }

            _longPressStartPoint = e.GetPosition(this);
            _dragService.StartLongPressDetection(slotIndex);
            e.Handled = true;
        }

        private void Slot_MouseMiddleButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle) return;
            var border = (Border)sender;
            var slotId = border.Name.Replace("Slot_", "");
            var item = _slotService.GetSlot(slotId, _currentStyle, _sharedData);
            if (item.IsEmpty) return;

            _isDistributeMode = true;
            _distributeSourceSlotId = slotId;
            _distributeFilePath = item.FilePath;
            _lastDistributeSlotId = null;

            if (_slotIcons.TryGetValue(slotId, out var sourceIcon) && sourceIcon.Source != null)
            {
                DragIconImage.Source = sourceIcon.Source;
                DragIconImage.Width = 16 * _scaleFactor;
                DragIconImage.Height = 16 * _scaleFactor;
                DragIconImage.Visibility = Visibility.Visible;
                RenderOptions.SetBitmapScalingMode(DragIconImage, RenderOptions.GetBitmapScalingMode(sourceIcon));
                var mousePos = e.GetPosition(this);
                Canvas.SetLeft(DragIconImage, mousePos.X - DragIconImage.Width / 2);
                Canvas.SetTop(DragIconImage, mousePos.Y - DragIconImage.Height / 2);
            }

            CaptureMouse();
            e.Handled = true;
        }

        private void Slot_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Q键长按扫过丢弃
            if (_isQKeyDown)
            {
                var qBorder = (Border)sender;
                var qSlotId = qBorder.Name.Replace("Slot_", "");
                if (qSlotId != _lastDroppedSlotId)
                {
                    var qItem = _slotService.GetSlot(qSlotId, _currentStyle, _sharedData);
                    if (!qItem.IsEmpty)
                    {
                        _slotService.ClearSlot(qSlotId, _currentStyle, _sharedData);
                        ClearSlotIcon(qSlotId);
                        if (qSlotId.StartsWith("hotbar_")) StatusBarService.Instance.RefreshHotbarIcons();
                    }
                    _lastDroppedSlotId = qSlotId;
                }
            }

            if (!_hoverEffect) return;

            var border = (Border)sender;
            var slotId = border.Name.Replace("Slot_", "");

            // 检查格子是否有文件（通过图标Visibility判断）
            bool hasFile = _slotIcons.TryGetValue(slotId, out var icon) && icon.Visibility == Visibility.Visible;

            if (_slotHoverOverlays.TryGetValue(slotId, out var hoverOverlay))
            {
                // 显示白色蒙版
                hoverOverlay.Background = new SolidColorBrush(WhiteOverlayColor);
                hoverOverlay.Visibility = Visibility.Visible;

                if (hasFile)
                {
                    // 有文件：启动计时器，之后切换为绿色蒙版
                    _currentHoverSlotId = slotId;
                    if (_hoverTimer == null)
                    {
                        _hoverTimer = new DispatcherTimer();
                        _hoverTimer.Interval = TimeSpan.FromMilliseconds(250);
                        _hoverTimer.Tick += HoverTimer_Tick;
                    }
                    _hoverTimer.Stop();
                    _hoverTimer.Start();

                    // 显示 Tooltip（立即显示）
                    ShowTooltip(slotId);
                }
                else
                {
                    // 空格子：不启动计时器，保持白色蒙版
                    _hoverTimer?.Stop();
                    _currentHoverSlotId = null;
                }
            }
        }

        private void HoverTimer_Tick(object? sender, EventArgs e)
        {
            _hoverTimer?.Stop();

            // 检查当前hover格子是否还有文件且蒙版可见
            if (_currentHoverSlotId != null &&
                _slotHoverOverlays.TryGetValue(_currentHoverSlotId, out var hoverOverlay) &&
                hoverOverlay.Visibility == Visibility.Visible)
            {
                // 切换为绿色蒙版
                hoverOverlay.Background = new SolidColorBrush(GreenOverlayColor);
            }

            _currentHoverSlotId = null;
        }

        private void Slot_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = (Border)sender;
            var slotId = border.Name.Replace("Slot_", "");

            // 停止计时器
            _hoverTimer?.Stop();
            _currentHoverSlotId = null;

            if (_slotHoverOverlays.TryGetValue(slotId, out var hoverOverlay))
            {
                // 隐藏蒙版并恢复白色配置（为下次hover准备）
                hoverOverlay.Visibility = Visibility.Collapsed;
                hoverOverlay.Background = new SolidColorBrush(WhiteOverlayColor);
            }

            // 隐藏 Tooltip（立即关闭）
            HideTooltip();
        }

        /// <summary>
        /// 根据鼠标位置判断落在哪个格子
        /// </summary>
        private string? GetSlotIdAtPosition(System.Windows.Point mousePos)
        {
            foreach (var kvp in _slotBorders)
            {
                var border = kvp.Value;
                var left = Canvas.GetLeft(border);
                var top = Canvas.GetTop(border);
                var width = border.Width;
                var height = border.Height;

                if (mousePos.X >= left && mousePos.X < left + width &&
                    mousePos.Y >= top && mousePos.Y < top + height)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        // ==================== Shift 模式批量移动 ====================

        /// <summary>
        /// 根据格子ID判断格子类型
        /// </summary>
        private SlotCategory GetSlotCategory(string slotId)
        {
            if (slotId.StartsWith("hotbar_"))
                return SlotCategory.Hotbar;

            if (slotId.StartsWith("inventory_"))
                return SlotCategory.Inventory;

            if (slotId == "helmet" || slotId == "chestplate" ||
                slotId == "leggings" || slotId == "boots")
                return SlotCategory.Armor;

            if (slotId.StartsWith("craft_") || slotId == "craft_result")
                return SlotCategory.Craft;

            if (slotId == "offhand")
                return SlotCategory.Offhand;

            return SlotCategory.Other;
        }

        /// <summary>
        /// 在指定区域查找第一个空格子
        /// </summary>
        private string? FindFirstEmptySlot(SlotCategory category)
        {
            List<string> slotIds;

            switch (category)
            {
                case SlotCategory.Hotbar:
                    slotIds = new List<string>();
                    for (int i = 0; i <= 8; i++)
                        slotIds.Add($"hotbar_{i}");
                    break;

                case SlotCategory.Inventory:
                    slotIds = new List<string>();
                    for (int i = 0; i <= 26; i++)
                        slotIds.Add($"inventory_{i}");
                    break;

                default:
                    return null;
            }

            foreach (var slotId in slotIds)
            {
                var item = _slotService.GetSlot(slotId, _currentStyle, _sharedData);
                if (item.IsEmpty)
                    return slotId;
            }

            return null;
        }

        /// <summary>
        /// 执行 Shift 模式格子移动
        /// </summary>
        private void ExecuteShiftMove(string sourceSlotId)
        {
            _lastShiftMoveSlotId = sourceSlotId;

            var sourceItem = _slotService.GetSlot(sourceSlotId, _currentStyle, _sharedData);
            if (sourceItem.IsEmpty) return;

            SlotCategory sourceCategory = GetSlotCategory(sourceSlotId);
            if (sourceCategory == SlotCategory.Other) return;

            string? targetSlotId = null;

            switch (sourceCategory)
            {
                case SlotCategory.Hotbar:
                    targetSlotId = FindFirstEmptySlot(SlotCategory.Inventory);
                    break;

                case SlotCategory.Inventory:
                    targetSlotId = FindFirstEmptySlot(SlotCategory.Hotbar);
                    break;

                case SlotCategory.Armor:
                case SlotCategory.Craft:
                case SlotCategory.Offhand:
                    targetSlotId = FindFirstEmptySlot(SlotCategory.Inventory);
                    break;
            }

            if (targetSlotId == null) return;

            MoveSlotContent(sourceSlotId, targetSlotId, sourceItem);
        }

        /// <summary>
        /// 移动格子内容（源变空，目标获得内容）
        /// </summary>
        private void MoveSlotContent(string sourceSlotId, string targetSlotId, SlotItem sourceItem)
        {
            _slotService.ClearSlot(sourceSlotId, _currentStyle, _sharedData);
            _slotService.SetSlot(targetSlotId, sourceItem, _currentStyle, _sharedData);

            ClearSlotIcon(sourceSlotId);
            SetSlotIcon(targetSlotId, sourceItem.FilePath);

            if (sourceSlotId.StartsWith("hotbar_") || targetSlotId.StartsWith("hotbar_"))
            {
                StatusBarService.Instance.RefreshHotbarIcons();
            }
        }

        // ==================== 原生拖放处理 ====================

        /// <summary>
        /// 判断鼠标位置是否可以接受文件放置
        /// </summary>
        private bool CanDropAtPosition(System.Windows.Point screenPoint)
        {
            var mousePos = PointFromScreen(screenPoint);
            return GetSlotIdAtPosition(mousePos) != null;
        }

        /// <summary>
        /// 处理原生拖放回调
        /// </summary>
        private void HandleNativeDrop(IReadOnlyList<string> paths, System.Windows.Point screenPoint)
        {
            if (paths.Count == 0) return;

            var filePath = paths[0];
            var mousePos = PointFromScreen(screenPoint);
            var slotId = GetSlotIdAtPosition(mousePos);

            if (slotId != null)
            {
                _slotService.SetSlot(slotId, new SlotItem { FilePath = filePath }, _currentStyle, _sharedData);
                SetSlotIcon(slotId, filePath);

                if (slotId.StartsWith("hotbar_"))
                {
                    StatusBarService.Instance.RefreshHotbarIcons();
                }
            }
        }

        // ==================== 显示/隐藏 ====================

        /// <summary>
        /// 切换显示/隐藏
        /// </summary>
        public void Toggle()
        {
            if (IsVisible)
            {
                HideInventory();
            }
            else
            {
                ShowInventory();
            }
        }

        /// <summary>
        /// 设置点击模式（"single"单击/"double"双击）
        /// </summary>
        public void SetClickMode(string mode)
        {
            _clickMode = mode;
            // 清除双击检测状态
            _lastClickedSlotId = null;
            _lastClickTime = DateTime.MinValue;
        }

        /// <summary>
        /// 显示物品栏
        /// </summary>
        private void ShowInventory()
        {
            // 每次显示时重新读取点击模式配置（确保使用最新设置）
            _clickMode = _settings?.Inventory.ClickMode ?? "single";

            if (_settings?.Inventory.GrayOverlay ?? true)
            {
                int opacity = _settings?.Inventory.GrayOverlayOpacity ?? 75;
                _grayOverlayWindow = new GrayOverlayWindow(opacity);
                _grayOverlayWindow.Show();
            }

            PositionWindow();
            if (_grayOverlayWindow != null)
            {
                Owner = _grayOverlayWindow;
            }
            Show();

            if (_settings?.Inventory.HideStatusBar ?? false)
            {
                _statusBarWasVisible = StatusBarService.Instance.IsVisible();
                if (_statusBarWasVisible)
                {
                    StatusBarService.Instance.SetVisible(false);
                }
            }
        }

        /// <summary>
        /// 隐藏物品栏
        /// </summary>
        private void HideInventory()
        {
            // 隐藏 Tooltip
            HideTooltip();

            Hide();
            Owner = null;

            if (_grayOverlayWindow != null)
            {
                _grayOverlayWindow.Close();
                _grayOverlayWindow = null;
            }

            if ((_settings?.Inventory.HideStatusBar ?? false) && _statusBarWasVisible)
            {
                StatusBarService.Instance.SetVisible(true);
            }
        }

        /// <summary>
        /// 刷新所有格子图标
        /// </summary>
        public void RefreshIcons()
        {
            // 更新共享数据配置和当前样式
            if (_settings != null)
            {
                _sharedData = _settings.Inventory.SharedData;
                _currentStyle = _settings.Inventory.StylePath;
                // 更新拖动服务的配置
                if (_dragService != null)
                {
                    _dragService.SharedData = _sharedData;
                    _dragService.CurrentStyle = _currentStyle;
                }

                // 重新创建 _iconService 以使用最新的配置（特别是 ShowTargetIcon）
                if (_iconService != null)
                {
                    _iconService.IconNeedsUpdate -= OnIconNeedsUpdate;
                }
                var fileValidator = SlotFileValidator.Instance;
                _iconService = new SlotIconService(fileValidator, _settings, _scaleFactor);
                _iconService.IconNeedsUpdate += OnIconNeedsUpdate;
            }

            foreach (var slotId in _slotBorders.Keys)
            {
                var item = _slotService.GetSlot(slotId, _currentStyle, _sharedData);
                if (!item.IsEmpty)
                {
                    SetSlotIcon(slotId, item.FilePath);
                }
                else
                {
                    ClearSlotIcon(slotId);
                }
            }
        }

        /// <summary>
        /// 刷新悬浮效果配置
        /// </summary>
        public void RefreshHoverEffect()
        {
            _hoverEffect = _settings?.Inventory.HoverEffect ?? true;
        }

        /// <summary>
        /// 刷新 Tooltip 配置
        /// </summary>
        public void RefreshShowTooltip()
        {
            _showTooltip = _settings?.Inventory.ShowTooltip ?? true;
        }

        /// <summary>
        /// 加载当前样式的背景图片
        /// </summary>
        private void LoadStyleImage()
        {
            string stylePath = $"assets/minecraft/textures/gui/container/{_currentStyle}";
            var bitmap = LoadBitmapImage(stylePath);
            if (bitmap != null)
            {
                InventoryImage.Source = bitmap;
                _originalImageWidth = bitmap.PixelWidth;
                _originalImageHeight = bitmap.PixelHeight;
                SetWindowSize();
            }
            else
            {
                // 回退到默认样式
                InventoryImage.Source = LoadBitmapImage(AssetPaths.Inventory);
                _originalImageWidth = 176;
                _originalImageHeight = 166;
                SetWindowSize();
            }
        }

        /// <summary>
        /// 刷新物品栏样式（即时生效）
        /// 流程：更新样式 → 加载背景 → 加载坐标 → 重建格子 → 刷新图标
        /// </summary>
        public void RefreshStyle(string stylePath)
        {
            _currentStyle = stylePath;
            _sharedData = _settings?.Inventory.SharedData ?? true;
            LoadStyleImage();
            LoadSlotCoords();
            SetupSlots();
            LoadSlots();
            PositionWindow();
        }

        /// <summary>
        /// 刷新玩家预览模型（上传新皮肤后调用）
        /// </summary>
        public void RefreshPlayerModel()
        {
            if (_playerPreviewControl != null)
            {
                _playerPreviewControl.RefreshModel();
            }
        }

        /// <summary>
        /// 加载指定的皮肤文件
        /// </summary>
        public void LoadPlayerSkin(string skinPath, bool isWide)
        {
            if (_playerPreviewControl != null)
            {
                _playerPreviewControl.LoadSkin(skinPath, isWide);
            }
        }

        // ==================== Tooltip 相关 ====================

        /// <summary>
        /// 显示 Tooltip
        /// </summary>
        private void ShowTooltip(string slotId)
        {
            // 检查 Tooltip 功能是否启用
            if (!_showTooltip) return;

            // 根据 SharedData 配置获取格子数据
            var item = _slotService.GetSlot(slotId, _currentStyle, _sharedData);
            if (item.IsEmpty) return;

            bool isMissing = SlotFileValidator.Instance.IsMissing(item.FilePath);

            // 创建或更新 Tooltip 窗口
            if (_tooltipWindow == null)
            {
                _tooltipWindow = new InventoryTooltipWindow(_scaleFactor);
            }

            // 获取文件名颜色配置
            string fileNameColor = _settings?.Inventory.FileNameColor ?? "#FCFCFC";

            // 获取显示开关配置
            bool showFileName = _settings?.Inventory.TooltipShowFileName ?? true;
            bool showOriginalName = _settings?.Inventory.TooltipShowOriginalName ?? false;
            bool showFilePath = _settings?.Inventory.TooltipShowFilePath ?? false;
            bool showFileType = _settings?.Inventory.TooltipShowFileType ?? false;

            // 设置 Tooltip 内容
            _tooltipWindow.SetContent(item.FilePath, isMissing, fileNameColor,
                showFileName, showOriginalName, showFilePath, showFileType);

            // 获取格子控件位置和尺寸
            if (_slotBorders.TryGetValue(slotId, out var border))
            {
                // 获取格子在窗口中的位置
                double cellLeft = Canvas.GetLeft(border);
                double cellTop = Canvas.GetTop(border);
                double cellWidth = border.Width;
                double cellHeight = border.Height;

                // 获取窗口在屏幕中的位置
                double windowLeft = Left;
                double windowTop = Top;

                // 计算格子在屏幕中的位置
                double screenCellLeft = windowLeft + cellLeft;
                double screenCellTop = windowTop + cellTop;

                // 显示 Tooltip 在格子右侧，垂直居中
                _tooltipWindow.ShowAtCellPosition(screenCellLeft, screenCellTop, cellWidth, cellHeight);
            }
        }

        /// <summary>
        /// 隐藏 Tooltip
        /// </summary>
        private void HideTooltip()
        {
            if (_tooltipWindow != null)
            {
                _tooltipWindow.Hide();
            }
        }

        // ==================== 右键菜单 ====================

        /// <summary>
        /// 右键点击格子 - 显示右键菜单
        /// </summary>
        private void Slot_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDistributeMode) { e.Handled = true; return; }
            // 执行全量检查
            if (System.Windows.Application.Current is App app)
            {
                app.ValidateAllSlots();
            }

            var border = (Border)sender;
            var slotId = border.Name.Replace("Slot_", "");

            // 根据 SharedData 配置获取格子数据
            var item = _slotService.GetSlot(slotId, _currentStyle, _sharedData);
            bool isMissing = !item.IsEmpty && SlotFileValidator.Instance.IsMissing(item.FilePath);

            var menu = SlotContextMenuService.Instance.CreateSlotContextMenu(
                slotId,
                item,
                isMissing,
                _currentStyle,
                _sharedData,
                () => RefreshSingleSlotUI(slotId));

            menu.PlacementTarget = border;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;

            e.Handled = true;
        }

        /// <summary>
        /// 刷新单个格子UI（右键菜单操作后）
        /// </summary>
        private void RefreshSingleSlotUI(string slotId)
        {
            var item = _slotService.GetSlot(slotId, _currentStyle, _sharedData);
            if (item.IsEmpty)
            {
                ClearSlotIcon(slotId);
            }
            else
            {
                SetSlotIcon(slotId, item.FilePath);
            }
        }
    }
}




