using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CraftSharp.Models;
using CraftSharp.Services;
using CraftSharp.Helpers;
using CraftSharp.Windows.Dialogs;

namespace CraftSharp.Windows.StatusBar
{
    /// <summary>
    /// 快捷栏和格子功能
    ///
    /// 布局规则：
    /// 1. 副手槽间距42px基准
    /// 2. 全局垂直间距6px基准
    /// 3. 使用Grid布局，不使用Canvas定位
    ///
    /// 数据规则：
    /// 快捷栏格子始终使用共享数据，不受 InventorySettings.SharedData 影响
    /// </summary>
    public partial class StatusBarWindow
    {
        private readonly SlotDataService _slotService = SlotDataService.Instance;
        private readonly string[] _slotIds = { "hotbar_left_offhand", "hotbar_right_offhand", "hotbar_0", "hotbar_1", "hotbar_2", "hotbar_3", "hotbar_4", "hotbar_5", "hotbar_6", "hotbar_7", "hotbar_8" };

        // ===== 服务实例 =====
#pragma warning disable CS8618 // 字段在构造函数中通过 InitializeSlotServices 初始化
        private SlotIconService _iconService;
        private SlotDragService _dragService;
#pragma warning restore CS8618

        // ===== 长按拖动相关状态（已迁移到服务，此处仅保留UI状态） =====
        /// <summary>
        /// 长按开始时的鼠标位置（用于移动阈值检测）
        /// </summary>
        private System.Windows.Point _longPressStartPoint;

        /// <summary>
        /// 悬浮效果是否启用（hover显示selection框）
        /// </summary>
        private bool _hoverEffectEnabled = true;

        /// <summary>
        /// 点击模式（"single"单击/"double"双击）
        /// </summary>
        private string _clickMode = "double";

        /// <summary>
        /// 当前选中的格子全局索引（-1表示无选中，0=左副手槽，1=右副手槽，2-10=主快捷栏Slot0-8）
        /// </summary>
        private int _selectedSlotIndex = -1;

        private double _originalHotbarWidth;
        private double _originalHotbarHeight;
        private double _originalOffhandWidth;
        private double _originalOffhandHeight;

        /// <summary>
        /// 副手槽与核心容器之间的间距基准
        /// </summary>
        private const double BaseOffhandSpacing = 7;

        /// <summary>
        /// 全局垂直间距基准
        /// </summary>
        private const double BaseVerticalSpacing = 1;

        /// <summary>
        /// 初始化格子相关服务（在构造函数中调用，_scaleFactor 和 _appSettings 已初始化）
        /// </summary>
        private void InitializeSlotServices()
        {
            // 使用 SlotFileValidator 单例
            var fileValidator = SlotFileValidator.Instance;
            _iconService = new SlotIconService(fileValidator, _appSettings, _scaleFactor);
            _dragService = new SlotDragService(_slotService);

            // 设置格子ID映射
            _dragService.SlotIdMapper = index =>
            {
                if (index >= 0 && index < _slotIds.Length)
                    return _slotIds[index];
                return $"slot_{index}";
            };

            // 快捷栏始终使用共享数据（不受 SharedData 开关影响）
            _dragService.SharedData = true;

            // 订阅服务事件
            SubscribeServiceEvents();
        }

        /// <summary>
        /// 订阅服务事件
        /// </summary>
        private void SubscribeServiceEvents()
        {
            // 文件丢失/恢复事件
            _iconService.IconNeedsUpdate += OnIconNeedsUpdate;

            // 拖动事件
            _dragService.DragStarted += OnDragStarted;
            _dragService.DragEnded += OnDragEnded;
            _dragService.SwapCompleted += OnSwapCompleted;
            _dragService.HoverChanged += OnHoverChanged;
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
            // 清除选中状态
            ClearSlotSelection();

            // 显示拖动图标副本
            ShowDragIconCopy(e.SourceSlotIndex, e.SourceItem);
        }

        /// <summary>
        /// 拖动结束事件处理
        /// </summary>
        private void OnDragEnded(object? sender, SlotDragService.DragEndedEventArgs e)
        {
            // 隐藏拖动图标副本
            DragIconCanvas.Visibility = Visibility.Collapsed;

            // 清理hover效果
            ClearDragHoverEffect();

            // 释放鼠标捕获
            ReleaseMouseCapture();

            // 根据事件参数处理
            if (e.ShouldRestore)
            {
                RestoreSlotIcon(e.SourceSlotIndex);
            }
        }

        /// <summary>
        /// 格子交换完成事件处理
        /// </summary>
        private void OnSwapCompleted(object? sender, SlotDragService.SwapCompletedEventArgs e)
        {
            // 更新UI显示（使用缓存的图标Source和渲染模式）
            SwapSlotIconsUI(e.SourceSlotIndex, e.TargetSlotIndex, e.SourceItem.IsEmpty, e.TargetItem.IsEmpty);

            // 如果涉及 hotbar 格子（索引 2-10 对应 hotbar_0~hotbar_8），通知 InventoryWindow 刷新
            if (e.SourceSlotIndex >= 2 && e.SourceSlotIndex <= 10 ||
                e.TargetSlotIndex >= 2 && e.TargetSlotIndex <= 10)
            {
                if (App.Current is App app)
                {
                    app.GetInventoryWindow()?.RefreshIcons();
                }
            }
        }

        /// <summary>
        /// Hover变化事件处理
        /// </summary>
        private void OnHoverChanged(object? sender, SlotDragService.HoverChangedEventArgs e)
        {
            UpdateDragHoverEffectUI(e.CurrentHoverSlotIndex, e.LastHoverSlotIndex);
        }

        #endregion

        /// <summary>
        /// 加载快捷栏图片尺寸
        /// </summary>
        private void LoadHotbarDimensions()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.Hotbar);
            if (File.Exists(path))
            {
                using (var stream = File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalHotbarWidth = frame.PixelWidth;
                    _originalHotbarHeight = frame.PixelHeight;
                }
            }
        }

        /// <summary>
        /// 加载副手槽图片尺寸
        /// </summary>
        private void LoadOffhandDimensions()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.HotbarOffhand);
            if (File.Exists(path))
            {
                using (var stream = File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalOffhandWidth = frame.PixelWidth;
                    _originalOffhandHeight = frame.PixelHeight;
                }
            }
        }

        /// <summary>
        /// 设置快捷栏（宽度占满核心容器）
        /// </summary>
        private void SetupHotbar()
        {
            double hotbarWidth = GetCoreContainerWidth();
            double hotbarHeight = _originalHotbarHeight * _scaleFactor;

            HotbarImage.Source = LoadBitmapImage(AssetPaths.Hotbar);
            HotbarImage.Width = hotbarWidth;
            HotbarImage.Height = hotbarHeight;
            HotbarImage.Visibility = _hotbarVisible ? Visibility.Visible : Visibility.Collapsed;

            HotbarGrid.Width = hotbarWidth;
            HotbarGrid.Height = hotbarHeight;
            HotbarGrid.Visibility = _hotbarVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 设置副手槽位置
        /// </summary>
        private void SetupOffhandSlots()
        {
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double offhandHeight = _originalOffhandHeight * _scaleFactor;

            LeftOffhandImage.Source = LoadBitmapImage(AssetPaths.HotbarOffhand);
            LeftOffhandImage.Width = offhandWidth;
            LeftOffhandImage.Height = offhandHeight;
            LeftOffhandGrid.Width = offhandWidth;
            LeftOffhandGrid.Height = offhandHeight;
            LeftOffhandGrid.Visibility = _leftOffhandEnabled ? Visibility.Visible : Visibility.Collapsed;

            RightOffhandImage.Source = LoadBitmapImage(AssetPaths.HotbarOffhand);
            RightOffhandImage.Width = offhandWidth;
            RightOffhandImage.Height = offhandHeight;
            RightOffhandScaleTransform.ScaleX = -1;
            RightOffhandGrid.Width = offhandWidth;
            RightOffhandGrid.Height = offhandHeight;
            RightOffhandGrid.Visibility = _rightOffhandEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 设置格子位置和大小
        /// </summary>
        private void SetupSlots()
        {
            double margin = 3 * _scaleFactor;
            double dropZoneExpansion = 2 * _scaleFactor;
            double slotSize = 16 * _scaleFactor;
            double dropZoneSize = slotSize + 2 * dropZoneExpansion;
            double iconSize = slotSize;
            double selectionSize = 24 * _scaleFactor;
            double selectionHeight = 23 * _scaleFactor;

            // 设置副手格子
            SetupOffhandSlotBorder("LeftOffhand", margin, dropZoneExpansion, dropZoneSize, iconSize, _leftOffhandEnabled);
            SetupOffhandSlotBorder("RightOffhand", margin, dropZoneExpansion, dropZoneSize, iconSize, _rightOffhandEnabled);

            // 主快捷栏格子布局
            double slotSpacing = 4 * _scaleFactor;
            double columnWidth = slotSize + slotSpacing;

            HotbarSlotsGrid.Margin = new Thickness(margin - dropZoneExpansion);

            HotbarSlotsGrid.ColumnDefinitions.Clear();
            for (int i = 0; i < 9; i++)
            {
                HotbarSlotsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(columnWidth) });
            }

            HotbarSlotsGrid.RowDefinitions.Clear();
            HotbarSlotsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(dropZoneSize) });

            HotbarSlotsGrid.Children.Clear();

            // 设置选中框叠加层
            SetupSelectionOverlay(margin, dropZoneExpansion, columnWidth, selectionSize, selectionHeight);

            // 创建主快捷栏格子
            for (int i = 0; i < 9; i++)
            {
                CreateHotbarSlot(i, dropZoneSize, iconSize);
            }
        }

        /// <summary>
        /// 设置副手槽格子
        /// </summary>
        private void SetupOffhandSlotBorder(string name, double margin, double dropZoneExpansion, double dropZoneSize, double iconSize, bool enabled)
        {
            var border = GetSlotBorder(name);
            var icon = GetIconImage(name);
            if (border != null && icon != null)
            {
                border.Margin = new Thickness(margin - dropZoneExpansion);
                border.Width = dropZoneSize;
                border.Height = dropZoneSize;
                icon.Width = iconSize;
                icon.Height = iconSize;
                border.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 设置选中框叠加层
        /// </summary>
        private void SetupSelectionOverlay(double margin, double dropZoneExpansion, double columnWidth, double selectionSize, double selectionHeight)
        {
            SelectionOverlayCanvas.Margin = new Thickness(margin - dropZoneExpansion);
            SelectionOverlayCanvas.Children.Clear();

            double hotbarHeight = _originalHotbarHeight * _scaleFactor;
            double canvasMarginTop = margin - dropZoneExpansion;

            for (int i = 0; i < 9; i++)
            {
                double leftPosition = i * columnWidth + (columnWidth - selectionSize) / 2;
                double topPosition = hotbarHeight - canvasMarginTop - selectionHeight;

                var selection = new System.Windows.Controls.Image
                {
                    Name = $"Selection{i}",
                    Source = LoadBitmapImage(AssetPaths.HotbarSelection),
                    Stretch = Stretch.Uniform,
                    Width = selectionSize,
                    Height = selectionHeight,
                    Visibility = Visibility.Collapsed
                };
                RenderOptions.SetBitmapScalingMode(selection, BitmapScalingMode.NearestNeighbor);
                SelectionOverlayCanvas.Children.Add(selection);
                Canvas.SetLeft(selection, leftPosition);
                Canvas.SetTop(selection, topPosition);
            }
        }

        /// <summary>
        /// 创建主快捷栏格子
        /// </summary>
        private void CreateHotbarSlot(int index, double dropZoneSize, double iconSize)
        {
            var border = new Border
            {
                Name = $"Slot{index}",
                Background = System.Windows.Media.Brushes.Transparent,
                Width = dropZoneSize,
                Height = dropZoneSize,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Visibility = _hotbarVisible ? Visibility.Visible : Visibility.Collapsed
            };
            border.MouseLeftButtonDown += Slot_MouseLeftButtonDown;
            border.MouseLeftButtonUp += Slot_Click;
            border.MouseEnter += Slot_MouseEnter;
            border.MouseLeave += Slot_MouseLeave;
            border.MouseMove += Slot_MouseMove;
            border.MouseRightButtonDown += Slot_MouseRightButtonDown;

            var icon = new System.Windows.Controls.Image
            {
                Name = $"Icon{index}",
                Stretch = Stretch.Uniform,
                Width = iconSize,
                Height = iconSize,
                Visibility = Visibility.Collapsed
            };
            RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);

            border.Child = icon;
            HotbarSlotsGrid.Children.Add(border);
            Grid.SetColumn(border, index);
            Grid.SetRow(border, 0);
        }

        /// <summary>
        /// 鼠标进入格子
        /// </summary>
        private void Slot_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_hoverEffectEnabled) return;

            var border = (Border)sender;
            int slotIndex = GetSlotIndex(border);

            if (slotIndex >= 2 && slotIndex <= 10)
            {
                if (_selectedSlotIndex == -1 || slotIndex == _selectedSlotIndex)
                {
                    var selection = GetSelectionImage(slotIndex - 2);
                    if (selection != null)
                    {
                        selection.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        /// <summary>
        /// 鼠标离开格子
        /// </summary>
        private void Slot_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_hoverEffectEnabled) return;

            var border = (Border)sender;
            int slotIndex = GetSlotIndex(border);

            if (slotIndex >= 2 && slotIndex <= 10)
            {
                if (slotIndex != _selectedSlotIndex)
                {
                    var selection = GetSelectionImage(slotIndex - 2);
                    if (selection != null)
                    {
                        selection.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        /// <summary>
        /// 鼠标按下 - 启动长按检测
        /// </summary>
        private void Slot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 拖拽前执行全量检查
            if (App.Current is App app)
            {
                app.ValidateAllSlots();
            }

            var border = (Border)sender;
            var slotIndex = GetSlotIndex(border);

            if (slotIndex < 0) return;

            _longPressStartPoint = e.GetPosition(this);
            _dragService.StartLongPressDetection(slotIndex);
        }

        /// <summary>
        /// 鼠标移动 - 检测移动阈值
        /// </summary>
        private void Slot_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_dragService.IsDragging) return;

            var currentPos = e.GetPosition(this);
            var distance = Math.Sqrt(
                Math.Pow(currentPos.X - _longPressStartPoint.X, 2) +
                Math.Pow(currentPos.Y - _longPressStartPoint.Y, 2));

            _dragService.HandleMouseMove(distance);
        }

        /// <summary>
        /// 显示拖动图标副本
        /// </summary>
        private void ShowDragIconCopy(int sourceSlotIndex, SlotItem sourceItem)
        {
            ImageSource? iconSource = null;
            BitmapScalingMode renderMode = BitmapScalingMode.Linear;

            if (!sourceItem.IsEmpty)
            {
                // 检查文件是否丢失
                bool isMissing = SlotFileValidator.Instance.IsMissing(sourceItem.FilePath);

                if (isMissing)
                {
                    // 文件丢失：使用占位图
                    iconSource = _iconService.LoadPlaceholderIcon();
                    renderMode = BitmapScalingMode.NearestNeighbor;
                }
                else
                {
                    // 文件正常：使用格子图标
                    System.Windows.Controls.Image? slotIconImage = GetSlotIconImage(sourceSlotIndex);
                    if (slotIconImage != null && slotIconImage.Source != null)
                    {
                        iconSource = slotIconImage.Source;
                        renderMode = RenderOptions.GetBitmapScalingMode(slotIconImage);
                    }
                }
            }

            // 清除源格子的图标显示
            if (!sourceItem.IsEmpty)
            {
                HideSlotIconUI(sourceSlotIndex);
            }

            // 设置拖动图标副本
            if (iconSource != null)
            {
                DragIconImage.Source = iconSource;
                RenderOptions.SetBitmapScalingMode(DragIconImage, renderMode);
                double iconSize = 16 * _scaleFactor;
                DragIconImage.Width = iconSize;
                DragIconImage.Height = iconSize;
                DragIconCanvas.Visibility = Visibility.Visible;

                var mousePos = Mouse.GetPosition(this);
                Canvas.SetLeft(DragIconImage, mousePos.X - iconSize / 2);
                Canvas.SetTop(DragIconImage, mousePos.Y - iconSize / 2);
            }
            else
            {
                DragIconCanvas.Visibility = Visibility.Collapsed;
            }

            CaptureMouse();
        }

        /// <summary>
        /// 拖动过程中鼠标移动（由窗口级别OnMouseMove调用）
        /// </summary>
        public void UpdateDragIconPosition(System.Windows.Point mousePos)
        {
            if (!_dragService.IsDragging) return;

            Canvas.SetLeft(DragIconImage, mousePos.X - DragIconImage.Width / 2);
            Canvas.SetTop(DragIconImage, mousePos.Y - DragIconImage.Height / 2);

            var targetSlotIndex = GetSlotIndexAtPosition(mousePos);
            _dragService.UpdateDragTarget(targetSlotIndex);
        }

        /// <summary>
        /// 拖动结束（由窗口级别OnMouseLeftButtonUp调用）
        /// </summary>
        public void EndSlotDrag()
        {
            _dragService.EndDrag();
        }

        /// <summary>
        /// 更新拖动hover效果UI
        /// </summary>
        private void UpdateDragHoverEffectUI(int currentSlotIndex, int lastHoverSlotIndex)
        {
            // 隐藏上一个hover格子的selection框
            if (lastHoverSlotIndex >= 2 && lastHoverSlotIndex <= 10)
            {
                int lastHotbarIndex = lastHoverSlotIndex - 2;
                var lastSelection = GetSelectionImage(lastHotbarIndex);
                if (lastSelection != null && lastHotbarIndex != currentSlotIndex - 2)
                {
                    lastSelection.Visibility = Visibility.Collapsed;
                }
            }

            // 显示当前hover格子的selection框
            if (currentSlotIndex >= 2 && currentSlotIndex <= 10)
            {
                int hotbarIndex = currentSlotIndex - 2;
                var selection = GetSelectionImage(hotbarIndex);
                if (selection != null)
                {
                    selection.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 清理拖动hover效果
        /// </summary>
        private void ClearDragHoverEffect()
        {
            var lastHover = _dragService.DragTargetSlotIndex;
            if (lastHover >= 2 && lastHover <= 10)
            {
                var selection = GetSelectionImage(lastHover - 2);
                if (selection != null)
                {
                    selection.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 恢复格子图标显示（拖动取消时）
        /// </summary>
        private void RestoreSlotIcon(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotIds.Length) return;

            ImageSource cachedIconSource = DragIconImage.Source;
            System.Windows.Controls.Image iconImage = GetSlotIconImage(slotIndex);

            if (iconImage != null && cachedIconSource != null)
            {
                iconImage.Source = cachedIconSource;
                iconImage.Visibility = Visibility.Visible;

                bool isPlaceholder = SlotIconService.IsPlaceholderImage(cachedIconSource);
                RenderOptions.SetBitmapScalingMode(iconImage, isPlaceholder ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
            }
        }

        /// <summary>
        /// 隐藏格子图标UI
        /// </summary>
        private void HideSlotIconUI(int slotIndex)
        {
            var iconImage = GetSlotIconImage(slotIndex);
            if (iconImage != null)
            {
                iconImage.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 交换格子图标UI显示（使用缓存的图标Source和渲染模式）
        /// </summary>
        private void SwapSlotIconsUI(int sourceIndex, int targetIndex, bool sourceIsEmpty, bool targetIsEmpty)
        {
            ImageSource? sourceIconSource = GetSlotIconSource(sourceIndex);
            ImageSource? targetIconSource = GetSlotIconSource(targetIndex);
            BitmapScalingMode sourceRenderMode = GetSlotRenderMode(sourceIndex);
            BitmapScalingMode targetRenderMode = GetSlotRenderMode(targetIndex);

            SetSlotIconSourceUI(sourceIndex, targetIconSource, targetRenderMode, targetIsEmpty);
            SetSlotIconSourceUI(targetIndex, sourceIconSource, sourceRenderMode, sourceIsEmpty);
        }

        /// <summary>
        /// 获取格子图标Image控件
        /// </summary>
        private System.Windows.Controls.Image? GetSlotIconImage(int slotIndex)
        {
            if (slotIndex == 0)
                return GetIconImage("LeftOffhand");
            else if (slotIndex == 1)
                return GetIconImage("RightOffhand");
            else
                return GetIconImage(slotIndex - 2);
        }

        /// <summary>
        /// 获取格子图标Source
        /// </summary>
        private ImageSource? GetSlotIconSource(int slotIndex)
        {
            var iconImage = GetSlotIconImage(slotIndex);
            return iconImage?.Source;
        }

        /// <summary>
        /// 获取格子渲染模式
        /// </summary>
        private BitmapScalingMode GetSlotRenderMode(int slotIndex)
        {
            var iconImage = GetSlotIconImage(slotIndex);
            if (iconImage != null)
            {
                return RenderOptions.GetBitmapScalingMode(iconImage);
            }
            return BitmapScalingMode.HighQuality;
        }

        /// <summary>
        /// 设置格子图标Source和渲染模式UI
        /// </summary>
        private void SetSlotIconSourceUI(int slotIndex, ImageSource? iconSource, BitmapScalingMode renderMode, bool isEmpty)
        {
            var iconImage = GetSlotIconImage(slotIndex);

            if (iconImage != null)
            {
                if (isEmpty || iconSource == null)
                {
                    iconImage.Source = null;
                    iconImage.Visibility = Visibility.Collapsed;
                    RenderOptions.SetBitmapScalingMode(iconImage, BitmapScalingMode.HighQuality);
                }
                else
                {
                    iconImage.Source = iconSource;
                    iconImage.Visibility = Visibility.Visible;
                    RenderOptions.SetBitmapScalingMode(iconImage, renderMode);
                }
            }
        }

        /// <summary>
        /// 加载已保存的格子数据
        /// </summary>
        private void LoadSlots()
        {
            // 程序启动时已执行全量检查，此处直接根据丢失状态显示图标

            // 加载副手格子
            LoadOffhandSlot(0, "LeftOffhand", _leftOffhandEnabled);
            LoadOffhandSlot(1, "RightOffhand", _rightOffhandEnabled);

            // 加载主快捷栏格子
            for (int i = 2; i <= 10; i++)
            {
                LoadHotbarSlot(i);
            }
        }

        /// <summary>
        /// 加载副手格子
        /// </summary>
        private void LoadOffhandSlot(int slotIndex, string name, bool enabled)
        {
            if (!enabled) return;

            var item = _slotService.GetSlot(_slotIds[slotIndex]);
            if (!item.IsEmpty)
            {
                if (SlotFileValidator.Instance.IsMissing(item.FilePath))
                {
                    ShowPlaceholderIconUI(name);
                }
                else
                {
                    SetSlotIconFromPath(name, item.FilePath);
                }
            }
            else
            {
                // 格子为空时清除图标
                ClearSlotIconUI(slotIndex);
            }
        }

        /// <summary>
        /// 加载主快捷栏格子
        /// </summary>
        private void LoadHotbarSlot(int slotIndex)
        {
            var item = _slotService.GetSlot(_slotIds[slotIndex]);
            if (!item.IsEmpty)
            {
                if (SlotFileValidator.Instance.IsMissing(item.FilePath))
                {
                    ShowPlaceholderIconUI(slotIndex - 2);
                }
                else
                {
                    SetSlotIconFromPath(slotIndex - 2, item.FilePath);
                }
            }
            else
            {
                // 格子为空时清除图标
                ClearSlotIconUI(slotIndex);
            }
        }

        /// <summary>
        /// 设置格子图标（从路径加载）
        /// </summary>
        private void SetSlotIconFromPath(string name, string filePath)
        {
            var result = _iconService.GetIconWithRenderMode(filePath);
            var iconImage = GetIconImage(name);
            if (iconImage != null && result.IconSource != null)
            {
                iconImage.Source = result.IconSource;
                iconImage.Visibility = Visibility.Visible;
                RenderOptions.SetBitmapScalingMode(iconImage, result.RenderMode);
            }
        }

        /// <summary>
        /// 设置格子图标（从路径加载）
        /// </summary>
        private void SetSlotIconFromPath(int index, string filePath)
        {
            var result = _iconService.GetIconWithRenderMode(filePath);
            var iconImage = GetIconImage(index);
            if (iconImage != null && result.IconSource != null)
            {
                iconImage.Source = result.IconSource;
                iconImage.Visibility = Visibility.Visible;
                RenderOptions.SetBitmapScalingMode(iconImage, result.RenderMode);
            }
        }

        /// <summary>
        /// 显示占位图UI
        /// </summary>
        private void ShowPlaceholderIconUI(string name)
        {
            var placeholder = _iconService.LoadPlaceholderIcon();
            var iconImage = GetIconImage(name);
            if (iconImage != null && placeholder != null)
            {
                iconImage.Source = placeholder;
                RenderOptions.SetBitmapScalingMode(iconImage, BitmapScalingMode.NearestNeighbor);
                iconImage.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 显示占位图UI
        /// </summary>
        private void ShowPlaceholderIconUI(int hotbarIndex)
        {
            var placeholder = _iconService.LoadPlaceholderIcon();
            var iconImage = GetIconImage(hotbarIndex);
            if (iconImage != null && placeholder != null)
            {
                iconImage.Source = placeholder;
                RenderOptions.SetBitmapScalingMode(iconImage, BitmapScalingMode.NearestNeighbor);
                iconImage.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 更新所有使用指定路径的格子为占位图
        /// </summary>
        private void UpdateSlotsToPlaceholder(string filePath)
        {
            if (_leftOffhandEnabled)
            {
                var leftItem = _slotService.GetSlot(_slotIds[0]);
                if (!leftItem.IsEmpty && leftItem.FilePath == filePath)
                {
                    ShowPlaceholderIconUI("LeftOffhand");
                }
            }

            if (_rightOffhandEnabled)
            {
                var rightItem = _slotService.GetSlot(_slotIds[1]);
                if (!rightItem.IsEmpty && rightItem.FilePath == filePath)
                {
                    ShowPlaceholderIconUI("RightOffhand");
                }
            }

            for (int i = 2; i <= 10; i++)
            {
                var item = _slotService.GetSlot(_slotIds[i]);
                if (!item.IsEmpty && item.FilePath == filePath)
                {
                    ShowPlaceholderIconUI(i - 2);
                }
            }
        }

        /// <summary>
        /// 更新所有使用指定路径的格子为正常图标
        /// </summary>
        private void UpdateSlotsToNormal(string filePath)
        {
            if (_leftOffhandEnabled)
            {
                var leftItem = _slotService.GetSlot(_slotIds[0]);
                if (!leftItem.IsEmpty && leftItem.FilePath == filePath)
                {
                    SetSlotIconFromPath("LeftOffhand", filePath);
                }
            }

            if (_rightOffhandEnabled)
            {
                var rightItem = _slotService.GetSlot(_slotIds[1]);
                if (!rightItem.IsEmpty && rightItem.FilePath == filePath)
                {
                    SetSlotIconFromPath("RightOffhand", filePath);
                }
            }

            for (int i = 2; i <= 10; i++)
            {
                var item = _slotService.GetSlot(_slotIds[i]);
                if (!item.IsEmpty && item.FilePath == filePath)
                {
                    SetSlotIconFromPath(i - 2, filePath);
                }
            }
        }

        /// <summary>
        /// 获取选中框Image控件
        /// </summary>
        private System.Windows.Controls.Image? GetSelectionImage(int index)
        {
            foreach (var child in SelectionOverlayCanvas.Children)
            {
                if (child is System.Windows.Controls.Image image && image.Name == $"Selection{index}")
                {
                    return image;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取图标Image控件
        /// </summary>
        private System.Windows.Controls.Image? GetIconImage(int index)
        {
            foreach (var child in HotbarSlotsGrid.Children)
            {
                if (child is Border border && border.Name == $"Slot{index}")
                {
                    return border.Child as System.Windows.Controls.Image;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取图标Image控件
        /// </summary>
        private System.Windows.Controls.Image GetIconImage(string name)
        {
            return (System.Windows.Controls.Image)FindName($"Icon{name}");
        }

        /// <summary>
        /// 获取格子Border控件
        /// </summary>
        private Border? GetSlotBorder(int index)
        {
            foreach (var child in HotbarSlotsGrid.Children)
            {
                if (child is Border border && border.Name == $"Slot{index}")
                {
                    return border;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取格子Border控件
        /// </summary>
        private Border GetSlotBorder(string name)
        {
            return (Border)FindName($"Slot{name}");
        }

        /// <summary>
        /// 点击格子
        /// </summary>
        private void Slot_Click(object sender, MouseButtonEventArgs e)
        {
            // 如果正在长按检测（定时器等待中），取消检测并视为点击
            if (_dragService.IsTimerRunning)
            {
                _dragService.CancelLongPress();
                // 继续执行点击逻辑（不return）
            }

            // 如果真正启动了拖动，结束拖动并处理交换
            if (_dragService.IsDragging)
            {
                _dragService.EndDrag();
                return;
            }

            // 如果定时器触发但没移动超过阈值（_isDragReady），视为点击
            // 清理状态后继续执行点击逻辑
            if (_dragService.IsDragReady)
            {
                _dragService.CancelLongPress();
                // 继续执行点击逻辑
            }

            var border = (Border)sender;
            var slotIndex = GetSlotIndex(border);

            if (slotIndex >= 0)
            {
                HandleSlotClick(slotIndex);
            }
        }

        /// <summary>
        /// 处理格子点击
        /// </summary>
        private void HandleSlotClick(int slotIndex)
        {
            // 点击前执行全量检查
            if (App.Current is App app)
            {
                app.ValidateAllSlots();
            }

            var item = _slotService.GetSlot(_slotIds[slotIndex]);
            bool isEmpty = item.IsEmpty;
            bool isMissing = SlotFileValidator.Instance.IsMissing(item.FilePath);

            if (_clickMode == "single")
            {
                if (isEmpty) return;

                // 丢失文件：显示确认对话框
                if (isMissing)
                {
                    HandleMissingFileSlot(slotIndex, item.FilePath);
                    return;
                }

                // 尝试打开文件（仅打开，不判断丢失）
                TryExecuteFile(item.FilePath);
            }
            else // double
            {
                if (_selectedSlotIndex == slotIndex)
                {
                    if (isEmpty)
                    {
                        ClearSlotSelection();
                        return;
                    }

                    // 丢失文件：显示确认对话框
                    if (isMissing)
                    {
                        HandleMissingFileSlot(slotIndex, item.FilePath);
                        ClearSlotSelection();
                        return;
                    }

                    // 尝试打开文件（仅打开，不判断丢失）
                    TryExecuteFile(item.FilePath);
                    ClearSlotSelection();
                }
                else
                {
                    ClearSlotSelection();
                    _selectedSlotIndex = slotIndex;

                    int hotbarSlotIndex = slotIndex >= 2 ? slotIndex - 2 : -1;
                    if (hotbarSlotIndex >= 0)
                    {
                        var selection = GetSelectionImage(hotbarSlotIndex);
                        if (selection != null)
                        {
                            selection.Visibility = Visibility.Visible;
                        }
                    }

                    // 显示文件名（选中时）
                    if (!isEmpty && !isMissing)
                    {
                        string fileName = System.IO.Path.GetFileName(item.FilePath);
                        ShowFileName(fileName);
                    }
                }
            }
        }

        /// <summary>
        /// 处理文件丢失的格子点击
        /// </summary>
        private void HandleMissingFileSlot(int slotIndex, string filePath)
        {
            var confirmWindow = new SlotMissingConfirmWindow(filePath);
            confirmWindow.Owner = Window.GetWindow(this);
            confirmWindow.ShowDialog();

            if (confirmWindow.IsConfirmed)
            {
                // 清除所有使用相同路径的格子（跨快捷栏+物品栏）
                SlotFileValidator.Instance.ClearAllSlotsByPath(
                    (App.Current as App)?.GetAppSettings(), filePath);

                // 刷新图标显示
                RefreshHotbarIcons();

                // 通知物品栏刷新（如果物品栏窗口存在）
                if (App.Current is App app)
                {
                    app.GetInventoryWindow()?.RefreshIcons();
                }
            }
        }

        /// <summary>
        /// 清空格子图标UI
        /// </summary>
        private void ClearSlotIconUI(int slotIndex)
        {
            var iconImage = GetSlotIconImage(slotIndex);
            if (iconImage != null)
            {
                iconImage.Source = null;
                iconImage.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 清除格子选中状态
        /// </summary>
        private void ClearSlotSelection()
        {
            if (_selectedSlotIndex >= 2)
            {
                var selection = GetSelectionImage(_selectedSlotIndex - 2);
                if (selection != null)
                {
                    selection.Visibility = Visibility.Collapsed;
                }
            }
            _selectedSlotIndex = -1;

            // 立即隐藏文件名（无动画，用于切换选中时立即清空）
            HideFileNameImmediately();
        }

        /// <summary>
        /// 公开方法：清除选中状态
        /// </summary>
        public void ClearSelection()
        {
            ClearSlotSelection();
        }

        /// <summary>
        /// 获取格子索引
        /// </summary>
        private int GetSlotIndex(Border border)
        {
            if (GetSlotBorder("LeftOffhand") == border) return 0;
            if (GetSlotBorder("RightOffhand") == border) return 1;

            for (int i = 0; i < 9; i++)
            {
                if (GetSlotBorder(i) == border) return i + 2;
            }

            return -1;
        }

        /// <summary>
        /// 尝试执行文件
        /// </summary>
        private bool TryExecuteFile(string filePath)
        {
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
        /// 移除格子内容
        /// </summary>
        public void RemoveSlot(int index)
        {
            if (index >= 0 && index < _slotIds.Length)
            {
                _slotService.ClearSlot(_slotIds[index]);
                ClearSlotIconUI(index);
            }
        }

        /// <summary>
        /// 设置悬浮效果启用状态
        /// </summary>
        public void SetHoverEffectEnabled(bool enabled)
        {
            _hoverEffectEnabled = enabled;
        }

        /// <summary>
        /// 设置点击模式
        /// </summary>
        public void SetClickMode(string mode)
        {
            _clickMode = mode;
            ClearSlotSelection();
        }

        /// <summary>
        /// 设置副手槽启用状态
        /// </summary>
        public void SetOffhandConfig(bool leftEnabled, bool rightEnabled)
        {
            _leftOffhandEnabled = leftEnabled;
            _rightOffhandEnabled = rightEnabled;

            LeftOffhandGrid.Visibility = leftEnabled ? Visibility.Visible : Visibility.Collapsed;
            RightOffhandGrid.Visibility = rightEnabled ? Visibility.Visible : Visibility.Collapsed;

            SetupOffhandSlots();
            SetupSlots();
            LoadSlots();

            UpdateLayout();
        }

        /// <summary>
        /// 根据鼠标位置判断落在哪个格子
        /// </summary>
        public int GetSlotIndexAtPosition(System.Windows.Point mousePos)
        {
            double margin = 3 * _scaleFactor;
            double dropZoneExpansion = 2 * _scaleFactor;
            double slotSize = 16 * _scaleFactor;
            double dropZoneSize = slotSize + 2 * dropZoneExpansion;
            double slotSpacing = 4 * _scaleFactor;
            double columnWidth = slotSize + slotSpacing;

            // 主快捷栏格子区域
            var hotbarGridPos = HotbarGrid.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
            double hotbarLeft = hotbarGridPos.X;
            double hotbarTop = hotbarGridPos.Y;
            double hotbarHeight = _originalHotbarHeight * _scaleFactor;

            double slotsGridLeft = hotbarLeft + margin - dropZoneExpansion;
            double slotsGridTop = hotbarTop + hotbarHeight - dropZoneSize - (margin - dropZoneExpansion);

            for (int i = 0; i < 9; i++)
            {
                double slotLeft = slotsGridLeft + i * columnWidth;
                double slotRight = slotLeft + dropZoneSize;

                if (mousePos.X >= slotLeft && mousePos.X <= slotRight &&
                    mousePos.Y >= slotsGridTop && mousePos.Y <= slotsGridTop + dropZoneSize)
                {
                    return i + 2;
                }
            }

            // 副手槽
            if (_leftOffhandEnabled)
            {
                var leftPos = LeftOffhandGrid.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
                double leftSlotLeft = leftPos.X + margin - dropZoneExpansion;
                double leftSlotTop = leftPos.Y + _originalOffhandHeight * _scaleFactor - dropZoneSize - (margin - dropZoneExpansion);

                if (mousePos.X >= leftSlotLeft && mousePos.X <= leftSlotLeft + dropZoneSize &&
                    mousePos.Y >= leftSlotTop && mousePos.Y <= leftSlotTop + dropZoneSize)
                {
                    return 0;
                }
            }

            if (_rightOffhandEnabled)
            {
                var rightPos = RightOffhandGrid.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
                double rightSlotLeft = rightPos.X + margin - dropZoneExpansion;
                double rightSlotTop = rightPos.Y + _originalOffhandHeight * _scaleFactor - dropZoneSize - (margin - dropZoneExpansion);

                if (mousePos.X >= rightSlotLeft && mousePos.X <= rightSlotLeft + dropZoneSize &&
                    mousePos.Y >= rightSlotTop && mousePos.Y <= rightSlotTop + dropZoneSize)
                {
                    return 1;
                }
            }

            return -1;
        }

        /// <summary>
        /// 处理文件拖放
        /// </summary>
        public void ProcessFileDrop(int slotIndex, string filePath)
        {
            if (slotIndex < 0 || slotIndex >= _slotIds.Length) return;

            _slotService.SetSlot(_slotIds[slotIndex], new SlotItem { FilePath = filePath });

            if (slotIndex == 0)
                SetSlotIconFromPath("LeftOffhand", filePath);
            else if (slotIndex == 1)
                SetSlotIconFromPath("RightOffhand", filePath);
            else
                SetSlotIconFromPath(slotIndex - 2, filePath);

            // 如果是 hotbar 格子（索引 2-10 对应 hotbar_0~hotbar_8），通知 InventoryWindow 刷新
            if (slotIndex >= 2 && slotIndex <= 10)
            {
                if (App.Current is App app)
                {
                    app.GetInventoryWindow()?.RefreshIcons();
                }
            }
        }

        /// <summary>
        /// 刷新快捷栏图标
        /// 快捷栏始终使用共享数据，不受 SharedData 开关影响
        /// </summary>
        public void RefreshHotbarIcons()
        {
            // 重新创建 _iconService 以使用最新的配置（特别是 ShowTargetIcon）
            if (_iconService != null)
            {
                _iconService.IconNeedsUpdate -= OnIconNeedsUpdate;
            }
            var fileValidator = SlotFileValidator.Instance;
            _iconService = new SlotIconService(fileValidator, _appSettings, _scaleFactor);
            _iconService.IconNeedsUpdate += OnIconNeedsUpdate;

            LoadSlots();
        }

        /// <summary>
        /// 右键点击格子 - 显示右键菜单
        /// </summary>
        private void Slot_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 执行全量检查
            if (App.Current is App app)
            {
                app.ValidateAllSlots();
            }

            var border = (Border)sender;
            var slotIndex = GetSlotIndex(border);
            if (slotIndex < 0) return;

            var slotId = _slotIds[slotIndex];
            var item = _slotService.GetSlot(slotId);
            bool isMissing = !item.IsEmpty && SlotFileValidator.Instance.IsMissing(item.FilePath);

            // 快捷栏始终使用共享数据
            string currentStyle = "inventory.png";
            bool sharedData = true;

            var menu = SlotContextMenuService.Instance.CreateSlotContextMenu(
                slotId,
                item,
                isMissing,
                currentStyle,
                sharedData,
                () => RefreshSlotUI(slotId, slotIndex));

            menu.PlacementTarget = border;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            menu.IsOpen = true;

            e.Handled = true;
        }

        /// <summary>
        /// 刷新单个格子UI（右键菜单操作后）
        /// </summary>
        private void RefreshSlotUI(string slotId, int slotIndex)
        {
            var item = _slotService.GetSlot(slotId);
            if (item.IsEmpty)
            {
                ClearSlotIconUI(slotIndex);
            }
            else
            {
                if (SlotFileValidator.Instance.IsMissing(item.FilePath))
                {
                    if (slotIndex == 0)
                        ShowPlaceholderIconUI("LeftOffhand");
                    else if (slotIndex == 1)
                        ShowPlaceholderIconUI("RightOffhand");
                    else
                        ShowPlaceholderIconUI(slotIndex - 2);
                }
                else
                {
                    if (slotIndex == 0)
                        SetSlotIconFromPath("LeftOffhand", item.FilePath);
                    else if (slotIndex == 1)
                        SetSlotIconFromPath("RightOffhand", item.FilePath);
                    else
                        SetSlotIconFromPath(slotIndex - 2, item.FilePath);
                }
            }
        }
    }
}