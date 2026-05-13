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
using CraftSharp.Models;

namespace CraftSharp.Windows.Inventory
{
    public partial class InventoryWindow : Window
    {
        private readonly SlotDataService _slotService;
        private readonly string[] _slotIds;
        private readonly AppSettings? _settings;

        private double _scaleFactor;
        private double _originalImageWidth;
        private double _originalImageHeight;

        // 原生拖放目标（支持 Windows 拖拽缩略图）
        private IDisposable? _nativeDropTarget;

        // 灰色蒙版窗口
        private GrayOverlayWindow? _grayOverlayWindow;

        // 状态栏隐藏前的可见性状态
        private bool _statusBarWasVisible = false;

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

            // 生成27个格子ID
            _slotIds = new string[27];
            for (int i = 0; i < 27; i++)
            {
                _slotIds[i] = $"inventory_{i}";
            }

            // 初始化缩放服务
            ScaleService.Instance.Initialize();
            _scaleFactor = ScaleService.Instance.ScaleFactor;

            // 获取原始图片尺寸
            GetOriginalImageSize();

            // 加载背景图片
            InventoryImage.Source = LoadBitmapImage(AssetPaths.Inventory);

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
            var (width, height) = ImageService.Instance.GetImageDimensions(AssetPaths.Inventory);
            _originalImageWidth = width;
            _originalImageHeight = height;
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

                Canvas.SetLeft(border, col * slotWidth);
                Canvas.SetTop(border, row * slotHeight);

                SlotCanvas.Children.Add(border);

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
        /// 显示物品栏（处理灰色蒙版和隐藏状态栏）
        /// </summary>
        private void ShowInventory()
        {
            // 1. 先显示灰色蒙版
            if (_settings?.InventoryWindowGrayOverlay ?? true)
            {
                int opacity = _settings?.InventoryWindowGrayOverlayOpacity ?? 50;
                _grayOverlayWindow = new GrayOverlayWindow(opacity);
                _grayOverlayWindow.Show(); // 先显示蒙版
            }

            // 2. 显示物品栏，设置 Owner 为蒙版窗口，这样物品栏在蒙版之上
            PositionWindow();
            if (_grayOverlayWindow != null)
            {
                Owner = _grayOverlayWindow;
            }
            Show();

            // 3. 隐藏状态栏
            if (_settings?.InventoryWindowHideStatusBar ?? false)
            {
                _statusBarWasVisible = StatusBarService.Instance.IsVisible();
                if (_statusBarWasVisible)
                {
                    StatusBarService.Instance.SetVisible(false);
                }
            }
        }

        /// <summary>
        /// 隐藏物品栏（恢复灰色蒙版和状态栏）
        /// </summary>
        private void HideInventory()
        {
            Hide();
            Owner = null; // 清除 Owner

            // 1. 关闭灰色蒙版
            if (_grayOverlayWindow != null)
            {
                _grayOverlayWindow.Close();
                _grayOverlayWindow = null;
            }

            // 2. 恢复状态栏
            if ((_settings?.InventoryWindowHideStatusBar ?? false) && _statusBarWasVisible)
            {
                StatusBarService.Instance.SetVisible(true);
            }
        }

        /// <summary>
        /// 判断鼠标位置是否可以接受文件放置
        /// </summary>
        private bool CanDropAtPosition(System.Windows.Point screenPoint)
        {
            var mousePos = PointFromScreen(screenPoint);
            return GetSlotIndexAtPosition(mousePos) >= 0;
        }

        /// <summary>
        /// 处理原生拖放回调
        /// </summary>
        private void HandleNativeDrop(IReadOnlyList<string> paths, System.Windows.Point screenPoint)
        {
            if (paths.Count == 0) return;

            var filePath = paths[0];
            var mousePos = PointFromScreen(screenPoint);
            int slotIndex = GetSlotIndexAtPosition(mousePos);

            if (slotIndex >= 0)
            {
                _slotService.SetSlot(_slotIds[slotIndex], new SlotItem { FilePath = filePath });
                SetSlotIcon(slotIndex, filePath);
            }
        }

        /// <summary>
        /// 根据鼠标位置判断落在哪个格子
        /// </summary>
        private int GetSlotIndexAtPosition(System.Windows.Point mousePos)
        {
            double slotWidth = _originalImageWidth / 9.0 * _scaleFactor;
            double slotHeight = _originalImageHeight / 3.0 * _scaleFactor;

            var canvasPos = SlotCanvas.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0));
            double relativeX = mousePos.X - canvasPos.X;
            double relativeY = mousePos.Y - canvasPos.Y;

            int col = (int)(relativeX / slotWidth);
            int row = (int)(relativeY / slotHeight);

            if (col >= 0 && col < 9 && row >= 0 && row < 3)
            {
                return row * 9 + col;
            }

            return -1;
        }
    }
}