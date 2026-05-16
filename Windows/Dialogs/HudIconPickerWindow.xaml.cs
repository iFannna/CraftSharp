using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CraftSharp.Helpers;
using CraftSharp.Services;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Dialogs
{
    /// <summary>
    /// HUD 图标选择器窗口（无左侧边栏）
    /// </summary>
    public partial class HudIconPickerWindow : FluentWindow
    {
        /// <summary>
        /// 用户选择的图标样式（如 "full", "hardcore_full", "food_full", "food_full_hunger"）
        /// </summary>
        public string? SelectedIconStyle { get; private set; }

        private readonly string _elementType; // "heart" 或 "food"
        private readonly ObservableCollection<HudIconItem> _iconItems = new();
        private readonly bool _useVerticalLayout;
        private readonly double _scaleFactor;

        // 原生拖放目标（支持 Windows 拖拽缩略图）
        private IDisposable? _nativeDropTarget;

        public HudIconPickerWindow(string elementType)
        {
            InitializeComponent();
            _elementType = elementType;

            // 初始化缩放因子
            ScaleService.Instance.Initialize();
            _scaleFactor = ScaleService.Instance.ScaleFactor;

            // 纵向布局元素类型（经验条、BOSS血条、BOSS等级）
            _useVerticalLayout = elementType == "expbar" || elementType == "boss_bar" || elementType == "boss_bar_notch";

            // 切换布局
            if (_useVerticalLayout)
            {
                GridScrollViewer.Visibility = Visibility.Collapsed;
                VerticalScrollViewer.Visibility = Visibility.Visible;
            }

            // 设置窗口图标
            IconService.Instance.ApplyWindowIcon(this);

            // 注册原生拖放（仅显示缩略图，不接受文件）
            SourceInitialized += (s, e) =>
            {
                try
                {
                    _nativeDropTarget = NativeDropHelper.RegisterForThumbnail(this);
                }
                catch (Exception)
                {
                    _nativeDropTarget?.Dispose();
                    _nativeDropTarget = null;
                }
            };

            // 窗口关闭时释放资源
            Closed += (s, e) =>
            {
                _nativeDropTarget?.Dispose();
                _nativeDropTarget = null;
            };

            // 加载图标
            LoadIcons();
        }

        private void LoadIcons()
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            // 确定图标目录路径（absorbing 使用 heart 目录）
            string spriteDir = _elementType == "absorbing" ? "heart" : _elementType;
            string basePath = $"Assets/minecraft/textures/gui/sprites/hud/{spriteDir}";

            if (_elementType == "heart")
            {
                // 生命值：显示所有 full 图标
                // 可选样式：
                // - 基础：full, hardcore_full
                // - 效果：poisoned_full, withered_full, frozen_full, vehicle_full
                // - 效果+极限：poisoned_hardcore_full, withered_hardcore_full, frozen_hardcore_full（vehicle无hardcore版本）
                // - 伤害吸收：absorbing_full, absorbing_hardcore_full
                var heartStyles = new[]
                {
                    ("full", "普通 / Normal"),
                    ("hardcore_full", "极限模式 / Hardcore"),
                    ("poisoned_full", "中毒 / Poisoned"),
                    ("poisoned_hardcore_full", "中毒极限 / Poisoned Hardcore"),
                    ("withered_full", "凋零 / Withered"),
                    ("withered_hardcore_full", "凋零极限 / Withered Hardcore"),
                    ("frozen_full", "冻结 / Frozen"),
                    ("frozen_hardcore_full", "冻结极限 / Frozen Hardcore"),
                    ("vehicle_full", "载具 / Vehicle"),
                    ("absorbing_full", "伤害吸收 / Absorption"),
                    ("absorbing_hardcore_full", "伤害吸收极限 / Absorption Hardcore")
                };

                foreach (var (style, displayName) in heartStyles)
                {
                    var bitmap = ImageService.Instance.LoadBitmapImage($"{basePath}/{style}.png");
                    if (bitmap != null)
                    {
                        _iconItems.Add(new HudIconItem
                        {
                            IconStyle = style,
                            DisplayName = displayName,
                            BitmapImage = bitmap
                        });
                    }
                }
            }
            else if (_elementType == "food")
            {
                // 饥饿值：food_full 和 food_full_hunger
                var foodStyles = new[]
                {
                    ("food_full", "普通 / Normal"),
                    ("food_full_hunger", "饥饿效果 / Hunger Effect")
                };

                foreach (var (style, displayName) in foodStyles)
                {
                    var bitmap = ImageService.Instance.LoadBitmapImage($"{basePath}/{style}.png");
                    if (bitmap != null)
                    {
                        _iconItems.Add(new HudIconItem
                        {
                            IconStyle = style,
                            DisplayName = displayName,
                            BitmapImage = bitmap
                        });
                    }
                }
            }
            else if (_elementType == "absorbing")
            {
                // 伤害吸收值：absorbing_full 和 absorbing_hardcore_full
                var absorbingStyles = new[]
                {
                    ("absorbing_full", "普通 / Normal"),
                    ("absorbing_hardcore_full", "极限模式 / Hardcore")
                };

                foreach (var (style, displayName) in absorbingStyles)
                {
                    var bitmap = ImageService.Instance.LoadBitmapImage($"{basePath}/{style}.png");
                    if (bitmap != null)
                    {
                        _iconItems.Add(new HudIconItem
                        {
                            IconStyle = style,
                            DisplayName = displayName,
                            BitmapImage = bitmap
                        });
                    }
                }
            }
            else if (_elementType == "armor")
            {
                // 护甲值：只有 armor_full
                var armorStyles = new[]
                {
                    ("armor_full", "普通 / Normal")
                };

                foreach (var (style, displayName) in armorStyles)
                {
                    var bitmap = ImageService.Instance.LoadBitmapImage($"{basePath}/{style}.png");
                    if (bitmap != null)
                    {
                        _iconItems.Add(new HudIconItem
                        {
                            IconStyle = style,
                            DisplayName = displayName,
                            BitmapImage = bitmap
                        });
                    }
                }
            }
            else if (_elementType == "air")
            {
                // 空气值：只有 air
                var airStyles = new[]
                {
                    ("air", "普通 / Normal")
                };

                foreach (var (style, displayName) in airStyles)
                {
                    var bitmap = ImageService.Instance.LoadBitmapImage($"{basePath}/{style}.png");
                    if (bitmap != null)
                    {
                        _iconItems.Add(new HudIconItem
                        {
                            IconStyle = style,
                            DisplayName = displayName,
                            BitmapImage = bitmap
                        });
                    }
                }
            }
            else if (_elementType == "expbar")
            {
                // 经验条：experience_bar_progress 和 jump_bar_progress
                var expbarStyles = new[]
                {
                    ("experience_bar_progress", "经验条 / Experience Bar"),
                    ("jump_bar_progress", "跳跃进度条 / Jump Bar")
                };

                foreach (var (style, displayName) in expbarStyles)
                {
                    var bitmap = ImageService.Instance.LoadBitmapImage($"Assets/minecraft/textures/gui/sprites/hud/experience_bar/{style}.png");
                    if (bitmap != null)
                    {
                        _iconItems.Add(new HudIconItem
                        {
                            IconStyle = style,
                            DisplayName = displayName,
                            BitmapImage = bitmap,
                            MaxWidth = bitmap.PixelWidth * _scaleFactor
                        });
                    }
                }
            }
            else if (_elementType == "boss_bar")
            {
                // BOSS血条颜色样式（不含notches）
                var bossBarStyles = new[]
                {
                    ("blue", "蓝色 / Blue"),
                    ("green", "绿色 / Green"),
                    ("red", "红色 / Red"),
                    ("pink", "粉色 / Pink"),
                    ("purple", "紫色 / Purple"),
                    ("white", "白色 / White"),
                    ("yellow", "黄色 / Yellow")
                };

                foreach (var (style, displayName) in bossBarStyles)
                {
                    var bitmap = ImageService.Instance.LoadBitmapImage($"Assets/minecraft/textures/gui/sprites/hud/boss_bar/{style}_progress.png");
                    if (bitmap != null)
                    {
                        _iconItems.Add(new HudIconItem
                        {
                            IconStyle = style,
                            DisplayName = displayName,
                            BitmapImage = bitmap,
                            MaxWidth = bitmap.PixelWidth * _scaleFactor
                        });
                    }
                }
            }
            else if (_elementType == "boss_bar_notch")
            {
                // BOSS血条分段样式（Notches），第一个选项为"无"
                // "无"选项（空字符串表示无分段）
                _iconItems.Add(new HudIconItem
                {
                    IconStyle = "",
                    DisplayName = "无 / None",
                    BitmapImage = null! // 无图标
                });

                var notchStyles = new[]
                {
                    ("notched_6", "6格 / 6 Notches"),
                    ("notched_10", "10格 / 10 Notches"),
                    ("notched_12", "12格 / 12 Notches"),
                    ("notched_20", "20格 / 20 Notches")
                };

                foreach (var (style, displayName) in notchStyles)
                {
                    var bitmap = ImageService.Instance.LoadBitmapImage($"Assets/minecraft/textures/gui/sprites/hud/boss_bar/{style}_progress.png");
                    if (bitmap != null)
                    {
                        _iconItems.Add(new HudIconItem
                        {
                            IconStyle = style,
                            DisplayName = displayName,
                            BitmapImage = bitmap,
                            MaxWidth = bitmap.PixelWidth * _scaleFactor
                        });
                    }
                }
            }

            // 绑定到对应的 ItemsControl
            if (_useVerticalLayout)
            {
                VerticalIconGrid.ItemsSource = _iconItems;
            }
            else
            {
                IconGrid.ItemsSource = _iconItems;
            }

            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        private void IconItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is HudIconItem icon)
            {
                SelectedIconStyle = icon.IconStyle;
                DialogResult = true;
                Close();
            }
        }
    }

    /// <summary>
    /// HUD 图标项
    /// </summary>
    public class HudIconItem
    {
        public string IconStyle { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public BitmapImage BitmapImage { get; set; } = null!;
        public double MaxWidth { get; set; } = double.PositiveInfinity;
    }
}