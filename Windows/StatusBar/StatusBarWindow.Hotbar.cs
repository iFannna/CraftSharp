using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CraftSharp.Models;
using CraftSharp.Services;
using CraftSharp.Helpers;

namespace CraftSharp.Windows.StatusBar
{
    /// <summary>
    /// 快捷栏和格子功能
    ///
    /// 布局规则：
    /// 1. 副手槽间距42px基准
    /// 2. 全局垂直间距6px基准
    /// 3. 使用Grid布局，不使用Canvas定位
    /// </summary>
    public partial class StatusBarWindow
    {
        private readonly SlotDataService _slotService;
        private readonly string[] _slotIds = { "hotbar_left_offhand", "hotbar_right_offhand", "hotbar_0", "hotbar_1", "hotbar_2", "hotbar_3", "hotbar_4", "hotbar_5", "hotbar_6", "hotbar_7", "hotbar_8" };

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

        /// <summary>
        /// 加载快捷栏图片尺寸
        /// </summary>
        private void LoadHotbarDimensions()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.Hotbar);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
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
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.HotbarOffhand);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
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
        /// 快捷栏是核心容器最下方的元素
        /// </summary>
        private void SetupHotbar()
        {
            double hotbarWidth = GetCoreContainerWidth(); // 占满核心容器
            double hotbarHeight = _originalHotbarHeight * _scaleFactor;

            HotbarImage.Source = LoadBitmapImage(AssetPaths.Hotbar);
            HotbarImage.Width = hotbarWidth;
            HotbarImage.Height = hotbarHeight;
            HotbarImage.Visibility = _hotbarVisible ? Visibility.Visible : Visibility.Collapsed;

            // 设置快捷栏Grid尺寸
            HotbarGrid.Width = hotbarWidth;
            HotbarGrid.Height = hotbarHeight;
            HotbarGrid.Visibility = _hotbarVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 设置副手槽位置
        /// 副手槽位于核心容器外部，间距42px基准
        /// </summary>
        private void SetupOffhandSlots()
        {
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double offhandHeight = _originalOffhandHeight * _scaleFactor;

            // 左副手槽
            LeftOffhandImage.Source = LoadBitmapImage(AssetPaths.HotbarOffhand);
            LeftOffhandImage.Width = offhandWidth;
            LeftOffhandImage.Height = offhandHeight;
            LeftOffhandGrid.Width = offhandWidth;
            LeftOffhandGrid.Height = offhandHeight;
            LeftOffhandGrid.Visibility = _leftOffhandEnabled ? Visibility.Visible : Visibility.Collapsed;

            // 右副手槽
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
        /// 使用Grid布局，固定列宽
        /// 拖拽热区比格子显示区域大（上下左右各+2），覆盖边框
        /// </summary>
        private void SetupSlots()
        {
            // 副手格子布局参数（基于原图22×22）
            // 格子显示区域=16×16，拖拽热区=20×20（上下左右各+2）
            double margin = 3 * _scaleFactor;
            double dropZoneExpansion = 2 * _scaleFactor;
            double slotSize = 16 * _scaleFactor; // 格子显示尺寸
            double dropZoneSize = slotSize + 2 * dropZoneExpansion; // 拖拽热区尺寸 = 20
            double iconSize = slotSize; // 图标显示尺寸 = 16
            double selectionSize = 24 * _scaleFactor; // selection 图片宽度 = 24
            double selectionHeight = 23 * _scaleFactor; // selection 图片高度 = 23（底部对齐快捷栏）

            // 设置左副手格子
            var leftOffhandBorder = GetSlotBorder("LeftOffhand");
            var leftOffhandIcon = GetIconImage("LeftOffhand");
            if (leftOffhandBorder != null && leftOffhandIcon != null)
            {
                // Border居中于副手槽Grid，热区覆盖边框
                leftOffhandBorder.Margin = new Thickness(margin - dropZoneExpansion); // 1px margin
                leftOffhandBorder.Width = dropZoneSize; // 20
                leftOffhandBorder.Height = dropZoneSize; // 20
                leftOffhandIcon.Width = iconSize; // 16
                leftOffhandIcon.Height = iconSize; // 16
                leftOffhandBorder.Visibility = _leftOffhandEnabled ? Visibility.Visible : Visibility.Collapsed;
            }

            // 设置右副手格子
            var rightOffhandBorder = GetSlotBorder("RightOffhand");
            var rightOffhandIcon = GetIconImage("RightOffhand");
            if (rightOffhandBorder != null && rightOffhandIcon != null)
            {
                rightOffhandBorder.Margin = new Thickness(margin - dropZoneExpansion); // 1px margin
                rightOffhandBorder.Width = dropZoneSize; // 20
                rightOffhandBorder.Height = dropZoneSize; // 20
                rightOffhandIcon.Width = iconSize; // 16
                rightOffhandIcon.Height = iconSize; // 16
                rightOffhandBorder.Visibility = _rightOffhandEnabled ? Visibility.Visible : Visibility.Collapsed;
            }

            // 主快捷栏格子布局参数（基于原图182×22）
            // 格子显示区域=16×16，格子间距=4px，拖拽热区=20×20
            double slotSpacing = 4 * _scaleFactor;
            double columnWidth = slotSize + slotSpacing; // 每列宽度 = 20（格子+间距）

            // 设置格子容器的Margin（热区需要向外扩展2）
            HotbarSlotsGrid.Margin = new Thickness(margin - dropZoneExpansion); // 1px

            // 设置列定义：每列宽度=格子+间距=20
            HotbarSlotsGrid.ColumnDefinitions.Clear();
            for (int i = 0; i < 9; i++)
            {
                HotbarSlotsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(columnWidth) });
            }

            // 设置行定义：单行，高度=热区尺寸
            HotbarSlotsGrid.RowDefinitions.Clear();
            HotbarSlotsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(dropZoneSize) });

            // 清除现有格子并重新添加
            HotbarSlotsGrid.Children.Clear();

            // ===== 设置选中框叠加层（Canvas 允许超出边界） =====
            SelectionOverlayCanvas.Margin = new Thickness(margin - dropZoneExpansion); // 与格子容器相同

            SelectionOverlayCanvas.Children.Clear();

            // 创建 9 个 selection 图片，使用 Canvas.Left/Top 定位
            // 水平：居中于每列（列宽20，selection宽24，左右各扩展2）
            // 垂直：底部对齐快捷栏（hotbar高22，selection高23，向上超出1）
            double hotbarHeight = _originalHotbarHeight * _scaleFactor; // 22
            double canvasMarginTop = margin - dropZoneExpansion; // 1
            for (int i = 0; i < 9; i++)
            {
                // 水平居中于列
                double leftPosition = i * columnWidth + (columnWidth - selectionSize) / 2;

                // 垂直底部对齐快捷栏
                // Canvas.SetTop = hotbarHeight - canvasMarginTop - selectionHeight
                double topPosition = hotbarHeight - canvasMarginTop - selectionHeight;

                var selection = new System.Windows.Controls.Image
                {
                    Name = $"Selection{i}",
                    Source = LoadBitmapImage(AssetPaths.HotbarSelection),
                    Stretch = Stretch.Uniform,
                    Width = selectionSize, // 24
                    Height = selectionHeight, // 23
                    Visibility = Visibility.Collapsed
                };
                RenderOptions.SetBitmapScalingMode(selection, BitmapScalingMode.NearestNeighbor);
                SelectionOverlayCanvas.Children.Add(selection);
                Canvas.SetLeft(selection, leftPosition);
                Canvas.SetTop(selection, topPosition); // 底部对齐快捷栏
            }

            // 创建 9 个格子 Border
            for (int i = 0; i < 9; i++)
            {
                var border = new Border
                {
                    Name = $"Slot{i}",
                    Background = System.Windows.Media.Brushes.Transparent,
                    Width = dropZoneSize, // 20（拖拽热区）
                    Height = dropZoneSize, // 20（拖拽热区）
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center, // 居中于列
                    VerticalAlignment = System.Windows.VerticalAlignment.Center, // 居中于行
                    Visibility = _hotbarVisible ? Visibility.Visible : Visibility.Collapsed
                };
                border.MouseLeftButtonUp += Slot_Click;
                // 原生拖放已接管 AllowDrop/Drop/DragOver，不再使用 WPF 拖放
                border.MouseEnter += Slot_MouseEnter;
                border.MouseLeave += Slot_MouseLeave;

                var icon = new System.Windows.Controls.Image
                {
                    Name = $"Icon{i}",
                    Stretch = Stretch.Uniform,
                    Width = iconSize, // 16（图标显示）
                    Height = iconSize, // 16（图标显示）
                    Visibility = Visibility.Collapsed
                };
                RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);

                border.Child = icon;
                HotbarSlotsGrid.Children.Add(border);
                Grid.SetColumn(border, i);
                Grid.SetRow(border, 0);
            }
        }

        /// <summary>
        /// 鼠标进入格子 - 显示悬浮框
        /// </summary>
        private void Slot_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_hoverEffectEnabled) return;

            var border = (Border)sender;
            int hotbarIndex = -1; // 主快捷栏索引（0-8）
            for (int i = 0; i < 9; i++)
            {
                if (border.Name == $"Slot{i}")
                {
                    hotbarIndex = i;
                    break;
                }
            }
            // 仅主快捷栏格子显示悬浮效果
            if (hotbarIndex >= 0)
            {
                // 全局索引：主快捷栏格子 = hotbarIndex + 2
                int globalIndex = hotbarIndex + 2;
                // 有选中格子时不显示其他格子的悬浮效果
                if (_selectedSlotIndex == -1 || globalIndex == _selectedSlotIndex)
                {
                    var selection = GetSelectionImage(hotbarIndex);
                    if (selection != null)
                    {
                        selection.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        /// <summary>
        /// 鼠标离开格子 - 隐藏悬浮框（但不隐藏选中格子的框）
        /// </summary>
        private void Slot_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_hoverEffectEnabled) return;

            var border = (Border)sender;
            int hotbarIndex = -1; // 主快捷栏索引（0-8）
            for (int i = 0; i < 9; i++)
            {
                if (border.Name == $"Slot{i}")
                {
                    hotbarIndex = i;
                    break;
                }
            }
            // 仅主快捷栏格子处理
            if (hotbarIndex >= 0)
            {
                // 全局索引：主快捷栏格子 = hotbarIndex + 2
                int globalIndex = hotbarIndex + 2;
                // 不是当前选中的格子才隐藏悬浮框
                if (globalIndex != _selectedSlotIndex)
                {
                    var selection = GetSelectionImage(hotbarIndex);
                    if (selection != null)
                    {
                        selection.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        /// <summary>
        /// 获取选中框 Image 控件
        /// </summary>
        private System.Windows.Controls.Image GetSelectionImage(int index)
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
        /// 加载已保存的格子数据
        /// </summary>
        private void LoadSlots()
        {
            // 加载左副手格子
            var leftOffhandItem = _slotService.GetSlot(_slotIds[0]);
            if (!leftOffhandItem.IsEmpty && _leftOffhandEnabled)
            {
                SetSlotIcon("LeftOffhand", leftOffhandItem.FilePath);
            }

            // 加载右副手格子
            var rightOffhandItem = _slotService.GetSlot(_slotIds[1]);
            if (!rightOffhandItem.IsEmpty && _rightOffhandEnabled)
            {
                SetSlotIcon("RightOffhand", rightOffhandItem.FilePath);
            }

            // 加载主快捷栏格子
            for (int i = 2; i <= 10; i++)
            {
                var slotId = _slotIds[i];
                var item = _slotService.GetSlot(slotId);

                if (!item.IsEmpty)
                {
                    SetSlotIcon(i - 2, item.FilePath);
                }
            }
        }

        /// <summary>
        /// 设置格子图标
        /// </summary>
        private void SetSlotIcon(int index, string filePath)
        {
            var icon = GetHotbarIcon(filePath);
            if (icon != null)
            {
                var iconImage = GetIconImage(index);
                if (iconImage != null)
                {
                    iconImage.Source = icon;
                    iconImage.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 设置格子图标
        /// </summary>
        private void SetSlotIcon(string name, string filePath)
        {
            var icon = GetHotbarIcon(filePath);
            if (icon != null)
            {
                var iconImage = GetIconImage(name);
                if (iconImage != null)
                {
                    iconImage.Source = icon;
                    iconImage.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 根据设置获取快捷栏图标
        /// 对于快捷方式：根据 HotbarShowTargetIcon 设置决定显示快捷方式图标还是目标程序图标
        /// </summary>
        private ImageSource? GetHotbarIcon(string filePath)
        {
            int iconSize = (int)(32 * _scaleFactor);

            // 检查是否为快捷方式
            if (IconExtractor.IsShortcut(filePath))
            {
                bool showTargetIcon = _appSettings?.HotbarShowTargetIcon ?? false;
                if (showTargetIcon)
                {
                    // 显示目标程序图标
                    return IconExtractor.GetTargetIcon(filePath, iconSize);
                }
                else
                {
                    // 显示快捷方式图标
                    return IconExtractor.GetShortcutIcon(filePath, iconSize);
                }
            }

            // 普通文件，使用默认图标提取
            return IconExtractor.GetIcon(filePath, iconSize);
        }

        /// <summary>
        /// 刷新快捷栏所有图标（用于设置切换后重新加载）
        /// </summary>
        public void RefreshHotbarIcons()
        {
            LoadSlots();
        }

        /// <summary>
        /// 获取图标 Image 控件
        /// </summary>
        private System.Windows.Controls.Image GetIconImage(int index)
        {
            // 从HotbarSlotsGrid中查找
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
        /// 获取图标 Image 控件
        /// </summary>
        private System.Windows.Controls.Image GetIconImage(string name)
        {
            return (System.Windows.Controls.Image)FindName($"Icon{name}");
        }

        /// <summary>
        /// 获取格子 Border 控件
        /// </summary>
        private Border GetSlotBorder(int index)
        {
            // 从HotbarSlotsGrid中查找
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
        /// 获取格子 Border 控件
        /// </summary>
        private Border GetSlotBorder(string name)
        {
            return (Border)FindName($"Slot{name}");
        }

        /// <summary>
        /// 点击格子 - 根据点击模式处理
        /// 单击模式：直接执行操作
        /// 双击模式：第一次选中，第二次执行
        /// </summary>
        private void Slot_Click(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            var slotIndex = GetSlotIndex(border); // 获取全局索引（0=左副手槽，1=右副手槽，2-10=主快捷栏Slot0-8）

            if (slotIndex >= 0)
            {
                // 获取主快捷栏格子索引（仅用于selection框显示，2-10转换为0-8）
                int hotbarSlotIndex = slotIndex >= 2 ? slotIndex - 2 : -1;

                if (_clickMode == "single")
                {
                    // 单击模式：直接执行操作
                    var item = _slotService.GetSlot(_slotIds[slotIndex]);
                    if (!item.IsEmpty)
                    {
                        OpenFile(item.FilePath);
                    }
                }
                else // double
                {
                    // 双击模式：第一次选中（不显示selection框），第二次执行
                    if (_selectedSlotIndex == slotIndex)
                    {
                        // 再次点击同一格子 → 执行操作
                        var item = _slotService.GetSlot(_slotIds[slotIndex]);
                        if (!item.IsEmpty)
                        {
                            OpenFile(item.FilePath);
                        }
                        // 执行后清除选中
                        ClearSlotSelection();
                    }
                    else
                    {
                        // 点击不同格子 → 切换选中到新格子
                        ClearSlotSelection();
                        _selectedSlotIndex = slotIndex;
                        // 只有主快捷栏格子显示selection框（副手槽不显示）
                        if (hotbarSlotIndex >= 0)
                        {
                            var selection = GetSelectionImage(hotbarSlotIndex);
                            if (selection != null)
                            {
                                selection.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 清除格子选中状态
        /// </summary>
        private void ClearSlotSelection()
        {
            if (_selectedSlotIndex >= 0)
            {
                // 只有主快捷栏格子需要隐藏selection框（全局索引2-10对应主快捷栏0-8）
                if (_selectedSlotIndex >= 2)
                {
                    int hotbarIndex = _selectedSlotIndex - 2;
                    var selection = GetSelectionImage(hotbarIndex);
                    if (selection != null)
                    {
                        selection.Visibility = Visibility.Collapsed;
                    }
                }
                _selectedSlotIndex = -1;
            }
        }

        /// <summary>
        /// 公开方法：清除选中状态（供外部调用）
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
            if (GetSlotBorder("LeftOffhand") == border)
                return 0;

            if (GetSlotBorder("RightOffhand") == border)
                return 1;

            for (int i = 0; i < 9; i++)
            {
                if (GetSlotBorder(i) == border)
                    return i + 2;
            }

            return -1;
        }

        /// <summary>
        /// 打开文件/程序
        /// </summary>
        private void OpenFile(string filePath)
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
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"无法打开: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 右键菜单 - 移除格子内容
        /// </summary>
        public void RemoveSlot(int index)
        {
            if (index >= 0 && index < _slotIds.Length)
            {
                _slotService.ClearSlot(_slotIds[index]);
                if (index == 0)
                {
                    var icon = GetIconImage("LeftOffhand");
                    if (icon != null)
                    {
                        icon.Source = null;
                        icon.Visibility = Visibility.Collapsed;
                    }
                }
                else if (index == 1)
                {
                    var icon = GetIconImage("RightOffhand");
                    if (icon != null)
                    {
                        icon.Source = null;
                        icon.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    var icon = GetIconImage(index - 2);
                    if (icon != null)
                    {
                        icon.Source = null;
                        icon.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        /// <summary>
        /// 设置悬浮效果启用状态（hover显示selection框）
        /// </summary>
        public void SetHoverEffectEnabled(bool enabled)
        {
            _hoverEffectEnabled = enabled;
        }

        /// <summary>
        /// 设置点击模式（"single"单击/"double"双击）
        /// </summary>
        public void SetClickMode(string mode)
        {
            _clickMode = mode;
            // 切换模式时清除选中状态
            ClearSlotSelection();
        }

        /// <summary>
        /// 设置副手槽启用状态
        /// 窗口宽度固定，只改变副手槽Grid的可见性，位置不变
        /// </summary>
        public void SetOffhandConfig(bool leftEnabled, bool rightEnabled)
        {
            _leftOffhandEnabled = leftEnabled;
            _rightOffhandEnabled = rightEnabled;

            // 更新副手槽显示（只改变可见性，不改变布局）
            LeftOffhandGrid.Visibility = leftEnabled ? Visibility.Visible : Visibility.Collapsed;
            RightOffhandGrid.Visibility = rightEnabled ? Visibility.Visible : Visibility.Collapsed;

            SetupOffhandSlots();
            SetupSlots();
            LoadSlots();

            // 更新布局（窗口位置不变）
            UpdateLayout();
        }

        /// <summary>
        /// 根据鼠标位置判断落在哪个格子
        /// 返回全局索引（0=左副手，1=右副手，2-10=主快捷栏Slot0-8），-1表示不在格子区域
        /// </summary>
        public int GetSlotIndexAtPosition(System.Windows.Point mousePos)
        {
            double margin = 3 * _scaleFactor;
            double dropZoneExpansion = 2 * _scaleFactor;
            double slotSize = 16 * _scaleFactor;
            double dropZoneSize = slotSize + 2 * dropZoneExpansion; // 20
            double slotSpacing = 4 * _scaleFactor;
            double columnWidth = slotSize + slotSpacing; // 20

            // 计算主快捷栏格子区域（相对于窗口）
            // HotbarSlotsGrid 位于 HotbarGrid 内，HotbarGrid 位于 CoreContainerGrid 内
            // 需要计算 HotbarSlotsGrid 相对于窗口的位置

            // 获取 HotbarGrid 相对于窗口的位置
            var hotbarGridPos = HotbarGrid.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
            double hotbarLeft = hotbarGridPos.X;
            double hotbarTop = hotbarGridPos.Y;
            double hotbarHeight = _originalHotbarHeight * _scaleFactor;

            // HotbarSlotsGrid 的 Margin = margin - dropZoneExpansion = 1
            double slotsGridLeft = hotbarLeft + margin - dropZoneExpansion;
            double slotsGridTop = hotbarTop + hotbarHeight - dropZoneSize - (margin - dropZoneExpansion);

            // 检查主快捷栏格子（Slot0-8）
            for (int i = 0; i < 9; i++)
            {
                double slotLeft = slotsGridLeft + i * columnWidth;
                double slotRight = slotLeft + dropZoneSize;
                double slotTop = slotsGridTop;
                double slotBottom = slotTop + dropZoneSize;

                if (mousePos.X >= slotLeft && mousePos.X <= slotRight &&
                    mousePos.Y >= slotTop && mousePos.Y <= slotBottom)
                {
                    return i + 2; // 主快捷栏索引 = i + 2
                }
            }

            // 检查副手槽
            if (_leftOffhandEnabled)
            {
                var leftOffhandGridPos = LeftOffhandGrid.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
                double leftSlotLeft = leftOffhandGridPos.X + margin - dropZoneExpansion;
                double leftSlotTop = leftOffhandGridPos.Y + _originalOffhandHeight * _scaleFactor - dropZoneSize - (margin - dropZoneExpansion);

                if (mousePos.X >= leftSlotLeft && mousePos.X <= leftSlotLeft + dropZoneSize &&
                    mousePos.Y >= leftSlotTop && mousePos.Y <= leftSlotTop + dropZoneSize)
                {
                    return 0; // 左副手槽
                }
            }

            if (_rightOffhandEnabled)
            {
                var rightOffhandGridPos = RightOffhandGrid.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
                double rightSlotLeft = rightOffhandGridPos.X + margin - dropZoneExpansion;
                double rightSlotTop = rightOffhandGridPos.Y + _originalOffhandHeight * _scaleFactor - dropZoneSize - (margin - dropZoneExpansion);

                if (mousePos.X >= rightSlotLeft && mousePos.X <= rightSlotLeft + dropZoneSize &&
                    mousePos.Y >= rightSlotTop && mousePos.Y <= rightSlotTop + dropZoneSize)
                {
                    return 1; // 右副手槽
                }
            }

            return -1; // 不在任何格子区域
        }

        /// <summary>
        /// 处理文件拖放（原生拖放回调调用）
        /// </summary>
        public void ProcessFileDrop(int slotIndex, string filePath)
        {
            if (slotIndex < 0 || slotIndex >= _slotIds.Length) return;

            _slotService.SetSlot(_slotIds[slotIndex], new Models.SlotItem
            {
                FilePath = filePath
            });

            if (slotIndex == 0)
                SetSlotIcon("LeftOffhand", filePath);
            else if (slotIndex == 1)
                SetSlotIcon("RightOffhand", filePath);
            else
                SetSlotIcon(slotIndex - 2, filePath);
        }
    }
}