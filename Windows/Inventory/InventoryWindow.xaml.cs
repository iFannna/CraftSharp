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

        // 原生拖放目标（支持 Windows 拖拽缩略图）
        private IDisposable? _nativeDropTarget;

        // 灰色蒙版窗口
        private GrayOverlayWindow? _grayOverlayWindow;

        // 状态栏隐藏前的可见性状态
        private bool _statusBarWasVisible = false;

        // 格子坐标数据结构
        public class SlotCoord
        {
            public int x { get; set; }
            public int y { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public string name { get; set; } = "";
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

            // 加载已保存的格子数据
            LoadSlots();

            PositionWindow();
        }

        /// <summary>
        /// 加载格子坐标数据
        /// </summary>
        private void LoadSlotCoords()
        {
            try
            {
                var coordsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inventory_coords.json");
                if (File.Exists(coordsPath))
                {
                    var json = File.ReadAllText(coordsPath);
                    _slotCoords = JsonConvert.DeserializeObject<List<SlotCoord>>(json);
                }
            }
            catch
            {
                // 如果加载失败，使用空列表
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
            double iconSize = slotSize; // 图标占满格子

            foreach (var coord in _slotCoords)
            {
                // 只创建 16x16 的格子（排除 player 区域）
                if (coord.width != 16 || coord.height != 16) continue;

                var slotId = coord.name;

                var border = new Border
                {
                    Name = $"Slot_{slotId}",
                    Background = System.Windows.Media.Brushes.Transparent,
                    Width = slotSize,
                    Height = slotSize,
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                border.MouseLeftButtonDown += Slot_MouseLeftButtonDown;
                border.MouseLeftButtonUp += Slot_MouseLeftButtonUp;

                var icon = new System.Windows.Controls.Image
                {
                    Name = $"Icon_{slotId}",
                    Stretch = Stretch.Uniform,
                    Width = iconSize,
                    Height = iconSize,
                    Visibility = Visibility.Collapsed
                };
                RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);

                border.Child = icon;

                // 使用坐标数据定位
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
            foreach (var slotId in _slotBorders.Keys)
            {
                var item = _slotService.GetSlot(slotId);
                if (!item.IsEmpty)
                {
                    SetSlotIcon(slotId, item.FilePath);
                }
            }
        }

        /// <summary>
        /// 设置格子图标
        /// </summary>
        private void SetSlotIcon(string slotId, string filePath)
        {
            if (!_slotIcons.TryGetValue(slotId, out var icon)) return;

            var iconSource = IconExtractor.GetIcon(filePath, (int)(32 * _scaleFactor));
            if (iconSource != null)
            {
                icon.Source = iconSource;
                icon.Visibility = Visibility.Visible;
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

        private void Slot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 只记录点击位置，不做其他处理
            e.Handled = true;
        }

        private void Slot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            var slotId = border.Name.Replace("Slot_", "");

            var item = _slotService.GetSlot(slotId);
            if (!item.IsEmpty)
            {
                OpenFile(item.FilePath);
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
                _slotService.SetSlot(slotId, new SlotItem { FilePath = filePath });
                SetSlotIcon(slotId, filePath);

                // 如果涉及 hotbar 格子，通知 StatusBarService 刷新
                if (slotId.StartsWith("hotbar_"))
                {
                    StatusBarService.Instance.RefreshHotbarIcons();
                }
            }
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

        // ==================== 显示/隐藏 ====================

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
            if (_settings?.Inventory.GrayOverlay ?? true)
            {
                int opacity = _settings?.Inventory.GrayOverlayOpacity ?? 75;
                _grayOverlayWindow = new GrayOverlayWindow(opacity);
                _grayOverlayWindow.Show();
            }

            // 2. 显示物品栏，设置 Owner 为蒙版窗口
            PositionWindow();
            if (_grayOverlayWindow != null)
            {
                Owner = _grayOverlayWindow;
            }
            Show();

            // 3. 隐藏状态栏
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
        /// 隐藏物品栏（恢复灰色蒙版和状态栏）
        /// </summary>
        private void HideInventory()
        {
            Hide();
            Owner = null;

            // 1. 关闭灰色蒙版
            if (_grayOverlayWindow != null)
            {
                _grayOverlayWindow.Close();
                _grayOverlayWindow = null;
            }

            // 2. 恢复状态栏
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