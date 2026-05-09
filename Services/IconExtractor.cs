using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Services
{
    /// <summary>
    /// 图标提取服务 - 使用 Windows Shell API 获取高质量大尺寸图标
    /// </summary>
    public static class IconExtractor
    {
        #region Shell32 P/Invoke

        // 图标尺寸级别常量
        private const int SHIL_LARGE = 0;      // 32x32
        private const int SHIL_SMALL = 1;      // 16x16
        private const int SHIL_EXTRALARGE = 2; // 48x48
        private const int SHIL_JUMBO = 4;      // 256x256 (Windows Vista+)

        // SHGetFileInfo 标志
        private const uint SHGFI_SYSICONINDEX = 0x000004000;
        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        // 文件属性
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        // IImageList 接口 GUID
        private static readonly Guid IID_IImageList = new Guid("46EB5926-582E-4017-9F5E-73562D6D6D6D");

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        [DllImport("shell32.dll", EntryPoint = "#727")]
        private static extern int SHGetImageList(
            int iImageList,
            ref Guid riid,
            out IImageList ppv);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfoW(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFOW psfi,
            uint cbFileInfo,
            uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFOW
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        /// <summary>
        /// IImageList COM 接口 - 用于获取指定尺寸的图标
        /// </summary>
        [ComImport]
        [Guid("46EB5926-582E-4017-9F5E-73562D6D6D6D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig]
            int GetImageCount(out int pcImages);

            [PreserveSig]
            int GetImage(int i, int dwFlags, out IntPtr hImage);

            [PreserveSig]
            int GetImageInfo(int i, ref IMAGEINFO pImageInfo);

            // 其他方法省略，我们只需要 GetImage
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGEINFO
        {
            public IntPtr hbmImage;
            public IntPtr hbmMask;
            public int Unused1;
            public int Unused2;
            public RECT rcImage;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        #endregion

        /// <summary>
        /// 从文件路径提取图标（支持指定尺寸）
        /// </summary>
        /// <param name="filePath">文件或文件夹路径</param>
        /// <param name="size">目标尺寸（像素）</param>
        /// <returns>WPF ImageSource</returns>
        public static ImageSource? GetIcon(string filePath, int size = 32)
        {
            if (!File.Exists(filePath) && !Directory.Exists(filePath))
                return null;

            try
            {
                // 根据请求尺寸选择合适的图标级别
                int imageListLevel = GetImageListLevel(size);

                // 尝试使用 Shell API 获取高质量图标
                var icon = GetIconFromShell(filePath, imageListLevel);
                if (icon != null)
                {
                    return icon;
                }

                // 回退到传统方法
                return GetIconFallback(filePath, size);
            }
            catch
            {
                // 任何异常都回退到传统方法
                return GetIconFallback(filePath, size);
            }
        }

        /// <summary>
        /// 根据请求尺寸选择 Shell 图标级别
        /// </summary>
        private static int GetImageListLevel(int size)
        {
            if (size <= 32)
                return SHIL_LARGE;      // 32x32
            else if (size <= 48)
                return SHIL_EXTRALARGE; // 48x48
            else
                return SHIL_JUMBO;      // 256x256
        }

        /// <summary>
        /// 使用 Shell API 获取高质量图标
        /// </summary>
        private static ImageSource? GetIconFromShell(string filePath, int imageListLevel)
        {
            // 获取文件属性（用于文件夹识别）
            uint fileAttributes = Directory.Exists(filePath)
                ? FILE_ATTRIBUTE_DIRECTORY
                : FILE_ATTRIBUTE_NORMAL;

            // 调用 SHGetFileInfo 获取图标索引
            var shfi = new SHFILEINFOW();
            uint flags = SHGFI_SYSICONINDEX | SHGFI_USEFILEATTRIBUTES;

            IntPtr result = SHGetFileInfoW(filePath, fileAttributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

            if (result == IntPtr.Zero || shfi.iIcon < 0)
                return null;

            int iconIndex = shfi.iIcon;

            // 获取指定尺寸的图标列表
            IImageList? imageList = null;
            try
            {
                var iid = IID_IImageList;
                int hr = SHGetImageList(imageListLevel, ref iid, out imageList);
                if (hr != 0 || imageList == null)
                    return null;

                // 从列表中获取图标
                IntPtr hIcon = IntPtr.Zero;
                hr = imageList.GetImage(iconIndex, 0, out hIcon);
                if (hr != 0 || hIcon == IntPtr.Zero)
                    return null;

                // 转换为 WPF ImageSource
                var imageSource = ConvertIconToImageSource(hIcon);

                // 释放图标句柄
                DestroyIcon(hIcon);

                return imageSource;
            }
            finally
            {
                // 释放 COM 对象
                if (imageList != null)
                {
                    Marshal.ReleaseComObject(imageList);
                }
            }
        }

        /// <summary>
        /// 将图标句柄转换为 WPF ImageSource
        /// </summary>
        private static ImageSource? ConvertIconToImageSource(IntPtr hIcon)
        {
            if (hIcon == IntPtr.Zero)
                return null;

            try
            {
                // 从句柄创建 Icon 对象
                using var icon = Icon.FromHandle(hIcon);
                using var bitmap = icon.ToBitmap();
                using var memoryStream = new MemoryStream();

                // 保存为 PNG 格式
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Position = 0;

                // 创建 BitmapImage
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 回退方法：使用传统 Icon.ExtractAssociatedIcon
        /// </summary>
        private static ImageSource? GetIconFallback(string filePath, int size)
        {
            try
            {
                using var icon = Icon.ExtractAssociatedIcon(filePath);
                if (icon == null)
                    return null;

                using var bitmap = icon.ToBitmap();
                using var memoryStream = new MemoryStream();

                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取文件夹图标
        /// </summary>
        public static ImageSource? GetFolderIcon(int size = 32)
        {
            try
            {
                return GetIcon(Environment.GetFolderPath(Environment.SpecialFolder.Windows), size);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 扩展方法：将 Icon 转换为 ImageSource
        /// </summary>
        public static ImageSource ToImageSource(this Icon icon)
        {
            using var bitmap = icon.ToBitmap();
            using var memoryStream = new MemoryStream();

            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }
    }
}