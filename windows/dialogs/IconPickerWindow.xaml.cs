using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CraftSharp.Helpers;
using CraftSharp.Services.Core;
using CraftSharp.Services.Hud;
using CraftSharp.Services.Slot;
using CraftSharp.Services.Resource;
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
        private readonly Dictionary<string, List<IconItem>> _iconCache = new();
        private IconCategoriesConfig? _categoryConfig;
        private int _loadGeneration;
        private const int BatchSize = 50;

        // 原生拖放目标（支持 Windows 拖拽缩略图）
        private IDisposable? _nativeDropTarget;

        public IconPickerWindow()
        {
            InitializeComponent();
            _assetsBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

            // 加载分类配置
            LoadCategoryConfig();

            // 注册原生拖放（仅显示缩略图，不接受文件）
            SourceInitialized += (_, _) =>
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
            Closed += (_, _) =>
            {
                _nativeDropTarget?.Dispose();
                _nativeDropTarget = null;
            };

            // 手动触发加载"全部方块"
            LoadIconsForTagAsync("block_all");
        }

        private void LoadCategoryConfig()
        {
            _categoryConfig = new IconCategoriesConfig();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // 加载方块分类
            var blockCategoriesPath = Path.Combine(_assetsBasePath, "minecraft", "textures", "block", "data", "categories.json");
            if (File.Exists(blockCategoriesPath))
            {
                try
                {
                    var json = File.ReadAllText(blockCategoriesPath);
                    var blocks = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, options);
                    if (blocks != null)
                        _categoryConfig.Blocks = blocks;
                }
                catch { }
            }

            // 加载物品分类
            var itemCategoriesPath = Path.Combine(_assetsBasePath, "minecraft", "textures", "item", "data", "categories.json");
            if (File.Exists(itemCategoriesPath))
            {
                try
                {
                    var json = File.ReadAllText(itemCategoriesPath);
                    var items = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, options);
                    if (items != null)
                        _categoryConfig.Items = items;
                }
                catch { }
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
            // 加载代数：快速切换分类时使旧任务失效，避免向列表交错插入
            var generation = ++_loadGeneration;

            // 重置加载提示（可见的一定属于已失效的加载）
            LoadingOverlay.Visibility = Visibility.Collapsed;

            // 先检查缓存
            if (_iconCache.TryGetValue(tag, out var cachedItems))
            {
                // 分批回流：一次性生成上千容器会阻塞UI线程，造成切换分类时的瞬时卡顿
                _iconItems.Clear();
                IconGrid.ItemsSource = _iconItems;

                for (int i = 0; i < cachedItems.Count; i += BatchSize)
                {
                    if (generation != _loadGeneration)
                    {
                        return;
                    }
                    foreach (var item in cachedItems.Skip(i).Take(BatchSize))
                    {
                        _iconItems.Add(item);
                    }
                    await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                }
                return;
            }

            var imageService = ImageService.Instance;

            // 在后台线程收集文件路径
            var iconDataList = await Task.Run(() => CollectIconPaths(tag));
            if (generation != _loadGeneration)
            {
                return;
            }

            // 如果是根节点（返回空列表），不更新显示，保持当前状态
            if (iconDataList.Count == 0)
            {
                return;
            }

            // 显示加载提示
            LoadingOverlay.Visibility = Visibility.Visible;

            // 先挂载再渐进填充
            _iconItems.Clear();
            IconGrid.ItemsSource = _iconItems;

            for (int i = 0; i < iconDataList.Count; i += BatchSize)
            {
                // 解码在线程池完成（冻结的 BitmapImage 可跨线程），UI 线程只做廉价的添加
                var batchItems = await Task.Run(() =>
                {
                    var result = new List<IconItem>(BatchSize);
                    foreach (var data in iconDataList.Skip(i).Take(BatchSize))
                    {
                        var bitmap = imageService.LoadBitmapImageFromPath(data.FilePath);
                        if (bitmap != null)
                        {
                            result.Add(new IconItem
                            {
                                Name = data.Name,
                                BitmapImage = bitmap,
                                RelativePath = data.RelativePath
                            });
                        }
                    }
                    return result;
                });

                if (generation != _loadGeneration)
                {
                    return;
                }

                foreach (var item in batchItems)
                {
                    _iconItems.Add(item);
                }

                // 以 Background 优先级让出调度，鼠标输入与渲染优先于后续批次
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
            }

            // 存入缓存
            _iconCache[tag] = _iconItems.ToList();

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
                                RelativePath = $"assets/minecraft/textures/block/block/{filename}"
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
                                RelativePath = $"assets/minecraft/textures/item/item/{filename}"
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