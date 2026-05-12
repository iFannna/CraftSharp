using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace CraftSharp.Services
{
    /// <summary>
    /// 图标提取服务 - 使用 Windows Shell API 获取高质量大尺寸图标
    /// 同时支持图片文件的高质量加载
    /// </summary>
    public static class IconExtractor
    {
        // 支持的图片扩展名
        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".webp" };

        #region Shell32 Shortcut Parsing - Method 1: Shell.Application

        /// <summary>
        /// 解析快捷方式获取目标路径 - 方法1：使用 Shell.Application COM
        /// </summary>
        private static string? GetShortcutTargetPath_ShellApp(string shortcutPath)
        {
            try
            {
                var shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return null;

                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell == null) return null;

                dynamic? folder = shell.NameSpace(Path.GetDirectoryName(shortcutPath));
                if (folder == null) return null;

                dynamic? folderItem = folder.ParseName(Path.GetFileName(shortcutPath));
                if (folderItem == null) return null;

                if (folderItem.IsLink)
                {
                    dynamic? link = folderItem.GetLink;
                    if (link != null)
                    {
                        string targetPath = link.Target?.Path ?? "";
                        if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
                        {
                            return targetPath;
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Shell32 Shortcut Parsing - Method 2: IShellLinkW

        [ComImport]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMax, out _WIN32_FIND_DATAW pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMax);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMax);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out ushort pwHotkey);
            void SetHotkey(ushort wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIconIndex);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIconIndex);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct _WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public long ftCreationTime;
            public long ftLastAccessTime;
            public long ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        [ClassInterface(ClassInterfaceType.None)]
        private class ShellLink
        {
        }

        private const uint SLGP_SHORTPATH = 0x1;
        private const uint SLGP_UNCPRIORITY = 0x2;
        private const uint SLGP_RAWPATH = 0x4;

        /// <summary>
        /// 解析快捷方式获取目标路径 - 方法2：使用 IShellLinkW COM 接口
        /// </summary>
        private static string? GetShortcutTargetPath_IShellLink(string shortcutPath)
        {
            try
            {
                var link = new ShellLink() as IShellLinkW;
                if (link == null) return null;

                // 加载快捷方式文件
                ((IPersistFile)link).Load(shortcutPath, 0);

                // 解析快捷方式（获取目标路径）
                link.Resolve(IntPtr.Zero, 0);

                // 获取目标路径
                StringBuilder sb = new StringBuilder(260);
                link.GetPath(sb, sb.Capacity, out _, SLGP_RAWPATH);

                string targetPath = sb.ToString();
                if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
                {
                    return targetPath;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        [ComImport]
        [Guid("0000010b-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] StringBuilder ppszFileName);
        }

        #endregion

        #region Shell32 Shortcut Parsing - Method 3: SHGetFileInfo + Icon.ExtractAssociatedIcon

        /// <summary>
        /// 解析快捷方式获取目标路径 - 方法3：使用 ExtractAssociatedIcon 回退
        /// 这个方法不返回目标路径，而是直接获取目标程序图标
        /// </summary>
        private static ImageSource? GetTargetIconFromShortcut(string shortcutPath, int size)
        {
            try
            {
                // Icon.ExtractAssociatedIcon 对于快捷方式会返回目标程序的图标
                using var icon = Icon.ExtractAssociatedIcon(shortcutPath);
                if (icon == null) return null;

                return ConvertIconToImageSourceInternal(icon);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        /// <summary>
        /// 解析快捷方式获取目标路径（尝试多种方法）
        /// </summary>
        /// <param name="shortcutPath">快捷方式文件路径(.lnk)</param>
        /// <returns>目标路径，如果解析失败返回null</returns>
        public static string? GetShortcutTargetPath(string shortcutPath)
        {
            if (!File.Exists(shortcutPath) || !shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // 方法1: Shell.Application COM
            string? targetPath = GetShortcutTargetPath_ShellApp(shortcutPath);
            if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
                return targetPath;

            // 方法2: IShellLinkW COM 接口
            targetPath = GetShortcutTargetPath_IShellLink(shortcutPath);
            if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
                return targetPath;

            return null;
        }

        /// <summary>
        /// 判断文件是否为快捷方式
        /// </summary>
        public static bool IsShortcut(string filePath)
        {
            return filePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取快捷方式自身的图标（不考虑目标程序）
        /// </summary>
        public static ImageSource? GetShortcutIcon(string shortcutPath, int size = 32)
        {
            if (!File.Exists(shortcutPath))
                return null;

            try
            {
                // 使用 Shell API 获取快捷方式图标
                var icon = GetIconFromShell(shortcutPath, GetImageListLevel(size));
                if (icon != null)
                {
                    return icon;
                }

                // 回退到 ExtractAssociatedIcon
                using var extractedIcon = Icon.ExtractAssociatedIcon(shortcutPath);
                if (extractedIcon == null) return null;

                return ConvertIconToImageSourceInternal(extractedIcon);
            }
            catch (Exception)
            {
                return null;
                return null;
            }
        }

        /// <summary>
        /// 获取目标程序图标（尝试多种方法）
        /// </summary>
        public static ImageSource? GetTargetIcon(string shortcutPath, int size = 32)
        {
            // 方法1: 先尝试解析目标路径，再获取图标
            string? targetPath = GetShortcutTargetPath(shortcutPath);
            if (!string.IsNullOrEmpty(targetPath) && File.Exists(targetPath))
            {
                return GetIcon(targetPath, size);
            }

            // 方法2: 使用 ExtractAssociatedIcon 直接从快捷方式获取目标图标
            // 对于某些快捷方式，ExtractAssociatedIcon 会直接返回目标程序的图标
            return GetTargetIconFromShortcut(shortcutPath, size);
        }

        private static ImageSource? ConvertIconToImageSourceInternal(Icon icon)
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
        /// 从文件路径提取图标或图片（支持指定尺寸）
        /// 自动识别文件类型：
        /// - 图片文件(.png/.jpg等)：直接加载图片
        /// - 其他文件/文件夹：获取系统图标
        /// </summary>
        /// <param name="filePath">文件或文件夹路径</param>
        /// <param name="size">目标尺寸（像素）</param>
        /// <returns>WPF ImageSource</returns>
        public static ImageSource? GetIcon(string filePath, int size = 32)
        {
            if (!File.Exists(filePath) && !Directory.Exists(filePath))
                return null;

            // 检查是否是图片文件
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (ImageExtensions.Contains(extension))
            {
                return LoadImageFile(filePath, size);
            }

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
        /// 加载图片文件，保持原始尺寸
        /// 让WPF自己进行高质量缩放，避免二次缩放导致质量损失
        /// </summary>
        private static ImageSource? LoadImageFile(string filePath, int targetSize)
        {
            try
            {
                // 直接加载原图，不预先缩放
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
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