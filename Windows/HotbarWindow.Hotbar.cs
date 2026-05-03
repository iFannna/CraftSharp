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
    public partial class HotbarWindow
    {
        private readonly SlotDataService _slotService;
        private readonly string[] _slotIds = { "hotbar_offhand", "hotbar_0", "hotbar_1", "hotbar_2", "hotbar_3", "hotbar_4", "hotbar_5", "hotbar_6", "hotbar_7", "hotbar_8" };

        private double _originalHotbarWidth;
        private double _originalHotbarHeight;
        private double _originalOffhandWidth;
        private double _originalOffhandHeight;
        private bool _offhandOnRight = false; // 副手槽位置配置：默认左边
        private double _offhandSpacing = 7; // 副手槽与快捷栏之间的间距（像素）

        /// <summary>
        /// 加载快捷栏图片尺寸
        /// </summary>
        private void LoadHotbarDimensions()
        {
            // hotbar.png
            var uri = new Uri(AssetPaths.Hotbar, UriKind.Relative);
            var stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
            if (stream != null)
            {
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                var frame = decoder.Frames[0];
                _originalHotbarWidth = frame.PixelWidth;
                _originalHotbarHeight = frame.PixelHeight;
            }
        }

        /// <summary>
        /// 加载副手槽图片尺寸
        /// </summary>
        private void LoadOffhandDimensions()
        {
            // hotbar_offhand.png
            var uri = new Uri(AssetPaths.HotbarOffhand, UriKind.Relative);
            var stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
            if (stream != null)
            {
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                var frame = decoder.Frames[0];
                _originalOffhandWidth = frame.PixelWidth;
                _originalOffhandHeight = frame.PixelHeight;
            }
        }

        /// <summary>
        /// 设置快捷栏位置（在最底部）
        /// </summary>
        private void SetupHotbar()
        {
            double hotbarWidth = _originalHotbarWidth * _scaleFactor;
            double hotbarHeight = _originalHotbarHeight * _scaleFactor;

            HotbarImage.Width = hotbarWidth;
            HotbarImage.Height = hotbarHeight;

            // 快捷栏Y位置 = 经验条Y + 经验条高度 + 经验条与快捷栏间距
            double expBarTopOffset = GetExpBarTopOffset();
            double expBarHeight = _originalExpBarHeight * _scaleFactor;
            double expBarSpacing = _spacing * _scaleFactor;
            double hotbarTopOffset = expBarTopOffset + expBarHeight + expBarSpacing;

            // 快捷栏位置根据副手槽位置决定
            if (_offhandOnRight)
            {
                // 副手槽在右边，快捷栏在左边
                Canvas.SetLeft(HotbarImage, 0);
            }
            else
            {
                // 副手槽在左边（默认），快捷栏在右边
                Canvas.SetLeft(HotbarImage, (_originalOffhandWidth + _offhandSpacing) * _scaleFactor);
            }
            Canvas.SetTop(HotbarImage, hotbarTopOffset);
        }

        /// <summary>
        /// 设置副手槽位置（与快捷栏同一行）
        /// </summary>
        private void SetupOffhandSlot()
        {
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double offhandHeight = _originalOffhandHeight * _scaleFactor;

            OffhandImage.Width = offhandWidth;
            OffhandImage.Height = offhandHeight;

            // 副手槽Y位置 = 与快捷栏同一行
            double expBarTopOffset = GetExpBarTopOffset();
            double expBarHeight = _originalExpBarHeight * _scaleFactor;
            double expBarSpacing = _spacing * _scaleFactor;
            double hotbarTopOffset = expBarTopOffset + expBarHeight + expBarSpacing;

            // 根据配置设置副手槽位置和翻转
            if (_offhandOnRight)
            {
                // 副手槽在右边，图片翻转
                OffhandScaleTransform.ScaleX = -1;
                Canvas.SetLeft(OffhandImage, (_originalOffhandWidth + _offhandSpacing + _originalHotbarWidth) * _scaleFactor);
            }
            else
            {
                // 副手槽在左边（默认），图片正常
                OffhandScaleTransform.ScaleX = 1;
                Canvas.SetLeft(OffhandImage, 0);
            }
            Canvas.SetTop(OffhandImage, hotbarTopOffset);
        }

        /// <summary>
        /// 动态设置格子位置和大小（与快捷栏同一行）
        /// </summary>
        private void SetupSlots()
        {
            // 格子Y位置 = 与快捷栏同一行
            double expBarTopOffset = GetExpBarTopOffset();
            double expBarHeight = _originalExpBarHeight * _scaleFactor;
            double expBarSpacing = _spacing * _scaleFactor;
            double hotbarTopOffset = expBarTopOffset + expBarHeight + expBarSpacing;

            // 副手格子
            double offhandSlotWidth = _originalOffhandWidth * _scaleFactor;
            double offhandSlotHeight = _originalOffhandHeight * _scaleFactor;
            double offhandIconSize = _originalOffhandHeight * 0.73 * _scaleFactor;

            var offhandBorder = GetSlotBorder("Offhand");
            var offhandIcon = GetIconImage("Offhand");
            if (offhandBorder != null && offhandIcon != null)
            {
                offhandBorder.Width = offhandSlotWidth;
                offhandBorder.Height = offhandSlotHeight;
                offhandIcon.Width = offhandIconSize;
                offhandIcon.Height = offhandIconSize;

                // 副手格子位置根据配置决定
                if (_offhandOnRight)
                {
                    Canvas.SetLeft(offhandBorder, (_originalOffhandWidth + _offhandSpacing + _originalHotbarWidth) * _scaleFactor);
                }
                else
                {
                    Canvas.SetLeft(offhandBorder, 0);
                }
                Canvas.SetTop(offhandBorder, hotbarTopOffset);
            }

            // 主快捷栏格子 (9个)
            double slotWidth = _originalHotbarWidth / 9.0 * _scaleFactor;
            double slotHeight = _originalHotbarHeight * _scaleFactor;
            double iconSize = _originalHotbarHeight * 0.73 * _scaleFactor;

            // 快捷栏格子位置根据副手槽位置决定
            double hotbarLeft;
            if (_offhandOnRight)
            {
                hotbarLeft = 0;
            }
            else
            {
                hotbarLeft = (_originalOffhandWidth + _offhandSpacing) * _scaleFactor;
            }

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
            // 加载副手格子
            var offhandItem = _slotService.GetSlot(_slotIds[0]);
            if (!offhandItem.IsEmpty)
            {
                SetSlotIcon("Offhand", offhandItem.FilePath);
            }

            // 加载主快捷栏格子
            for (int i = 1; i <= 9; i++)
            {
                var slotId = _slotIds[i];
                var item = _slotService.GetSlot(slotId);

                if (!item.IsEmpty)
                {
                    SetSlotIcon(i - 1, item.FilePath);
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
            // 副手槽
            if (GetSlotBorder("Offhand") == border)
                return 0;

            // 主快捷栏格子
            for (int i = 0; i < 9; i++)
            {
                if (GetSlotBorder(i) == border)
                    return i + 1;
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

                    // 保存数据
                    _slotService.SetSlot(_slotIds[slotIndex], new SlotItem
                    {
                        FilePath = filePath
                    });

                    // 显示图标
                    if (slotIndex == 0)
                        SetSlotIcon("Offhand", filePath);
                    else
                        SetSlotIcon(slotIndex - 1, filePath);
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
                    GetIconImage("Offhand").Source = null;
                    GetIconImage("Offhand").Visibility = Visibility.Collapsed;
                }
                else
                {
                    GetIconImage(index - 1).Source = null;
                    GetIconImage(index - 1).Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}