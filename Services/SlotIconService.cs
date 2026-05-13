using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CraftSharp.Helpers;
using CraftSharp.Models;
using CraftSharp.Services;

namespace CraftSharp.Services
{
    /// <summary>
    /// 格子图标管理服务
    /// 负责图标加载、渲染模式、占位图处理
    /// </summary>
    public class SlotIconService
    {
        private readonly SlotFileValidator _fileValidator;
        private readonly AppSettings? _appSettings;
        private readonly double _scaleFactor;

        /// <summary>
        /// 图标需要更新事件（文件丢失或恢复时触发）
        /// 参数：slotId, filePath, isPlaceholder
        /// </summary>
        public event EventHandler<IconUpdateEventArgs>? IconNeedsUpdate;

        public SlotIconService(SlotFileValidator fileValidator, AppSettings? appSettings, double scaleFactor)
        {
            _fileValidator = fileValidator;
            _appSettings = appSettings;
            _scaleFactor = scaleFactor;

            // 订阅文件丢失/恢复事件
            _fileValidator.FileMissing += OnFileMissing;
            _fileValidator.FileRecovered += OnFileRecovered;
        }

        /// <summary>
        /// 图标更新事件参数
        /// </summary>
        public class IconUpdateEventArgs : EventArgs
        {
            public string FilePath { get; }
            public bool IsPlaceholder { get; }

            public IconUpdateEventArgs(string filePath, bool isPlaceholder)
            {
                FilePath = filePath;
                IsPlaceholder = isPlaceholder;
            }
        }

        private void OnFileMissing(object? sender, string filePath)
        {
            IconNeedsUpdate?.Invoke(this, new IconUpdateEventArgs(filePath, true));
        }

        private void OnFileRecovered(object? sender, string filePath)
        {
            IconNeedsUpdate?.Invoke(this, new IconUpdateEventArgs(filePath, false));
        }

        /// <summary>
        /// 根据设置获取格子图标
        /// 对于快捷方式：根据 HotbarShowTargetIcon 设置决定显示快捷方式图标还是目标程序图标
        /// </summary>
        public ImageSource? GetSlotIcon(string filePath)
        {
            int iconSize = (int)(32 * _scaleFactor);

            // 检查是否为快捷方式
            if (IconExtractor.IsShortcut(filePath))
            {
                bool showTargetIcon = _appSettings?.Hotbar.ShowTargetIcon ?? false;
                if (showTargetIcon)
                {
                    return IconExtractor.GetTargetIcon(filePath, iconSize);
                }
                else
                {
                    return IconExtractor.GetShortcutIcon(filePath, iconSize);
                }
            }

            // 普通文件，使用默认图标提取
            return IconExtractor.GetIcon(filePath, iconSize);
        }

        /// <summary>
        /// 加载占位图（barrier.png）
        /// </summary>
        public ImageSource? LoadPlaceholderIcon()
        {
            var placeholderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.PlaceholderBarrier);
            return LoadBitmapImage(placeholderPath);
        }

        /// <summary>
        /// 获取格子图标，自动处理文件丢失情况
        /// 返回图标 Source 和渲染模式
        /// 渲染模式规则：占位图用 NearestNeighbor，正常图标用 HighQuality
        /// </summary>
        public SlotIconResult GetIconWithRenderMode(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                // 空格子，返回默认的 HighQuality 模式（与格子创建时一致）
                return new SlotIconResult(null, BitmapScalingMode.HighQuality, true);
            }

            // 文件丢失时显示占位图
            if (!_fileValidator.IsFilePathValid(filePath))
            {
                _fileValidator.MarkMissing(filePath);
                var placeholder = LoadPlaceholderIcon();
                return new SlotIconResult(placeholder, BitmapScalingMode.NearestNeighbor, true);
            }

            // 文件有效，正常加载（使用 HighQuality 与格子创建时一致）
            var icon = GetSlotIcon(filePath);
            return new SlotIconResult(icon, BitmapScalingMode.HighQuality, false);
        }

        /// <summary>
        /// 判断图标是否是占位图
        /// </summary>
        public static bool IsPlaceholderImage(ImageSource? imageSource)
        {
            if (imageSource is BitmapImage bitmapImage && bitmapImage.UriSource != null)
            {
                return bitmapImage.UriSource.AbsolutePath.Contains("barrier.png");
            }
            return false;
        }

        /// <summary>
        /// 从文件路径加载 BitmapImage
        /// </summary>
        private static BitmapImage? LoadBitmapImage(string absolutePath)
        {
            if (!File.Exists(absolutePath))
                return null;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(absolutePath, UriKind.Absolute);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        /// <summary>
        /// 获取图标尺寸（基于缩放比例）
        /// </summary>
        public int GetIconSize()
        {
            return (int)(32 * _scaleFactor);
        }
    }

    /// <summary>
    /// 格子图标加载结果
    /// </summary>
    public class SlotIconResult
    {
        public ImageSource? IconSource { get; }
        public BitmapScalingMode RenderMode { get; }
        public bool IsPlaceholder { get; }

        public SlotIconResult(ImageSource? iconSource, BitmapScalingMode renderMode, bool isPlaceholder)
        {
            IconSource = iconSource;
            RenderMode = renderMode;
            IsPlaceholder = isPlaceholder;
        }
    }
}