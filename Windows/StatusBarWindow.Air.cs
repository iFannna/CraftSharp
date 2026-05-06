using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 空气值功能
    ///
    /// 布局规则：
    /// 1. 空气值紧贴核心容器右边缘右对齐
    /// 2. 空气值在饥饿值上方，间距6px基准
    /// 3. XAML顺序：空气值(最先) → 饥饿值(最后)
    /// 4. 实际显示：空气值(最上) → 饥饿值(最下)
    ///
    /// 数值规则：
    /// - 只允许2的倍数，向下取整
    /// - 当前值最小0，最大值最小2
    /// - 当前值不得超过最大值
    /// - 图标数量 = maxValue / 2
    /// - 满图标数量 = currentValue / 2
    /// - 没有半图标，只有满(air.png)和空(air_empty.png)两种状态
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalAirWidth;
        private double _originalAirHeight;
        private double _airGap = -1; // 空气值之间的间距（基准像素）

        /// <summary>
        /// 加载空气值图片尺寸
        /// </summary>
        private void LoadAirDimensions()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.Air);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalAirWidth = frame.PixelWidth;
                    _originalAirHeight = frame.PixelHeight;
                }
            }
        }

        /// <summary>
        /// 设置空气值（紧贴核心容器右边缘右对齐，在饥饿值上方）
        /// Canvas用于绘制空气值图标，Grid用于容器布局
        /// </summary>
        private void SetupAir()
        {
            double airWidth = _originalAirWidth * _scaleFactor;
            double airHeight = _originalAirHeight * _scaleFactor;
            double airGap = _airGap * _scaleFactor;

            // 获取配置值
            var settings = GetHudElementSettings("air");
            int maxValue = Math.Max(2, settings?.CustomMaxValue ?? 20); // 最大值最小为2
            int slotCount = maxValue / 2; // 图标数量

            // 设置AirCanvas尺寸（根据maxValue动态计算）
            double airsWidth = slotCount * airWidth + (slotCount - 1) * airGap;
            AirCanvas.Width = airsWidth;
            AirCanvas.Height = airHeight;

            // 设置AirGrid尺寸和布局
            AirGrid.Width = airsWidth;
            AirGrid.Height = airHeight;
            AirGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Right; // 右对齐
            // 与下方饥饿值间距：6px基准（Margin.Bottom在上层元素上）
            AirGrid.Margin = new Thickness(0, 0, 0, BaseVerticalSpacing * _scaleFactor);
            AirGrid.Visibility = _airVisible ? Visibility.Visible : Visibility.Collapsed;

            // 清除现有空气值图标
            AirCanvas.Children.Clear();

            // 绘制空气值图标，从右到左排列（因为右对齐）
            for (int i = 0; i < slotCount; i++)
            {
                // 计算图标位置：从右往左
                double iconLeft = airsWidth - (i + 1) * (airWidth + airGap) + airGap;

                // 空空气值（背景）
                var emptyImage = new System.Windows.Controls.Image
                {
                    Name = $"AirEmpty{i}",
                    Source = LoadBitmapImage(AssetPaths.AirEmpty),
                    Width = airWidth,
                    Height = airHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(emptyImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(emptyImage, iconLeft);
                Canvas.SetTop(emptyImage, 0);
                AirCanvas.Children.Add(emptyImage);

                // 满空气值
                var fullImage = new System.Windows.Controls.Image
                {
                    Name = $"AirFull{i}",
                    Source = LoadBitmapImage(AssetPaths.Air),
                    Width = airWidth,
                    Height = airHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(fullImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(fullImage, iconLeft);
                Canvas.SetTop(fullImage, 0);
                AirCanvas.Children.Add(fullImage);
            }

            UpdateAirLevel();
        }

        /// <summary>
        /// 更新空气值显示
        /// currentValue决定显示的满图标数量
        /// </summary>
        private void UpdateAirLevel()
        {
            // 获取配置值
            var settings = GetHudElementSettings("air");
            int maxValue = Math.Max(2, settings?.CustomMaxValue ?? 20);
            int currentValue = Math.Max(0, Math.Min(settings?.CustomCurrentValue ?? 20, maxValue)); // 当前值不超过最大值
            int slotCount = maxValue / 2;

            // 向下取整到2的倍数
            currentValue = (currentValue / 2) * 2;

            // 满图标数量
            int fullAirs = currentValue / 2;

            for (int i = 0; i < slotCount; i++)
            {
                var emptyImage = AirCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"AirEmpty{i}");
                var fullImage = AirCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"AirFull{i}");

                if (i < fullAirs)
                {
                    // 满空气
                    if (emptyImage != null) emptyImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Visible;
                }
                else
                {
                    // 空空气（只有背景empty可见）
                    if (emptyImage != null) emptyImage.Visibility = Visibility.Visible;
                    if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                }
            }
        }
    }
}