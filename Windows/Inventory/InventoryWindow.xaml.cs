using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CraftSharp.Services;
using CraftSharp.Helpers;
using CraftSharp.Models;
using Newtonsoft.Json;

namespace CraftSharp.Windows.Inventory
{
    public partial class InventoryWindow : Window
    {
        private readonly SlotDataService _slotService;
        private readonly AppSettings? _settings;

        private double _scaleFactor;
        private double _originalImageWidth = 176;
        private double _originalImageHeight = 166;

        // 格子坐标数据
        private List<SlotCoord>? _slotCoords;

        // 格子控件字典（Key: slotId, Value: Border）
        private Dictionary<string, Border> _slotBorders = new();
        private Dictionary<string, System.Windows.Controls.Image> _slotIcons = new();

        // 服务实例
        private SlotFileValidator? _fileValidator;
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

        // 格子坐标数据结构
        public class SlotCoord
        {
            public string slot_id { get; set; } = "";
            public int x { get; set; }
            public int y { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

        public InventoryWindow(AppSettings? settings = null)
        {
            InitializeComponent();

            _settings = settings;

            // 注册原生拖放
            SourceInitialized += (s, e) =>
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

            Closed += (s, e) =>
            {
                _nativeDropTarget?.Dispose();
                _nativeDropTarget = null;
            };

            _slotService = new SlotDataService();

            // 初始化缩放服务
            ScaleService.Instance.Initialize();
            _scaleFactor = ScaleService.Instance.ScaleFactor;

            // 加载格子坐标数据
            LoadSlotCoords();

            // 加载背景图片
            InventoryImage.Source = LoadBitmapImage(AssetPaths.Inventory);

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
            _fileValidator = new SlotFileValidator();
            _iconService = new SlotIconService(_fileValidator, _settings, _scaleFactor);
            _dragService = new SlotDragService(_slotService);

            // 设置格子ID映射
            _dragService.SlotIdMapper = GetSlotIdFromIndex;

            // 订阅事件
            _iconService.IconNeedsUpdate += OnIconNeedsUpdate;
            _dragService.DragStarted += OnDragStarted;
            _dragService.DragEnded += OnDragEnded;
            _dragService.SwapCompleted += OnSwapCompleted;
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

            // 显示拖动图标副本
            if (sourceIcon.Source != null)
            {
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
        }

        /// <summary>
        /// 更新指定文件路径的所有格子为占位图
        /// </summary>
        private void UpdateSlotsToPlaceholder(string filePath)
        {
            var placeholder = _iconService?.LoadPlaceholderIcon();
            if (placeholder == null) return;

            foreach (var kvp in _slotBorders)
            {
                var slotId = kvp.Key;
                var item = _slotService.GetSlot(slotId);
                if (!item.IsEmpty && item.FilePath == filePath)
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
        /// </summary>
        private void UpdateSlotsToNormal(string filePath)
        {
            foreach (var kvp in _slotBorders)
            {
                var slotId = kvp.Key;
                var item = _slotService.GetSlot(slotId);
                if (!item.IsEmpty && item.FilePath == filePath)
                {
                    SetSlotIcon(slotId, item.FilePath);
                }
            }
        }

        #endregion

        /// <summary>
        /// 加载格子坐标数据（从 Assets 目录读取）
        /// </summary>
        private void LoadSlotCoords()
        {
            try
            {
                var coordsPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets/minecraft/textures/gui/container/coordinate/inventory.json");

                if (File.Exists(coordsPath))
                {
                    var json = File.ReadAllText(coordsPath);
                    _slotCoords = JsonConvert.DeserializeObject<List<SlotCoord>>(json);
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
        /// </summary>
        private void SetupSlots()
        {
            if (_slotCoords == null) return;

            double slotSize = 16 * _scaleFactor;

            foreach (var coord in _slotCoords)
            {
                // 只创建 16x16 的格子
                if (coord.width != 16 || coord.height != 16) continue;

                var slotId = coord.slot_id;

                var border = new Border
                {
                    Name = $"Slot_{slotId}",
                    Background = System.Windows.Media.Brushes.Transparent,
                    Width = slotSize,
                    Height = slotSize
                };

                border.MouseLeftButtonDown += Slot_MouseLeftButtonDown;

                var icon = new System.Windows.Controls.Image
                {
                    Name = $"Icon_{slotId}",
                    Stretch = Stretch.Uniform,
                    Width = slotSize,
                    Height = slotSize,
                    Visibility = Visibility.Collapsed
                };
                RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);

                border.Child = icon;

                Canvas.SetLeft(border, coord.x * _scaleFactor);
                Canvas.SetTop(border, coord.y * _scaleFactor);

                SlotCanvas.Children.Add(border);

                _slotBorders[slotId] = border;
                _slotIcons[slotId] = icon;
            }
        }

        /// <summary>
        /// 定位窗口到屏幕居中
        /// </summary>
        private void PositionWindow()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            Left = (screenWidth - Width) / 2;
            Top = (screenHeight - Height) / 2;
        }

        /// <summary>
        /// 加载已保存的格子数据
        /// </summary>
        private void LoadSlots()
        {
            // 清空丢失路径记录
            _fileValidator?.ClearMissingPaths();

            foreach (var slotId in _slotBorders.Keys)
            {
                var item = _slotService.GetSlot(slotId);
                if (!item.IsEmpty)
                {
                    // 检查文件有效性
                    if (_fileValidator != null && !_fileValidator.IsFilePathValid(item.FilePath))
                    {
                        _fileValidator.MarkMissing(item.FilePath);
                    }
                    SetSlotIcon(slotId, item.FilePath);
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
                    var item = _slotService.GetSlot(slotId);
                    if (!item.IsEmpty)
                    {
                        OpenFile(item.FilePath);
                    }
                }
            }
        }

        private void Slot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            var slotId = border.Name.Replace("Slot_", "");
            var slotIndex = GetIndexFromSlotId(slotId);

            if (slotIndex < 0 || _dragService == null) return;

            _longPressStartPoint = e.GetPosition(this);
            _dragService.StartLongPressDetection(slotIndex);
            e.Handled = true;
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
                _slotService.SetSlot(slotId, new SlotItem { FilePath = filePath });
                SetSlotIcon(slotId, filePath);

                if (slotId.StartsWith("hotbar_"))
                {
                    StatusBarService.Instance.RefreshHotbarIcons();
                }
            }
        }

        // ==================== 显示/隐藏 ====================

        /// <summary>
        /// 打开文件/程序
        /// 返回是否成功打开
        /// 后续检验通过执行结果来判断，不预先检查文件是否存在
        /// </summary>
        private bool OpenFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                // 执行成功，取消丢失标记
                _fileValidator?.UnmarkMissing(filePath);
                return true;
            }
            catch (Exception)
            {
                // 执行失败，标记为丢失
                _fileValidator?.MarkMissing(filePath);
                return false;
            }
        }

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
        /// 显示物品栏
        /// </summary>
        private void ShowInventory()
        {
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
            foreach (var slotId in _slotBorders.Keys)
            {
                var item = _slotService.GetSlot(slotId);
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
    }
}