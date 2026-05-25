using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using CraftSharp.Models;
using CraftSharp.Services.Wallpaper;

namespace CraftSharp.Windows.Settings.Panels.Wallpaper;

public partial class WallpaperPanel : UserControl
{
    private List<WallpaperItem> _wallpapers = new();
    private McBlockPagination? _pagination;
    private int _currentPage = 1;
    private string _currentFilter = "all";
    private string _currentSort = "latest";
    private const int PageSize = 20;
    private bool _isLoading;

    public WallpaperPanel()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadWallpapersAsync(1);
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateColumns(e.NewSize.Width);
    }

    private void UpdateColumns(double width)
    {
        _columnCount = width switch
        {
            > 900 => 5,
            > 700 => 4,
            > 500 => 3,
            _ => 2
        };

        ApplyColumnCount();
    }

    private int _columnCount = 4;

    private void ApplyColumnCount()
    {
        if (WallpaperListBox.ItemsPanel is not System.Windows.Controls.ItemsPanelTemplate)
            return;

        // Find the actual UniformGrid in the visual tree
        var itemsPresenter = FindVisualChild<System.Windows.Controls.ItemsPresenter>(WallpaperListBox);
        if (itemsPresenter == null) return;

        var uniformGrid = FindVisualChild<System.Windows.Controls.Primitives.UniformGrid>(itemsPresenter);
        if (uniformGrid != null)
            uniformGrid.Columns = _columnCount;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;
        int children = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < children; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result) return result;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private async Task LoadWallpapersAsync(int page)
    {
        if (_isLoading) return;
        _isLoading = true;

        WallpaperListBox.Visibility = Visibility.Collapsed;
        ErrorStack.Visibility = Visibility.Collapsed;
        LoadingStack.Visibility = Visibility.Visible;

        try
        {
            var sort = _currentSort;
            var type = _currentFilter == "all" ? null : _currentFilter;
            var response = await McBlockApiClient.Instance.GetWallpapersAsync(page, PageSize, sort, type);

            _wallpapers = response.Data.Wallpapers;
            _pagination = response.Data.Pagination;
            _currentPage = page;

            WallpaperListBox.ItemsSource = _wallpapers;
            WallpaperListBox.Visibility = Visibility.Visible;
            LoadingStack.Visibility = Visibility.Collapsed;

            UpdatePagination();
            UpdateColumns(ActualWidth);

            // 等待布局完成后再加载缩略图，确保容器已生成
            Dispatcher.BeginInvoke(() => LoadThumbnails(),
                System.Windows.Threading.DispatcherPriority.Background);
        }
        catch
        {
            WallpaperListBox.Visibility = Visibility.Collapsed;
            LoadingStack.Visibility = Visibility.Collapsed;
            ErrorStack.Visibility = Visibility.Visible;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async void LoadThumbnails()
    {
        var items = _wallpapers.ToList();
        var images = new (int index, System.Windows.Media.Imaging.BitmapImage? image)[items.Count];

        // 先并行获取所有图片
        await Task.WhenAll(items.Select(async (wp, i) =>
        {
            images[i] = (i, await WallpaperImageCache.Instance.GetAsync(wp.ThumbnailUrl));
        }));

        // 等布局彻底完成
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);

        Dispatcher.Invoke(() =>
        {
            for (int i = 0; i < images.Length; i++)
            {
                var (index, image) = images[i];
                if (image == null) continue;

                var wallpaper = items[index];
                var container = WallpaperListBox.ItemContainerGenerator.ContainerFromIndex(index);
                if (container == null) continue;

                var loadingRing = FindNamedChild<FrameworkElement>(container, "LoadingRing");
                var errorPanel = FindNamedChild<FrameworkElement>(container, "ErrorPanel");
                var thumbnailImage = FindNamedChild<System.Windows.Controls.Image>(container, "ThumbnailImage");
                var dynamicBadge = FindNamedChild<FrameworkElement>(container, "DynamicBadge");

                if (loadingRing != null) loadingRing.Visibility = Visibility.Collapsed;
                if (errorPanel != null) errorPanel.Visibility = Visibility.Collapsed;
                if (thumbnailImage != null)
                {
                    thumbnailImage.Source = image;
                    thumbnailImage.Visibility = Visibility.Visible;
                }
                if (dynamicBadge != null && wallpaper.Type == "dynamic")
                    dynamicBadge.Visibility = Visibility.Visible;
            }
        });
    }

    private static T? FindNamedChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent == null) return null;
        int children = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < children; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T element && element.Name == name)
                return element;
            var result = FindNamedChild<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void UpdatePagination()
    {
        if (_pagination == null) return;

        PrevPageBtn.IsEnabled = _pagination.HasPrev;
        NextPageBtn.IsEnabled = _pagination.HasNext;
        PageInfo.Text = string.Format(
            (string)Application.Current.FindResource("WallpaperPageInfo") ?? "{0} / {1}",
            _pagination.Page, _pagination.TotalPages);
    }

    private void SetFilterAppearance(string activeTag)
    {
        FilterAll.Appearance = activeTag == "all" ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary;
        FilterStatic.Appearance = activeTag == "static" ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary;
        FilterDynamic.Appearance = activeTag == "dynamic" ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary;
    }

    private async void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button btn || btn.Tag is not string tag) return;
        if (tag == _currentFilter) return;

        _currentFilter = tag;
        SetFilterAppearance(tag);
        await LoadWallpapersAsync(1);
    }

    private async void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SortComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string sort) return;
        if (sort == _currentSort) return;

        _currentSort = sort;
        await LoadWallpapersAsync(1);
    }

    private async void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_pagination?.HasPrev == true)
            await LoadWallpapersAsync(_currentPage - 1);
    }

    private async void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_pagination?.HasNext == true)
            await LoadWallpapersAsync(_currentPage + 1);
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadWallpapersAsync(_currentPage);
    }

    private void WallpaperListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (WallpaperListBox.SelectedItem is not WallpaperItem item) return;
        OpenPreview(item);
    }

    private void QuickSetWallpaper_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        var item = FindWallpaperItem(fe);
        if (item == null) return;

        if (item.Type == "dynamic")
        {
            MessageBox.Show(
                (string)Application.Current.FindResource("WallpaperDynamicNotSupported") ?? "Dynamic wallpaper is not supported yet.",
                "Craft#",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var btn = (Wpf.Ui.Controls.Button)sender;
        btn.IsEnabled = false;
        btn.Content = Application.Current.FindResource("WallpaperSetting") ?? "...";

        WallpaperService.Instance.ApplyStaticWallpaper(item,
            onSuccess: _ => Dispatcher.Invoke(() =>
            {
                btn.IsEnabled = true;
                btn.Content = Application.Current.FindResource("WallpaperQuickSet");
                ShowToast((string)Application.Current.FindResource("WallpaperSetSuccess") ?? "Wallpaper set!");
            }),
            onError: msg => Dispatcher.Invoke(() =>
            {
                btn.IsEnabled = true;
                btn.Content = Application.Current.FindResource("WallpaperQuickSet");
                MessageBox.Show(msg, "Craft#", MessageBoxButton.OK, MessageBoxImage.Error);
            }));
    }

    private WallpaperItem? FindWallpaperItem(FrameworkElement element)
    {
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(element);
        while (parent != null)
        {
            if (parent is ContentPresenter cp && cp.Content is WallpaperItem item)
                return item;
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        }
        return null;
    }

    private void OpenPreview(WallpaperItem item)
    {
        var index = _wallpapers.IndexOf(item);
        var window = new WallpaperPreviewWindow(_wallpapers, index);
        window.Owner = Window.GetWindow(this);
        window.Show();
    }

    private void ShowToast(string message)
    {
        Dispatcher.InvokeAsync(async () =>
        {
            var tooltip = new System.Windows.Controls.ToolTip
            {
                Content = message,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Center,
                Background = System.Windows.Media.Brushes.White,
                Foreground = System.Windows.Media.Brushes.Black,
                Padding = new Thickness(16, 8, 16, 8)
            };
            tooltip.IsOpen = true;
            await Task.Delay(2000);
            tooltip.IsOpen = false;
        });
    }
}
