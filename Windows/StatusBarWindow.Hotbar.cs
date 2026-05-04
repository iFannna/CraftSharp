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

namespace CraftSharp.Windows
{
    /// <summary>
    /// 快捷栏和格子功能
    /// </summary>
    public partial class StatusBarWindow
    {
        private readonly SlotDataService _slotService;
        private readonly string[] _slotIds = { "hotbar_left_offhand", "hotbar_right_offhand", "hotbar_0", "hotbar_1", "hotbar_2", "hotbar_3", "hotbar_4", "hotbar_5", "hotbar_6", "hotbar_7", "hotbar_8" };

        private double _originalHotbarWidth;
        private double _originalHotbarHeight;
        private double _originalOffhandWidth;
        private double _originalOffhandHeight;
        private double _offhandSpacing = 6; // 副手槽与快捷栏之间的间距（像素）

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
        /// 设置快捷栏位置（在最底部）
        /// 快捷栏是水平位置的基准点
        /// </summary>
        private void SetupHotbar()
        {
            double hotbarWidth = _originalHotbarWidth * _scaleFactor;
            double hotbarHeight = _originalHotbarHeight * _scaleFactor;

            HotbarImage.Source = LoadBitmapImage(AssetPaths.Hotbar);
            HotbarImage.Width = hotbarWidth;
            HotbarImage.Height = hotbarHeight;

            // 快捷栏Y位置 = 窗口底部
            double expBarTopOffset = GetExpBarTopOffset();
            double expBarHeight = _originalExpBarHeight * _scaleFactor;
            double expBarSpacing = _spacing * _scaleFactor;
            double hotbarTopOffset = expBarTopOffset + expBarHeight + expBarSpacing;

            // 快捷栏左边位置
            double hotbarLeft = GetHotbarLeft();
            Canvas.SetLeft(HotbarImage, hotbarLeft);
            Canvas.SetTop(HotbarImage, hotbarTopOffset);
        }

        /// <summary>
        /// 设置副手槽位置（浮动在快捷栏左右两侧）
        /// </summary>
        private void SetupOffhandSlots()
        {
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double offhandHeight = _originalOffhandHeight * _scaleFactor;
            double spacing = _offhandSpacing * _scaleFactor;

            // 副手槽Y位置 = 与快捷栏同一行
            double expBarTopOffset = GetExpBarTopOffset();
            double expBarHeight = _originalExpBarHeight * _scaleFactor;
            double expBarSpacing = _spacing * _scaleFactor;
            double hotbarTopOffset = expBarTopOffset + expBarHeight + expBarSpacing;

            // 快捷栏左边位置（基准点）
            double hotbarLeft = GetHotbarLeft();
            double hotbarWidth = _originalHotbarWidth * _scaleFactor;

            // 左副手槽：在快捷栏左边，间距6px
            LeftOffhandImage.Source = LoadBitmapImage(AssetPaths.HotbarOffhand);
            LeftOffhandImage.Width = offhandWidth;
            LeftOffhandImage.Height = offhandHeight;

            if (_leftOffhandEnabled)
            {
                LeftOffhandImage.Visibility = Visibility.Visible;
                Canvas.SetLeft(LeftOffhandImage, hotbarLeft - spacing - offhandWidth);
                Canvas.SetTop(LeftOffhandImage, hotbarTopOffset);
            }
            else
            {
                LeftOffhandImage.Visibility = Visibility.Collapsed;
            }

            // 右副手槽：在快捷栏右边，间距6px，图片翻转
            RightOffhandImage.Source = LoadBitmapImage(AssetPaths.HotbarOffhand);
            RightOffhandImage.Width = offhandWidth;
            RightOffhandImage.Height = offhandHeight;
            RightOffhandScaleTransform.ScaleX = -1;

            if (_rightOffhandEnabled)
            {
                RightOffhandImage.Visibility = Visibility.Visible;
                Canvas.SetLeft(RightOffhandImage, hotbarLeft + hotbarWidth + spacing);
                Canvas.SetTop(RightOffhandImage, hotbarTopOffset);
            }
            else
            {
                RightOffhandImage.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 设置格子位置和大小
        /// </summary>
        private void SetupSlots()
        {
            double expBarTopOffset = GetExpBarTopOffset();
            double expBarHeight = _originalExpBarHeight * _scaleFactor;
            double expBarSpacing = _spacing * _scaleFactor;
            double hotbarTopOffset = expBarTopOffset + expBarHeight + expBarSpacing;

            double hotbarLeft = GetHotbarLeft();
            double hotbarWidth = _originalHotbarWidth * _scaleFactor;
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double spacing = _offhandSpacing * _scaleFactor;

            // 副手格子尺寸
            double offhandSlotWidth = offhandWidth;
            double offhandSlotHeight = _originalOffhandHeight * _scaleFactor;
            double offhandIconSize = _originalOffhandHeight * 0.73 * _scaleFactor;

            // 左副手格子
            var leftOffhandBorder = GetSlotBorder("LeftOffhand");
            var leftOffhandIcon = GetIconImage("LeftOffhand");
            if (leftOffhandBorder != null && leftOffhandIcon != null)
            {
                leftOffhandBorder.Width = offhandSlotWidth;
                leftOffhandBorder.Height = offhandSlotHeight;
                leftOffhandIcon.Width = offhandIconSize;
                leftOffhandIcon.Height = offhandIconSize;

                if (_leftOffhandEnabled)
                {
                    leftOffhandBorder.Visibility = Visibility.Visible;
                    Canvas.SetLeft(leftOffhandBorder, hotbarLeft - spacing - offhandWidth);
                    Canvas.SetTop(leftOffhandBorder, hotbarTopOffset);
                }
                else
                {
                    leftOffhandBorder.Visibility = Visibility.Collapsed;
                }
            }

            // 右副手格子
            var rightOffhandBorder = GetSlotBorder("RightOffhand");
            var rightOffhandIcon = GetIconImage("RightOffhand");
            if (rightOffhandBorder != null && rightOffhandIcon != null)
            {
                rightOffhandBorder.Width = offhandSlotWidth;
                rightOffhandBorder.Height = offhandSlotHeight;
                rightOffhandIcon.Width = offhandIconSize;
                rightOffhandIcon.Height = offhandIconSize;

                if (_rightOffhandEnabled)
                {
                    rightOffhandBorder.Visibility = Visibility.Visible;
                    Canvas.SetLeft(rightOffhandBorder, hotbarLeft + hotbarWidth + spacing);
                    Canvas.SetTop(rightOffhandBorder, hotbarTopOffset);
                }
                else
                {
                    rightOffhandBorder.Visibility = Visibility.Collapsed;
                }
            }

            // 主快捷栏格子 (9个)
            double slotWidth = hotbarWidth / 9.0;
            double slotHeight = _originalHotbarHeight * _scaleFactor;
            double iconSize = _originalHotbarHeight * 0.73 * _scaleFactor;

            for (int i = 0; i < 9; i++)
            {
                var border = GetSlotBorder(i);
                var icon = GetIconImage(i);

                if (border != null && icon != null)
                {
                    border.Width = slotWidth;
                    border.Height = slotHeight;
                    icon.Width = iconSize;
                    icon.Height = iconSize;

                    Canvas.SetLeft(border, hotbarLeft + i * slotWidth);
                    Canvas.SetTop(border, hotbarTopOffset);
                }
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
                GetIconImage(index).Source = icon;
                GetIconImage(index).Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 设置格子图标（副手槽）
        /// </summary>
        private void SetSlotIcon(string name, string filePath)
        {
            var icon = IconExtractor.GetIcon(filePath, (int)(32 * _scaleFactor));
            if (icon != null)
            {
                GetIconImage(name).Source = icon;
                GetIconImage(name).Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 获取图标 Image 控件
        /// </summary>
        private System.Windows.Controls.Image GetIconImage(int index)
        {
            return (System.Windows.Controls.Image)FindName($"Icon{index}");
        }

        /// <summary>
        /// 获取图标 Image 控件（副手槽）
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
            return (Border)FindName($"Slot{index}");
        }

        /// <summary>
        /// 获取格子 Border 控件（副手槽）
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
                    GetIconImage("LeftOffhand").Source = null;
                    GetIconImage("LeftOffhand").Visibility = Visibility.Collapsed;
                }
                else if (index == 1)
                {
                    GetIconImage("RightOffhand").Source = null;
                    GetIconImage("RightOffhand").Visibility = Visibility.Collapsed;
                }
                else
                {
                    GetIconImage(index - 2).Source = null;
                    GetIconImage(index - 2).Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 设置副手槽启用状态
        /// 窗口尺寸和位置固定不变，只切换副手槽的显示/隐藏
        /// </summary>
        public void SetOffhandConfig(bool leftEnabled, bool rightEnabled)
        {
            _leftOffhandEnabled = leftEnabled;
            _rightOffhandEnabled = rightEnabled;

            // 只更新副手槽显示，窗口尺寸和位置不变
            SetupOffhandSlots();
            SetupSlots();
            LoadSlots();
        }
    }
}