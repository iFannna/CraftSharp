using System.Drawing;
using System.Drawing.Imaging;
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
        private System.Drawing.Bitmap? _trayBitmap; // 保持托盘图标bitmap不被GC回收

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

            // 更新托盘图标（使用System.Drawing.Icon）
            var trayIcon = CreateIconFromPng(fullPath);
            if (trayIcon != null)
            {
                UpdateNotifyIcon(trayIcon);
            }
        }

        /// <summary>
        /// 直接使用PNG作为窗口图标（颜色正确，放大显示）
        /// </summary>
        private void UpdateWindowIconFromPng(string pngPath)
        {
            if (_settingsWindow == null) return;

            try
            {
                // 先加载原图
                var originalBitmap = new BitmapImage();
                originalBitmap.BeginInit();
                originalBitmap.UriSource = new Uri(pngPath, UriKind.Absolute);
                originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
                originalBitmap.EndInit();
                originalBitmap.Freeze();

                // 如果图标太小（小于64x64），放大显示
                if (originalBitmap.PixelWidth < 64 || originalBitmap.PixelHeight < 64)
                {
                    var scaledBitmap = new TransformedBitmap(originalBitmap, new ScaleTransform(4, 4));
                    scaledBitmap.Freeze();
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
        /// 从PNG创建Icon（用于托盘和通知）
        /// </summary>
        private Icon CreateIconFromPng(string pngPath)
        {
            try
            {
                // 直接用System.Drawing.Bitmap加载PNG
                using (var originalBitmap = new System.Drawing.Bitmap(pngPath))
                {
                    // 放大到托盘需要的尺寸
                    _trayBitmap = new System.Drawing.Bitmap(48, 48, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (var g = System.Drawing.Graphics.FromImage(_trayBitmap))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.DrawImage(originalBitmap, 0, 0, 48, 48);
                    }

                    var hIcon = _trayBitmap.GetHicon();
                    return Icon.FromHandle(hIcon);
                }
            }
            catch
            {
                return null!;
            }
        }

        /// <summary>
        /// 更新托盘图标（使用 TaskbarIcon）
        /// </summary>
        private void UpdateNotifyIcon(Icon icon)
        {
            if (_taskbarIcon == null) return;
            // TaskbarIcon 使用 ImageSource
            _taskbarIcon.IconSource = icon.ToImageSource();
        }

        /// <summary>
        /// 获取当前图标路径
        /// </summary>
        public string GetCurrentIconPath() => _currentIconPath ?? "minecraft/textures/block/block/glass.png";

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