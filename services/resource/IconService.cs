using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CraftSharp.Helpers;
using Hardcodet.Wpf.TaskbarNotification;

namespace CraftSharp.Services.Resource
{
    public class IconService
    {
        private static IconService? _instance;
        public static IconService Instance => _instance ??= new IconService();

        private string? _currentIconPath;
        private TaskbarIcon? _taskbarIcon;
        private IntPtr _appliedIconHandle;
        private IntPtr _trayIconHandle;
        private const int WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

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
                // 未设置时使用 craftsharp.ico：正常显示，不套用像素图管线
                ResetToDefaultIcon();
                return;
            }

            var fullPath = relativePath.StartsWith("assets/")
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath)
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", relativePath);

            if (!File.Exists(fullPath)) return;

            var bitmap = LoadImageSource(fullPath);
            if (bitmap == null) return;

            UpdateNotifyIcon(bitmap);
            ApplyWindowIcons(bitmap);
        }

        /// <summary>
        /// 直接以 WM_SETICON 写入精确物理像素的 HICON：任务栏按钮读窗口小图标，
        /// 而 WPF 的 Window.Icon 按 DIP 换算重采样会引入缩放模糊
        /// </summary>
        private void ApplyWindowIcons(BitmapSource? source)
        {
            if (source == null) return;

            var rendered = RenderNearestNeighbor(source, GetTaskbarIconSize());
            var newHandle = CreateHIcon(rendered);
            if (newHandle == IntPtr.Zero) return;

            foreach (Window window in Application.Current.Windows)
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) continue;
                SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, newHandle);
                SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, newHandle);
            }

            // 窗口不再引用旧句柄后销毁，避免 GDI 句柄泄漏
            if (_appliedIconHandle != IntPtr.Zero)
            {
                DestroyIcon(_appliedIconHandle);
            }
            _appliedIconHandle = newHandle;
        }

        /// <summary>
        /// 将当前应用图标补投到指定窗口：启动早期窗口尚无 HWND，
        /// WM_SETICON 会被跳过，需在窗口 Loaded 时重新应用
        /// </summary>
        public void ApplyIconToWindow(Window window)
        {
            if (_appliedIconHandle == IntPtr.Zero) return;
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, _appliedIconHandle);
            SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, _appliedIconHandle);
        }

        /// <summary>
        /// 最近邻缩放到目标尺寸（无滤镜放大，保持像素图锐利），返回 Bgra32 像素数据
        /// </summary>
        private static (byte[] Pixels, int Width, int Height) RenderNearestNeighbor(BitmapSource source, int target)
        {
            BitmapSource bgra = source.Format == PixelFormats.Bgra32
                ? source
                : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            int sw = bgra.PixelWidth, sh = bgra.PixelHeight;
            var src = new byte[sw * sh * 4];
            bgra.CopyPixels(src, sw * 4, 0);

            double scale = (double)target / Math.Max(sw, sh);
            int tw = Math.Max(1, (int)Math.Round(sw * scale));
            int th = Math.Max(1, (int)Math.Round(sh * scale));
            var dst = new byte[tw * th * 4];

            for (int y = 0; y < th; y++)
            {
                int sRow = (y * sh / th) * sw * 4;
                int dRow = y * tw * 4;
                for (int x = 0; x < tw; x++)
                {
                    int s = sRow + (x * sw / tw) * 4;
                    int d = dRow + x * 4;
                    dst[d] = src[s];
                    dst[d + 1] = src[s + 1];
                    dst[d + 2] = src[s + 2];
                    dst[d + 3] = src[s + 3];
                }
            }
            return (dst, tw, th);
        }

        private static IntPtr CreateHIcon((byte[] Pixels, int Width, int Height) rendered)
        {
            using var bitmap = new System.Drawing.Bitmap(rendered.Width, rendered.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bits = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, rendered.Width, rendered.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                Marshal.Copy(rendered.Pixels, 0, bits.Scan0, rendered.Pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(bits);
            }
            return bitmap.GetHicon();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// 实测任务栏图标槽位：Win11 任务栏图标为 24 逻辑像素随任务栏 DPI 缩放
        /// （100%:24，150%:36，200%:48），且对不匹配尺寸一律平滑降采样，
        /// 因此必须提供正好等于槽位的图源。不能用 GetSystemMetrics 类推算：
        /// PerMonitorV2 进程拿到的是未缩放的原始值
        /// </summary>
        private static int GetTaskbarIconSize()
        {
            return GetTrayDpi() / 4;
        }

        /// <summary>
        /// 托盘图标槽位为 16 逻辑像素随任务栏 DPI 缩放（100%:16，150%:24，200%:32），
        /// 与任务栏同理必须提供正好等于槽位的 HICON
        /// </summary>
        private static int GetTrayIconSize() => GetTrayDpi() / 6;

        private static int GetTrayDpi()
        {
            var tray = Win32Helper.FindWindow("Shell_TrayWnd", null);
            uint dpi = tray != IntPtr.Zero ? GetDpiForWindow(tray) : 0;
            dpi = dpi != 0 ? dpi : GetDpiForSystem();
            return (int)(dpi != 0 ? dpi : 96);
        }

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        private BitmapSource? GetIcoImageSource()
        {
            try
            {
                return new System.Drawing.Icon(IcoPath).ToImageSource() as BitmapSource;
            }
            catch { return null; }
        }

        private static BitmapImage? LoadImageSource(string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch { return null; }
        }

        /// <summary>
        /// 托盘与任务栏同理：直接写入按槽位精确渲染的 HICON。
        /// TaskbarIcon.IconSource 的内部转换链不保证像素忠实，
        /// 而 Icon 属性持有原生 System.Drawing.Icon，HICON 原样进入 NOTIFYICONDATA
        /// </summary>
        private void UpdateNotifyIcon(BitmapSource? source)
        {
            if (_taskbarIcon == null || source == null) return;

            var rendered = RenderNearestNeighbor(source, GetTrayIconSize());
            var newHandle = CreateHIcon(rendered);
            if (newHandle == IntPtr.Zero) return;

            _taskbarIcon.Icon = System.Drawing.Icon.FromHandle(newHandle);

            // Icon.FromHandle 不持有句柄，替换后由本服务统一销毁旧句柄
            if (_trayIconHandle != IntPtr.Zero)
            {
                DestroyIcon(_trayIconHandle);
            }
            _trayIconHandle = newHandle;
        }

        /// <summary>
        /// 恢复默认图标：托盘回到 IconSource 常规路径，
        /// 窗口经 WPF 原生 Window.Icon 管线重设（直接清空 WM_SETICON
        /// 任务栏不会回退刷新，必须写入实际图标）
        /// </summary>
        private void ResetToDefaultIcon()
        {
            var source = GetIcoImageSource();

            if (_taskbarIcon != null && source != null)
            {
                _taskbarIcon.IconSource = source;
            }
            if (_trayIconHandle != IntPtr.Zero)
            {
                DestroyIcon(_trayIconHandle);
                _trayIconHandle = IntPtr.Zero;
            }

            if (source != null)
            {
                foreach (Window window in Application.Current.Windows)
                {
                    window.Icon = source;
                }
            }
            if (_appliedIconHandle != IntPtr.Zero)
            {
                DestroyIcon(_appliedIconHandle);
                _appliedIconHandle = IntPtr.Zero;
            }
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
