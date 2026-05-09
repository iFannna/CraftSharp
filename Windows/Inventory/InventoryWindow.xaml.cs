using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CraftSharp.Services;
using CraftSharp.Helpers;

namespace CraftSharp.Windows.Inventory
{
    public partial class InventoryWindow : Window
    {
        private readonly SlotDataService _slotService;
        private readonly string[] _slotIds;

        // 基准分辨率：2560下放大6倍
        private const double BaseScreenWidth = 2560;
        private const double BaseScaleMultiplier = 6;

        private double _scaleFactor;
        private double _originalImageWidth;
        private double _originalImageHeight;

        public InventoryWindow()
        {
            InitializeComponent();

            // 设置窗口到桌面层级
            SourceInitialized += (s, e) => DesktopWindowHelper.SetWindowToDesktopLevel(this);

            _slotService = new SlotDataService();

            // 生成27个格子ID
            _slotIds = new string[27];
            for (int i = 0; i < 27; i++)
            {
                _slotIds[i] = $"inventory_{i}";
            }

            // 获取原始图片尺寸
            GetOriginalImageSize();

            // 加载背景图片
            InventoryImage.Source = LoadBitmapImage(AssetPaths.Inventory);

            // 计算缩放比例
            CalculateScale();

            // 设置窗口尺寸
            SetWindowSize();

            // 动态创建和设置格子
            SetupSlots();

            LoadSlots();
            PositionWindow();
        }

        /// <summary>
        /// 获取原始图片尺寸
        /// </summary>
        private void GetOriginalImageSize()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.Inventory);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalImageWidth = frame.PixelWidth;
                    _originalImageHeight = frame.PixelHeight;
                }
            }
        }

        /// <summary>
        /// 加载位图图片
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
        /// 根据屏幕分辨率计算缩放比例
        /// </summary>
        private void CalculateScale()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;

            // 基于屏幕宽度与2560的比例，乘以基准倍数6
            _scaleFactor = (screenWidth / BaseScreenWidth) * BaseScaleMultiplier;
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
            // 基于原始图片尺寸计算格子比例
            // 3行9列布局，格子尺寸基于图片宽度和高度
            double slotWidth = _originalImageWidth / 9.0 * _scaleFactor;
            double slotHeight = _originalImageHeight / 3.0 * _scaleFactor;
            double iconSize = Math.Min(slotWidth, slotHeight) * 0.8;

            for (int i = 0; i < 27; i++)
            {
                int row = i / 9;
                int col = i % 9;

                var border = new Border
                {
                    Name = $"Slot{i}",
                    Background = System.Windows.Media.Brushes.Transparent,
                    AllowDrop = true,
                    Width = slotWidth,
                    Height = slotHeight
                };

                var icon = new System.Windows.Controls.Image
                {
                    Name = $"Icon{i}",
                    Stretch = Stretch.Uniform,
                    Visibility = Visibility.Collapsed,
                    Width = iconSize,
                    Height = iconSize
                };

                border.Child = icon;
                border.MouseLeftButtonDown += Slot_Click;
                border.Drop += Slot_Drop;
                border.DragOver += Slot_DragOver;

                Canvas.SetLeft(border, col * slotWidth);
                Canvas.SetTop(border, row * slotHeight);

                SlotCanvas.Children.Add(border);

                // 注册名称以便后续查找
                RegisterName(border.Name, border);
                RegisterName(icon.Name, icon);
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
            for (int i = 0; i < 27; i++)
            {
                var slotId = _slotIds[i];
                var item = _slotService.GetSlot(slotId);

                if (!item.IsEmpty)
                {
                    SetSlotIcon(i, item.FilePath);
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
        /// 获取图标 Image 控件
        /// </summary>
        private System.Windows.Controls.Image? GetIconImage(int index)
        {
            return (System.Windows.Controls.Image?)FindName($"Icon{index}");
        }

        /// <summary>
        /// 获取格子 Border 控件
        /// </summary>
        private Border? GetSlotBorder(int index)
        {
            return (Border?)FindName($"Slot{index}");
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
            for (int i = 0; i < 27; i++)
            {
                if (GetSlotBorder(i) == border)
                    return i;
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
                    _slotService.SetSlot(_slotIds[slotIndex], new Models.SlotItem
                    {
                        FilePath = filePath
                    });

                    // 显示图标
                    SetSlotIcon(slotIndex, filePath);
                }
            }
        }

        /// <summary>
        /// 切换显示/隐藏
        /// </summary>
        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                PositionWindow();
                Show();
            }
        }
    }
}