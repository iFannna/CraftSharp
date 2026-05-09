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
        /// </summary>
        private void SetupSlots()
        {
            // 副手格子布局参数（基于原图22×22）
            // Margin=3px，格子=16×16
            double margin = 3 * _scaleFactor;
            double slotSize = 16 * _scaleFactor;
            double iconSize = slotSize; // 图标刚好填满格子

            // 设置左副手格子
            var leftOffhandBorder = GetSlotBorder("LeftOffhand");
            var leftOffhandIcon = GetIconImage("LeftOffhand");
            if (leftOffhandBorder != null && leftOffhandIcon != null)
            {
                leftOffhandBorder.Margin = new Thickness(margin);
                leftOffhandBorder.Width = slotSize;
                leftOffhandBorder.Height = slotSize;
                leftOffhandIcon.Width = iconSize;
                leftOffhandIcon.Height = iconSize;
                leftOffhandBorder.Visibility = _leftOffhandEnabled ? Visibility.Visible : Visibility.Collapsed;
            }

            // 设置右副手格子
            var rightOffhandBorder = GetSlotBorder("RightOffhand");
            var rightOffhandIcon = GetIconImage("RightOffhand");
            if (rightOffhandBorder != null && rightOffhandIcon != null)
            {
                rightOffhandBorder.Margin = new Thickness(margin);
                rightOffhandBorder.Width = slotSize;
                rightOffhandBorder.Height = slotSize;
                rightOffhandIcon.Width = iconSize;
                rightOffhandIcon.Height = iconSize;
                rightOffhandBorder.Visibility = _rightOffhandEnabled ? Visibility.Visible : Visibility.Collapsed;
            }

            // 主快捷栏格子精确布局参数（基于原图182×22）
            // 格子尺寸：16×16，格子间距：4px（margin已在副手槽部分定义）
            // 容器可用宽度 = 182-6 = 176px = 9×16 + 8×4
            double slotSpacing = 4 * _scaleFactor;
            double columnWidth = slotSize + slotSpacing; // 每列宽度 = 格子 + 间距

            // 设置格子容器的Margin
            HotbarSlotsGrid.Margin = new Thickness(margin);

            // 设置列定义：前8列宽度=格子+间距，最后一列宽度=格子
            HotbarSlotsGrid.ColumnDefinitions.Clear();
            for (int i = 0; i < 8; i++)
            {
                HotbarSlotsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(columnWidth) });
            }
            HotbarSlotsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(slotSize) });

            // 设置行定义：单行，高度=格子尺寸
            HotbarSlotsGrid.RowDefinitions.Clear();
            HotbarSlotsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(slotSize) });

            // 清除现有格子并重新添加
            HotbarSlotsGrid.Children.Clear();

            for (int i = 0; i < 9; i++)
            {
                var border = new Border
                {
                    Name = $"Slot{i}",
                    Background = System.Windows.Media.Brushes.Transparent,
                    Width = slotSize,
                    Height = slotSize,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Visibility = _hotbarVisible ? Visibility.Visible : Visibility.Collapsed
                };
                border.MouseLeftButtonDown += Slot_Click;
                border.AllowDrop = true;
                border.Drop += Slot_Drop;
                border.DragOver += Slot_DragOver;

                var icon = new System.Windows.Controls.Image
                {
                    Name = $"Icon{i}",
                    Stretch = Stretch.Uniform,
                    Width = iconSize,
                    Height = iconSize,
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