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

    private bool _isInitialized;

    private List<MonitorInfo> _monitors = new();
    private string? _selectedMonitorId;

    public WallpaperPanel()
    {
        InitializeComponent();
        Loaded += OnPanelLoaded;
        IsVisibleChanged += OnPanelVisibleChanged;
        SizeChanged += OnSizeChanged;
    }

    private void OnPanelLoaded(object sender, RoutedEventArgs e)
    {
        // Visibility 可能还是 Collapsed（构造时就添加到 Collapsed 的容器中），
        // 仅在可见时才加载
        if (IsVisible)
            InitializeAndLoad();
    }

    private void OnPanelVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue && !_isInitialized)
            InitializeAndLoad();
        else if ((bool)e.NewValue && _isInitialized && _wallpapers.Count > 0)
        {
            RefreshMonitorBar();
            RefreshThumbnails();
        }
    }

    private async void InitializeAndLoad()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        RefreshMonitorBar();
        await LoadWallpapersAsync(1);
    }

    #region 多显示器选择

    private static WallpaperSettings? WallpaperConfig => (Application.Current as App)?.GetAppSettings()?.Wallpaper;

    private bool IsSpanMode => WallpaperConfig?.Mode == "span";

    /// <summary>
    /// 刷新显示器选择栏（每次面板可见时调用，感知热插拔）
    /// </summary>
    private void RefreshMonitorBar()
    {
        _monitors = MonitorLayoutService.Instance.GetMonitors();
        MonitorBar.Visibility = _monitors.Count >= 2 ? Visibility.Visible : Visibility.Collapsed;
        if (_monitors.Count < 2) return;

        if (_selectedMonitorId == null || _monitors.All(m => m.DevicePath != _selectedMonitorId))
            _selectedMonitorId = _monitors[0].DevicePath;

        UpdateModeButtons();
        BuildMonitorChips();
    }

    private void UpdateModeButtons()
    {
        var span = IsSpanMode;
        ModeIndependent.Appearance = span
            ? Wpf.Ui.Controls.ControlAppearance.Secondary
            : Wpf.Ui.Controls.ControlAppearance.Primary;
        ModeSpan.Appearance = span
            ? Wpf.Ui.Controls.ControlAppearance.Primary
            : Wpf.Ui.Controls.ControlAppearance.Secondary;
    }

    private void BuildMonitorChips()
    {
        MonitorChipPanel.Items.Clear();
        var spanMode = IsSpanMode;
        var primaryMark = (string)Application.Current.FindResource("WallpaperMonitorPrimary");

        foreach (var monitor in _monitors)
        {
            var label = $"{monitor.Index} · {monitor.Width}x{monitor.Height}";
            if (monitor.IsPrimary)
                label += $" ({primaryMark})";

            var chip = new Wpf.Ui.Controls.Button
            {
                Content = label,
                Tag = monitor.DevicePath,
                Appearance = monitor.DevicePath == _selectedMonitorId
                    ? Wpf.Ui.Controls.ControlAppearance.Primary
                    : Wpf.Ui.Controls.ControlAppearance.Secondary,
                Margin = new Thickness(0, 0, 4, 0),
                Padding = new Thickness(16, 6, 16, 6),
                FontSize = (double)FindResource("GlobalFontSizeSmall"),
                IsEnabled = !spanMode
            };
            chip.Click += MonitorChip_Click;
            MonitorChipPanel.Items.Add(chip);
        }
    }

    private async void ModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button btn || btn.Tag is not string tag) return;

        var settings = WallpaperConfig;
        if (settings == null || settings.Mode == tag) return;

        settings.Mode = tag;
        (Application.Current as App)?.SaveSettings();

        UpdateModeButtons();
        BuildMonitorChips();

        // 切模式立即应用该模式下的现有配置
        await WallpaperService.Instance.ApplyLayoutAsync();
    }

    private void MonitorChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button btn || btn.Tag is not string devicePath) return;

        _selectedMonitorId = devicePath;
        BuildMonitorChips();
    }

    /// <summary>
    /// 按当前模式与选中显示器应用壁纸（面板快速设置与预览窗口共用入口）
    /// </summary>
    private async Task ApplySelectedAsync(WallpaperItem wallpaper)
    {
        if (IsSpanMode)
        {
            await WallpaperService.Instance.ApplySpanAsync(wallpaper);
            return;
        }

        var target = _selectedMonitorId ?? _monitors.FirstOrDefault()?.DevicePath;
        if (string.IsNullOrEmpty(target)) return;
        await WallpaperService.Instance.ApplyToMonitorAsync(wallpaper, target);
    }

    #endregion

    private void RefreshThumbnails()
    {
        // 面板已经可见，重新绑定缩略图到容器
        var items = _wallpapers.ToList();
        for (int i = 0; i < items.Count; i++)
        {
            var container = WallpaperListBox.ItemContainerGenerator.ContainerFromIndex(i);
            if (container == null) continue;

            var thumbnailImage = FindNamedChild<System.Windows.Controls.Image>(container, "ThumbnailImage");
            var loadingRing = FindNamedChild<FrameworkElement>(container, "LoadingRing");
            var errorPanel = FindNamedChild<FrameworkElement>(container, "ErrorPanel");
            var dynamicBadge = FindNamedChild<FrameworkElement>(container, "DynamicBadge");
            var wallpaper = items[i];

            // 从缓存获取已加载的图片
            var cached = WallpaperImageCache.Instance.GetFromCache(wallpaper.ThumbnailUrl);
            if (cached != null)
            {
                if (loadingRing != null) loadingRing.Visibility = Visibility.Collapsed;
                if (errorPanel != null) errorPanel.Visibility = Visibility.Collapsed;
                if (thumbnailImage != null)
                {
                    thumbnailImage.Source = cached;
                    thumbnailImage.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (loadingRing != null) loadingRing.Visibility = Visibility.Visible;
                if (thumbnailImage != null) thumbnailImage.Visibility = Visibility.Collapsed;
            }

            if (dynamicBadge != null && wallpaper.Type == "dynamic")
                dynamicBadge.Visibility = Visibility.Visible;
        }
        ApplyCardHeight();
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

        var itemsPresenter = FindVisualChild<System.Windows.Controls.ItemsPresenter>(WallpaperListBox);
        if (itemsPresenter == null) return;

        var uniformGrid = FindVisualChild<System.Windows.Controls.Primitives.UniformGrid>(itemsPresenter);
        if (uniformGrid != null)
            uniformGrid.Columns = _columnCount;

        ApplyCardHeight();
    }

    private void ApplyCardHeight()
    {
        if (WallpaperListBox.ActualWidth <= 0) return;

        var itemMargin = new Thickness(0, 0, 8, 8);
        var itemPadding = new Thickness(4);
        var totalHorizontalMargin = itemMargin.Right * _columnCount;
        var cardWidth = (WallpaperListBox.ActualWidth - totalHorizontalMargin) / _columnCount
                        - itemPadding.Left - itemPadding.Right;
        var cardHeight = cardWidth * 10.0 / 16.0;

        for (int i = 0; i < WallpaperListBox.Items.Count; i++)
        {
            var container = WallpaperListBox.ItemContainerGenerator.ContainerFromIndex(i);
            if (container is FrameworkElement element)
                element.Height = cardHeight + itemPadding.Top + itemPadding.Bottom;
        }
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

        await Task.WhenAll(items.Select(async (wp, i) =>
        {
            images[i] = (i, await WallpaperImageCache.Instance.GetAsync(wp.ThumbnailUrl));
        }));

        // 等待布局完成，最多重试 10 次
        await WaitForItemContainers();

        Dispatcher.Invoke(() =>
        {
            ApplyThumbnails(items, images);
        });
    }

    private async Task WaitForItemContainers()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            if (WallpaperListBox.ItemContainerGenerator.ContainerFromIndex(0) != null)
                return;
            await Task.Delay(30);
        }
    }

    private void ApplyThumbnails(List<WallpaperItem> items, (int index, System.Windows.Media.Imaging.BitmapImage? image)[] images)
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
        ApplyCardHeight();
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

    private void WallpaperListBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // 卡片本体聚焦时 Enter 打开预览（与悬浮层"查看图片"同入口）；
        // 焦点在卡内按钮上时不拦，Enter 触发按钮自身
        if (e.Key != Key.Enter) return;
        if (Keyboard.FocusedElement is not ListBoxItem item) return;
        if (item.Content is WallpaperItem wallpaper)
        {
            OpenPreview(wallpaper);
            e.Handled = true;
        }
    }

    private void ViewImage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        var item = FindWallpaperItem(fe);
        if (item != null) OpenPreview(item);
    }

    private async void QuickSetWallpaper_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        var item = FindWallpaperItem(fe);
        if (item == null) return;

        var btn = (Wpf.Ui.Controls.Button)sender;
        btn.IsEnabled = false;
        btn.Content = Application.Current.FindResource("WallpaperSetting") ?? "...";

        try
        {
            await ApplySelectedAsync(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Craft#", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btn.IsEnabled = true;
            btn.Content = Application.Current.FindResource("WallpaperQuickSet");
        }
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
        var window = new WallpaperPreviewWindow(_wallpapers, index, ApplySelectedAsync);
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
                Background = (System.Windows.Media.Brush)Application.Current.FindResource("CardBackgroundBrush"),
                Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextPrimaryBrush"),
                Padding = new Thickness(16, 8, 16, 8)
            };
            tooltip.IsOpen = true;
            await Task.Delay(2000);
            tooltip.IsOpen = false;
        });
    }
}
