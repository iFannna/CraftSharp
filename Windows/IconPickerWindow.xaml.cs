using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows
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

        public IconPickerWindow()
        {
            InitializeComponent();
            _assetsBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

            // 手动触发加载"全部方块"
            LoadIconsForTagAsync("block_all");
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

            string basePath;
            List<string> subDirs = new();

            if (tag.StartsWith("block"))
            {
                basePath = Path.Combine(_assetsBasePath, "minecraft", "textures", "block");
                if (tag == "block_root")
                {
                    return result; // 根节点不加载，保持当前显示
                }
                else if (tag == "block_all")
                {
                    subDirs.Add("block");
                }
                else
                {
                    var parts = tag.Split('/');
                    if (parts.Length > 1)
                    {
                        subDirs.Add(parts[1]);
                    }
                }
            }
            else if (tag.StartsWith("item"))
            {
                basePath = Path.Combine(_assetsBasePath, "minecraft", "textures", "item");
                if (tag == "item_root")
                {
                    return result; // 根节点不加载，保持当前显示
                }
                else if (tag == "item_all")
                {
                    subDirs.Add("item");
                }
                else
                {
                    var parts = tag.Split('/');
                    if (parts.Length > 1)
                    {
                        subDirs.Add(parts[1]);
                    }
                }
            }
            else
            {
                return result;
            }

            foreach (var subDir in subDirs)
            {
                var fullPath = Path.Combine(basePath, subDir);
                if (!Directory.Exists(fullPath)) continue;

                var pngFiles = Directory.GetFiles(fullPath, "*.png");
                foreach (var pngPath in pngFiles.OrderBy(f => Path.GetFileNameWithoutExtension(f)))
                {
                    result.Add(new IconData
                    {
                        FilePath = pngPath,
                        Name = Path.GetFileNameWithoutExtension(pngPath),
                        RelativePath = $"Assets/minecraft/textures/{(tag.StartsWith("block") ? "block" : "item")}/{subDir}/{Path.GetFileName(pngPath)}"
                    });
                }
            }

            return result;
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
}