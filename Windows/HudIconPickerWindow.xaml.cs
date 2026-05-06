using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CraftSharp.Services;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows
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

        public HudIconPickerWindow(string elementType)
        {
            InitializeComponent();
            _elementType = elementType;

            // 设置窗口图标
            SetWindowIcon();

            // 加载图标
            LoadIcons();
        }

        private void SetWindowIcon()
        {
            var icon = IconService.Instance.GetWindowIcon();
            if (icon != null)
            {
                this.Icon = icon;
            }
        }

        private void LoadIcons()
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            // 确定图标目录路径（absorbing 使用 heart 目录）
            string spriteDir = _elementType == "absorbing" ? "heart" : _elementType;
            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "minecraft", "textures", "gui", "sprites", spriteDir);

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
                    ("full", "普通"),
                    ("hardcore_full", "极限模式"),
                    ("poisoned_full", "中毒"),
                    ("poisoned_hardcore_full", "中毒极限"),
                    ("withered_full", "凋零"),
                    ("withered_hardcore_full", "凋零极限"),
                    ("frozen_full", "冻结"),
                    ("frozen_hardcore_full", "冻结极限"),
                    ("vehicle_full", "载具"),
                    ("absorbing_full", "伤害吸收"),
                    ("absorbing_hardcore_full", "伤害吸收极限")
                };

                foreach (var (style, displayName) in heartStyles)
                {
                    var filename = $"{style}.png";
                    var fullPath = Path.Combine(basePath, filename);
                    if (File.Exists(fullPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

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
                    ("food_full", "普通"),
                    ("food_full_hunger", "饥饿效果")
                };

                foreach (var (style, displayName) in foodStyles)
                {
                    var filename = $"{style}.png";
                    var fullPath = Path.Combine(basePath, filename);
                    if (File.Exists(fullPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

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
                    ("absorbing_full", "普通"),
                    ("absorbing_hardcore_full", "极限模式")
                };

                foreach (var (style, displayName) in absorbingStyles)
                {
                    var filename = $"{style}.png";
                    var fullPath = Path.Combine(basePath, filename);
                    if (File.Exists(fullPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

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
                    ("armor_full", "普通")
                };

                foreach (var (style, displayName) in armorStyles)
                {
                    var filename = $"{style}.png";
                    var fullPath = Path.Combine(basePath, filename);
                    if (File.Exists(fullPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

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
                    ("air", "普通")
                };

                foreach (var (style, displayName) in airStyles)
                {
                    var filename = $"{style}.png";
                    var fullPath = Path.Combine(basePath, filename);
                    if (File.Exists(fullPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        _iconItems.Add(new HudIconItem
                        {
                            IconStyle = style,
                            DisplayName = displayName,
                            BitmapImage = bitmap
                        });
                    }
                }
            }

            IconGrid.ItemsSource = _iconItems;
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
    }
}