using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace CraftSharp.Services
{
    /// <summary>
    /// 图片加载服务 - 统一 BitmapImage 加载和尺寸读取
    /// </summary>
    public class ImageService
    {
        private static ImageService? _instance;
        public static ImageService Instance => _instance ??= new ImageService();

        private static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 将相对路径转换为绝对路径
        /// </summary>
        public string GetAbsolutePath(string relativePath)
        {
            if (relativePath.StartsWith("assets/"))
            {
                return Path.Combine(BaseDirectory, relativePath);
            }
            return Path.Combine(BaseDirectory, "Assets", relativePath);
        }

        /// <summary>
        /// 从相对路径加载 BitmapImage
        /// </summary>
        public BitmapImage? LoadBitmapImage(string relativePath)
        {
            var fullPath = GetAbsolutePath(relativePath);
            return LoadBitmapImageFromPath(fullPath);
        }

        /// <summary>
        /// 从绝对路径加载 BitmapImage
        /// </summary>
        public BitmapImage? LoadBitmapImageFromPath(string absolutePath)
        {
            if (!File.Exists(absolutePath))
            {
                return null;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(absolutePath, UriKind.Absolute);
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

        /// <summary>
        /// 获取图片尺寸（像素宽度、高度）
        /// </summary>
        public (int Width, int Height) GetImageDimensions(string relativePath)
        {
            var fullPath = GetAbsolutePath(relativePath);
            return GetImageDimensionsFromPath(fullPath);
        }

        /// <summary>
        /// 从绝对路径获取图片尺寸（像素宽度、高度）
        /// </summary>
        public (int Width, int Height) GetImageDimensionsFromPath(string absolutePath)
        {
            if (!File.Exists(absolutePath))
            {
                return (0, 0);
            }

            try
            {
                using (var stream = File.OpenRead(absolutePath))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    return (frame.PixelWidth, frame.PixelHeight);
                }
            }
            catch
            {
                return (0, 0);
            }
        }
    }
}