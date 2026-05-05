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
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalAirWidth;
        private double _originalAirHeight;
        private double _originalAirBurstingWidth;
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

            path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.AirBursting);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalAirBurstingWidth = frame.PixelWidth;
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
            double burstingWidth = _originalAirBurstingWidth * _scaleFactor;
            double airGap = _airGap * _scaleFactor;

            // 设置AirCanvas尺寸
            double airsWidth = 10 * airWidth + 9 * airGap;
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

            // 绘制10个空气值图标，从右到左排列（因为右对齐）
            for (int i = 0; i < 10; i++)
            {
                // 计算图标位置：从右往左
                double iconLeft = airsWidth - (i + 1) * (airWidth + airGap) + airGap;

                // 正常空气值
                var airImage = new System.Windows.Controls.Image
                {
                    Name = $"Air{i}",
                    Source = LoadBitmapImage(AssetPaths.Air),
                    Width = airWidth,
                    Height = airHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(airImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(airImage, iconLeft);
                Canvas.SetTop(airImage, 0);
                AirCanvas.Children.Add(airImage);

                // 爆裂空气值（半值）
                var burstingImage = new System.Windows.Controls.Image
                {
                    Name = $"AirBursting{i}",
                    Source = LoadBitmapImage(AssetPaths.AirBursting),
                    Width = burstingWidth,
                    Height = airHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(burstingImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(burstingImage, iconLeft + airWidth - burstingWidth);
                Canvas.SetTop(burstingImage, 0);
                AirCanvas.Children.Add(burstingImage);
            }

            UpdateAirLevel();
        }

        /// <summary>
        /// 更新空气值显示（向上取整）
        /// </summary>
        private void UpdateAirLevel()
        {
            var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
            var batteryPercent = powerStatus.BatteryLifePercent;

            int percent = (int)Math.Ceiling(batteryPercent * 100);
            int roundedPercent = ((percent + 4) / 5) * 5;
            if (roundedPercent > 100) roundedPercent = 100;

            int fullAirs = roundedPercent / 10;
            bool hasBursting = (roundedPercent % 10) == 5;

            for (int i = 0; i < 10; i++)
            {
                var airImage = AirCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"Air{i}");
                var burstingImage = AirCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"AirBursting{i}");

                if (i < fullAirs)
                {
                    if (airImage != null) airImage.Visibility = Visibility.Visible;
                    if (burstingImage != null) burstingImage.Visibility = Visibility.Hidden;
                }
                else if (i == fullAirs && hasBursting)
                {
                    if (airImage != null) airImage.Visibility = Visibility.Hidden;
                    if (burstingImage != null) burstingImage.Visibility = Visibility.Visible;
                }
                else
                {
                    if (airImage != null) airImage.Visibility = Visibility.Hidden;
                    if (burstingImage != null) burstingImage.Visibility = Visibility.Hidden;
                }
            }
        }
    }
}