using System.IO;
using System.Windows;
using System.Windows.Input;
using CraftSharp.Models;
using CraftSharp.Services.Wallpaper;
using Microsoft.Win32;

namespace CraftSharp.Windows.Settings.Panels.Wallpaper;

public partial class WallpaperPreviewWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly List<WallpaperItem> _wallpapers;
    private readonly Dictionary<string, string> _originalUrls = [];
    private readonly Func<WallpaperItem, Task> _applyHandler;
    private int _currentIndex;
    private readonly double[] _zoomLevels = { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0 };
    private int _zoomIndex;
    private bool _isDragging;
    private Point _dragStart;

    public WallpaperPreviewWindow(List<WallpaperItem> wallpapers, int selectedIndex,
        Func<WallpaperItem, Task> applyHandler)
    {
        InitializeComponent();

        _wallpapers = wallpapers;
        _currentIndex = selectedIndex;
        _applyHandler = applyHandler;

        Width = SystemParameters.PrimaryScreenWidth * 0.8;
        Height = SystemParameters.PrimaryScreenHeight * 0.8;

        KeyDown += (_, e) =>
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.Left:
                    NavigatePrev();
                    break;
                case Key.Right:
                    NavigateNext();
                    break;
            }
        };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadCurrentImage();
    }

    private async Task LoadCurrentImage()
    {
        if (_currentIndex < 0 || _currentIndex >= _wallpapers.Count) return;

        var wallpaper = _wallpapers[_currentIndex];
        PreviewImage.Source = null;
        ResolutionText.Text = "";
        LoadingRing.Visibility = Visibility.Visible;

        UpdateNavigationButtons();
        ResetZoom();

        var imageUrl = await GetOriginalUrlAsync(wallpaper);
        var image = await WallpaperImageCache.Instance.GetAsync(imageUrl);
        LoadingRing.Visibility = Visibility.Collapsed;
        if (image != null)
        {
            PreviewImage.Source = image;
            ResolutionText.Text = $"{image.PixelWidth}×{image.PixelHeight}";
        }
    }

    private async Task<string> GetOriginalUrlAsync(WallpaperItem wallpaper)
    {
        if (_originalUrls.TryGetValue(wallpaper.Id, out var url))
            return url;

        url = await WallpaperService.Instance.GetOriginalUrlAsync(wallpaper);
        _originalUrls[wallpaper.Id] = url;
        return url;
    }

    private void UpdateNavigationButtons()
    {
        PrevBtn.IsEnabled = _currentIndex > 0;
        NextBtn.IsEnabled = _currentIndex < _wallpapers.Count - 1;
    }

    private void NavigatePrev()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            _ = LoadCurrentImage();
        }
    }

    private void NavigateNext()
    {
        if (_currentIndex < _wallpapers.Count - 1)
        {
            _currentIndex++;
            _ = LoadCurrentImage();
        }
    }

    private void ZoomIn()
    {
        if (_zoomIndex < _zoomLevels.Length - 1)
        {
            _zoomIndex++;
            ApplyZoom();
        }
    }

    private void ZoomOut()
    {
        if (_zoomIndex > 0)
        {
            _zoomIndex--;
            ApplyZoom();
        }
    }

    private void ResetZoom()
    {
        _zoomIndex = 3;
        ApplyZoom();
        ImageTranslate.X = 0;
        ImageTranslate.Y = 0;
    }

    private void ApplyZoom()
    {
        var scale = _zoomLevels[_zoomIndex];
        ImageScale.ScaleX = scale;
        ImageScale.ScaleY = scale;

        if (scale <= 1.0)
        {
            ImageTranslate.X = 0;
            ImageTranslate.Y = 0;
        }
    }

    private void PrevBtn_Click(object sender, RoutedEventArgs e) => NavigatePrev();
    private void NextBtn_Click(object sender, RoutedEventArgs e) => NavigateNext();
    private void ZoomInBtn_Click(object sender, RoutedEventArgs e) => ZoomIn();
    private void ZoomOutBtn_Click(object sender, RoutedEventArgs e) => ZoomOut();
    private void FitBtn_Click(object sender, RoutedEventArgs e) => ResetZoom();

    private void RootGrid_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
            ZoomIn();
        else
            ZoomOut();
    }

    private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_zoomLevels[_zoomIndex] > 1.0)
        {
            _isDragging = true;
            _dragStart = e.GetPosition(RootGrid);
            RootGrid.CaptureMouse();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(RootGrid);
            var dx = pos.X - _dragStart.X;
            var dy = pos.Y - _dragStart.Y;

            ImageTranslate.X += dx;
            ImageTranslate.Y += dy;

            _dragStart = pos;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        _isDragging = false;
        RootGrid.ReleaseMouseCapture();
    }

    private async void SetWallpaperBtn_Click(object sender, RoutedEventArgs e)
    {
        var wallpaper = _wallpapers[_currentIndex];
        SetWallpaperBtn.IsEnabled = false;

        try
        {
            await _applyHandler(wallpaper);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Craft#", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetWallpaperBtn.IsEnabled = true;
        }
    }

    private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
    {
        var wallpaper = _wallpapers[_currentIndex];

        var dialog = new SaveFileDialog
        {
            FileName = wallpaper.Title,
            DefaultExt = ".png",
            Filter = FindResource("PngFileFilter") as string ?? "PNG 图片|*.png"
        };

        if (dialog.ShowDialog() != true)
            return;

        DownloadBtn.IsEnabled = false;

        try
        {
            var imageUrl = await GetOriginalUrlAsync(wallpaper);
            var bytes = await WallpaperService.Instance.DownloadBytesAsync(imageUrl);
            await File.WriteAllBytesAsync(dialog.FileName, bytes);
        }
        finally
        {
            Dispatcher.Invoke(() => { DownloadBtn.IsEnabled = true; });
        }
    }
}
