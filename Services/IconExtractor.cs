using System;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Services
{
    /// <summary>
    /// 图标提取服务
    /// </summary>
    public static class IconExtractor
    {
        /// <summary>
        /// 从文件路径提取图标
        /// </summary>
        public static ImageSource? GetIcon(string filePath, int size = 32)
        {
            if (!File.Exists(filePath) && !Directory.Exists(filePath))
                return null;

            try
            {
                // 使用 System.Drawing 提取图标
                using var icon = Icon.ExtractAssociatedIcon(filePath);
                if (icon == null)
                    return null;

                // 转换为 WPF ImageSource
                return ConvertToImageSource(icon, size);
            }
            catch
            {
                // 提取失败时返回 null
                return null;
            }
        }

        /// <summary>
        /// 将 Icon 转换为 ImageSource
        /// </summary>
        private static ImageSource ConvertToImageSource(Icon icon, int size)
        {
            // 获取指定大小的图标
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

        /// <summary>
        /// 获取文件夹图标
        /// </summary>
        public static ImageSource? GetFolderIcon(int size = 32)
        {
            try
            {
                // 使用 Shell32 获取文件夹图标
                return GetIcon(Environment.GetFolderPath(Environment.SpecialFolder.Windows), size);
            }
            catch
            {
                return null;
            }
        }
    }
}