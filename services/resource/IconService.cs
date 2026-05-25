using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;

namespace CraftSharp.Services.Resource
{
    public class IconService
    {
        private static IconService? _instance;
        public static IconService Instance => _instance ??= new IconService();

        private string? _currentIconPath;
        private TaskbarIcon? _taskbarIcon;

        private string IcoPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "craftsharp.ico");

        public void InitializeForTaskbarIcon(string? iconPath, TaskbarIcon taskbarIcon)
        {
            _taskbarIcon = taskbarIcon;
            SetAppIcon(iconPath);
        }

        public void SetAppIcon(string? relativePath)
        {
            _currentIconPath = relativePath;

            if (string.IsNullOrEmpty(relativePath))
            {
                // 未设置时使用 craftsharp.ico
                SetTrayIconFromIco();
                return;
            }

            var fullPath = relativePath.StartsWith("assets/")
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", relativePath);

            if (!File.Exists(fullPath)) return;

            UpdateNotifyIcon(fullPath);
        }

        private void SetTrayIconFromIco()
        {
            if (_taskbarIcon == null) return;

            try
            {
                var icon = new System.Drawing.Icon(IcoPath);
                _taskbarIcon.IconSource = icon.ToImageSource();
            }
            catch { }
        }

        private void UpdateNotifyIcon(string pngPath)
        {
            if (_taskbarIcon == null) return;

            try
            {
                var originalBitmap = new BitmapImage();
                originalBitmap.BeginInit();
                originalBitmap.UriSource = new Uri(pngPath, UriKind.Absolute);
                originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
                originalBitmap.EndInit();
                originalBitmap.Freeze();

                ImageSource trayImage;
                if (originalBitmap.PixelWidth < 32 || originalBitmap.PixelHeight < 32)
                {
                    var scale = 32.0 / Math.Max(originalBitmap.PixelWidth, originalBitmap.PixelHeight);
                    trayImage = new TransformedBitmap(originalBitmap, new ScaleTransform(scale, scale));
                    ((TransformedBitmap)trayImage).Freeze();
                }
                else if (originalBitmap.PixelWidth > 32 || originalBitmap.PixelHeight > 32)
                {
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

        public string? GetCurrentIconPath() => _currentIconPath;

        public ImageSource? GetIconPreview(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                try
                {
                    var icon = new System.Drawing.Icon(IcoPath);
                    return icon.ToImageSource();
                }
                catch { return null; }
            }

            var fullPath = relativePath.StartsWith("assets/")
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", relativePath);

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
