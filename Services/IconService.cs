using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;

namespace CraftSharp.Services
{
    /// <summary>
    /// 应用图标服务 - 处理图标切换、窗口图标、托盘图标更新
    /// </summary>
    public class IconService
    {
        private static IconService? _instance;
        public static IconService Instance => _instance ??= new IconService();

        private string? _currentIconPath;
        private TaskbarIcon? _taskbarIcon;
        private Window? _settingsWindow;

        /// <summary>
        /// 初始化图标服务（使用 TaskbarIcon）
        /// </summary>
        public void InitializeForTaskbarIcon(string iconPath, TaskbarIcon taskbarIcon, Window settingsWindow)
        {
            _taskbarIcon = taskbarIcon;
            _settingsWindow = settingsWindow;
            SetAppIcon(iconPath);
        }

        /// <summary>
        /// 设置应用图标
        /// </summary>
        public void SetAppIcon(string relativePath)
        {
            _currentIconPath = relativePath;

            // 构建完整路径
            var fullPath = relativePath.StartsWith("Assets/")
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", relativePath);

            // 检查文件是否存在
            if (!File.Exists(fullPath))
            {
                // 回退到默认图标
                fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "minecraft", "textures", "block", "block", "glass.png");
                if (!File.Exists(fullPath)) return;
            }

            // 更新窗口图标（使用WPF BitmapImage，颜色正确）
            UpdateWindowIconFromPng(fullPath);

            // 更新托盘图标（直接使用PNG，避免Icon转换损失）
            UpdateNotifyIcon(fullPath);
        }

        /// <summary>
        /// 直接使用PNG作为窗口图标（使用最近邻插值放大，保持像素风格清晰）
        /// </summary>
        private void UpdateWindowIconFromPng(string pngPath)
        {
            if (_settingsWindow == null) return;

            try
            {
                // 加载原图
                var originalBitmap = new BitmapImage();
                originalBitmap.BeginInit();
                originalBitmap.UriSource = new Uri(pngPath, UriKind.Absolute);
                originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
                originalBitmap.EndInit();
                originalBitmap.Freeze();

                // 任务栏需要多种尺寸：16, 24, 32, 48, 64, 128, 256（高DPI）
                // 对于小图标，放大到 256x256 以适配所有 DPI
                const int targetSize = 256;

                if (originalBitmap.PixelWidth < targetSize || originalBitmap.PixelHeight < targetSize)
                {
                    var scaledBitmap = ScaleWithNearestNeighbor(originalBitmap, targetSize);
                    _settingsWindow.Icon = scaledBitmap;
                }
                else
                {
                    _settingsWindow.Icon = originalBitmap;
                }
            }
            catch { }
        }

        /// <summary>
        /// 使用最近邻插值缩放图像到指定尺寸（保持像素风格清晰）
        /// </summary>
        private BitmapSource ScaleWithNearestNeighbor(BitmapSource source, int targetSize)
        {
            var scale = (double)targetSize / Math.Max(source.PixelWidth, source.PixelHeight);
            var newWidth = (int)(source.PixelWidth * scale);
            var newHeight = (int)(source.PixelHeight * scale);

            var visual = new DrawingVisual();
            // 关键：设置最近邻插值模式
            RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.NearestNeighbor);

            using (var dc = visual.RenderOpen())
            {
                dc.DrawImage(source, new Rect(0, 0, newWidth, newHeight));
            }

            var renderTarget = new RenderTargetBitmap(newWidth, newHeight, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(visual);
            renderTarget.Freeze();

            return renderTarget;
        }

        /// <summary>
        /// 更新托盘图标（直接使用PNG，避免Icon转换损失）
        /// </summary>
        private void UpdateNotifyIcon(string pngPath)
        {
            if (_taskbarIcon == null) return;

            try
            {
                // 托盘图标标准尺寸：16x16 或 32x32（高DPI）
                // 使用 32x32 适配大多数显示器
                var originalBitmap = new BitmapImage();
                originalBitmap.BeginInit();
                originalBitmap.UriSource = new Uri(pngPath, UriKind.Absolute);
                originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
                originalBitmap.EndInit();
                originalBitmap.Freeze();

                // 如果原图小于 32x32，用像素风格放大
                ImageSource trayImage;
                if (originalBitmap.PixelWidth < 32 || originalBitmap.PixelHeight < 32)
                {
                    var scale = 32.0 / Math.Max(originalBitmap.PixelWidth, originalBitmap.PixelHeight);
                    trayImage = new TransformedBitmap(originalBitmap, new ScaleTransform(scale, scale));
                    ((TransformedBitmap)trayImage).Freeze();
                }
                else if (originalBitmap.PixelWidth > 32 || originalBitmap.PixelHeight > 32)
                {
                    // 如果原图大于 32x32，缩放到 32x32
                    trayImage = new TransformedBitmap(originalBitmap, new ScaleTransform(32.0 / originalBitmap.PixelWidth, 32.0 / originalBitmap.PixelHeight));
                    ((TransformedBitmap)trayImage).Freeze();
                }
                else
                {
                    trayImage = originalBitmap;
                }

                _taskbarIcon.IconSource = trayImage;
            }
            catch { }
        }

        /// <summary>
        /// 获取当前图标路径
        /// </summary>
        public string GetCurrentIconPath() => _currentIconPath ?? "minecraft/textures/block/block/glass.png";

        /// <summary>
        /// 获取放大后的窗口图标（用于子窗口）
        /// </summary>
        public ImageSource? GetWindowIcon()
        {
            try
            {
                var iconPath = GetCurrentIconPath();
                var fullPath = iconPath.StartsWith("Assets/")
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconPath)
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", iconPath);

                if (!File.Exists(fullPath))
                {
                    fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "minecraft", "textures", "block", "block", "glass.png");
                }

                if (!File.Exists(fullPath)) return null;

                var originalBitmap = new BitmapImage();
                originalBitmap.BeginInit();
                originalBitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
                originalBitmap.EndInit();
                originalBitmap.Freeze();

                const int targetSize = 256;
                if (originalBitmap.PixelWidth < targetSize || originalBitmap.PixelHeight < targetSize)
                {
                    return ScaleWithNearestNeighbor(originalBitmap, targetSize);
                }
                return originalBitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取图标预览用的BitmapImage
        /// </summary>
        public BitmapImage? GetIconPreview(string relativePath)
        {
            var fullPath = relativePath.StartsWith("Assets/")
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", relativePath);

            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "minecraft", "textures", "block", "block", "glass.png");
            }

            if (!File.Exists(fullPath)) return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}