using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CraftSharp.Services;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Dialogs
{
    /// <summary>
    /// 图标选择器窗口
    /// </summary>
    public partial class IconPickerWindow : FluentWindow
    {
        /// <summary>
        /// 用户选择的图标路径（相对于Assets目录）
        /// </summary>
        public string? SelectedIconPath { get; private set; }

        /// <summary>
        /// 用户选择的图标完整路径
        /// </summary>
        public string? SelectedIconFullPath { get; private set; }

        private readonly string _assetsBasePath;
        private readonly ObservableCollection<IconItem> _iconItems = new();
        private IconCategoriesConfig? _categoryConfig;

        public IconPickerWindow()
        {
            InitializeComponent();
            _assetsBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

            // 加载分类配置
            LoadCategoryConfig();

            // 设置窗口图标（使用当前应用图标）
            SetWindowIcon();

            // 手动触发加载"全部方块"
            LoadIconsForTagAsync("block_all");
        }

        private void LoadCategoryConfig()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "icon_categories.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    _categoryConfig = JsonSerializer.Deserialize<IconCategoriesConfig>(json, options);
                }
                catch { }
            }
        }

        private void SetWindowIcon()
        {
            var icon = IconService.Instance.GetWindowIcon();
            if (icon != null)
            {
                this.Icon = icon;
            }
        }

        private void CategoryTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is System.Windows.Controls.TreeViewItem item && item.Tag is string tag)
            {
                LoadIconsForTagAsync(tag);
            }
        }

        private void TreeViewHeader_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 获取点击的Border，找到其父级TreeViewItem
            if (sender is Border border)
            {
                var treeViewItem = FindParent<System.Windows.Controls.TreeViewItem>(border);
                if (treeViewItem != null && treeViewItem.HasItems)
                {
                    // 切换展开状态
                    treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
                    e.Handled = true;
                }
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T result)
                    return result;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private async void LoadIconsForTagAsync(string tag)
        {
            // 在后台线程收集文件路径
            var iconDataList = await Task.Run(() => CollectIconPaths(tag));

            // 如果是根节点（返回空列表），不更新显示，保持当前状态
            if (iconDataList.Count == 0)
            {
                return;
            }

            // 显示加载提示
            LoadingOverlay.Visibility = Visibility.Visible;

            // 在UI线程分批创建BitmapImage
            _iconItems.Clear();
            const int batchSize = 50;

            for (int i = 0; i < iconDataList.Count; i += batchSize)
            {
                var batch = iconDataList.Skip(i).Take(batchSize);
                foreach (var data in batch)
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(data.FilePath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        _iconItems.Add(new IconItem
                        {
                            Name = data.Name,
                            BitmapImage = bitmap,
                            RelativePath = data.RelativePath
                        });
                    }
                    catch { }
                }

                // 让UI有机会更新
                await Task.Delay(1);
            }

            IconGrid.ItemsSource = _iconItems;

            // 隐藏加载提示
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        private List<IconData> CollectIconPaths(string tag)
        {
            var result = new List<IconData>();

            if (_categoryConfig == null)
                return result;

            string basePath;

            if (tag.StartsWith("block"))
            {
                basePath = Path.Combine(_assetsBasePath, "minecraft", "textures", "block", "block");

                if (tag == "block_root")
                    return result; // 根节点不加载，保持当前显示

                // 解析分类：block_all 或 block/{category}
                string? category = null;
                if (tag != "block_all")
                {
                    var parts = tag.Split('/');
                    if (parts.Length > 1)
                        category = parts[1];
                }

                // 从配置中筛选
                foreach (var (filename, categories) in _categoryConfig.Blocks)
                {
                    // block_all 包含所有方块，否则需要匹配分类
                    if (category == null || categories.Contains(category))
                    {
                        var fullPath = Path.Combine(basePath, filename);
                        if (File.Exists(fullPath))
                        {
                            result.Add(new IconData
                            {
                                FilePath = fullPath,
                                Name = Path.GetFileNameWithoutExtension(filename),
                                RelativePath = $"Assets/minecraft/textures/block/block/{filename}"
                            });
                        }
                    }
                }
            }
            else if (tag.StartsWith("item"))
            {
                basePath = Path.Combine(_assetsBasePath, "minecraft", "textures", "item", "item");

                if (tag == "item_root")
                    return result; // 根节点不加载，保持当前显示

                // 解析分类：item_all 或 item/{category}
                string? category = null;
                if (tag != "item_all")
                {
                    var parts = tag.Split('/');
                    if (parts.Length > 1)
                        category = parts[1];
                }

                // 从配置中筛选
                foreach (var (filename, categories) in _categoryConfig.Items)
                {
                    // item_all 包含所有物品，否则需要匹配分类
                    if (category == null || categories.Contains(category))
                    {
                        var fullPath = Path.Combine(basePath, filename);
                        if (File.Exists(fullPath))
                        {
                            result.Add(new IconData
                            {
                                FilePath = fullPath,
                                Name = Path.GetFileNameWithoutExtension(filename),
                                RelativePath = $"Assets/minecraft/textures/item/item/{filename}"
                            });
                        }
                    }
                }
            }

            // 按名称排序
            return result.OrderBy(d => d.Name).ToList();
        }

        private record IconData
        {
            public string FilePath { get; init; } = "";
            public string Name { get; init; } = "";
            public string RelativePath { get; init; } = "";
        }

        private void IconItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is IconItem icon)
            {
                SelectedIconPath = icon.RelativePath;
                SelectedIconFullPath = icon.BitmapImage.UriSource.LocalPath;
                DialogResult = true;
                Close();
            }
        }
    }

    /// <summary>
    /// 图标项
    /// </summary>
    public class IconItem
    {
        public string Name { get; set; } = "";
        public BitmapImage BitmapImage { get; set; } = null!;
        public string RelativePath { get; set; } = "";
    }

    /// <summary>
    /// 图标分类配置结构
    /// </summary>
    public class IconCategoriesConfig
    {
        public Dictionary<string, List<string>> Blocks { get; set; } = new();
        public Dictionary<string, List<string>> Items { get; set; } = new();
    }
}