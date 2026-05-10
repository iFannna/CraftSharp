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
        /// 选中效果是否启用（hover显示selection框）
        /// </summary>
        private bool _selectionEffectEnabled = true;

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
                border.MouseLeftButtonDown += Slot_Click;
                border.AllowDrop = true;
                border.Drop += Slot_Drop;
                border.DragOver += Slot_DragOver;
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
        /// 鼠标进入格子 - 显示选中框
        /// </summary>
        private void Slot_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_selectionEffectEnabled) return;

            var border = (Border)sender;
            int slotIndex = -1;
            for (int i = 0; i < 9; i++)
            {
                if (border.Name == $"Slot{i}")
                {
                    slotIndex = i;
                    break;
                }
            }
            if (slotIndex >= 0)
            {
                var selection = GetSelectionImage(slotIndex);
                if (selection != null)
                {
                    selection.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 鼠标离开格子 - 隐藏选中框
        /// </summary>
        private void Slot_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_selectionEffectEnabled) return;

            var border = (Border)sender;
            int slotIndex = -1;
            for (int i = 0; i < 9; i++)
            {
                if (border.Name == $"Slot{i}")
                {
                    slotIndex = i;
                    break;
                }
            }
            if (slotIndex >= 0)
            {
                var selection = GetSelectionImage(slotIndex);
                if (selection != null)
                {
                    selection.Visibility = Visibility.Collapsed;
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
            var icon = IconExtractor.GetIcon(filePath, (int)(32 * _scaleFactor));
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
            var icon = IconExtractor.GetIcon(filePath, (int)(32 * _scaleFactor));
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
        /// 点击格子 - 打开文件/程序
        /// </summary>
        private void Slot_Click(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            var slotIndex = GetSlotIndex(border);

            if (slotIndex >= 0)
            {
                var item = _slotService.GetSlot(_slotIds[slotIndex]);

                if (!item.IsEmpty)
                {
                    OpenFile(item.FilePath);
                }
            }
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
        /// 拖拽进入格子
        /// </summary>
        private void Slot_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
            e.Handled = true;
        }

        /// <summary>
        /// 拖拽放下 - 添加到格子
        /// </summary>
        private void Slot_Drop(object sender, System.Windows.DragEventArgs e)
        {
            var border = (Border)sender;
            var slotIndex = GetSlotIndex(border);

            if (slotIndex >= 0 && e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    var filePath = files[0];

                    _slotService.SetSlot(_slotIds[slotIndex], new SlotItem
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
        /// 设置选中效果启用状态（hover显示selection框）
        /// </summary>
        public void SetSelectionEffectEnabled(bool enabled)
        {
            _selectionEffectEnabled = enabled;
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
    }
}