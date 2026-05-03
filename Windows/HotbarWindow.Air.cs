using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 空气值功能
    /// </summary>
    public partial class HotbarWindow
    {
        private double _originalAirWidth;
        private double _originalAirHeight;
        private double _originalAirBurstingWidth;
        private double _airGap = -1; // 空气值之间的间距
        private double _airSpacing = 1; // 空气值与饥饿值之间的间距

        /// <summary>
        /// 加载空气值图片尺寸
        /// </summary>
        private void LoadAirDimensions()
        {
            var uri = new Uri(AssetPaths.Air, UriKind.Relative);
            var stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
            if (stream != null)
            {
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                var frame = decoder.Frames[0];
                _originalAirWidth = frame.PixelWidth;
                _originalAirHeight = frame.PixelHeight;
            }

            uri = new Uri(AssetPaths.AirBursting, UriKind.Relative);
            stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
            if (stream != null)
            {
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                var frame = decoder.Frames[0];
                _originalAirBurstingWidth = frame.PixelWidth;
            }
        }

        /// <summary>
        /// 设置空气值（右对齐经验条，在饥饿值上方）
        /// </summary>
        private void SetupAir()
        {
            double airWidth = _originalAirWidth * _scaleFactor;
            double airHeight = _originalAirHeight * _scaleFactor;
            double burstingWidth = _originalAirBurstingWidth * _scaleFactor;

            double heartY = GetHeartY();
            double airSpacing = _airSpacing * _scaleFactor;
            double airTopOffset = heartY - airSpacing - airHeight;

            double hotbarWidth = _originalHotbarWidth * _scaleFactor;
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double spacing = _offhandSpacing * _scaleFactor;

            double hotbarLeftInWindow = _offhandOnRight ? 0 : offhandWidth + spacing;
            double expBarWidth = _originalExpBarWidth * _scaleFactor;
            double expBarRight = hotbarLeftInWindow + (hotbarWidth + expBarWidth) / 2;
            double airGap = _airGap * _scaleFactor;

            for (int i = 0; i < 10; i++)
            {
                double airLeft = expBarRight - (i + 1) * (airWidth + airGap) + airGap;

                var airImage = new System.Windows.Controls.Image
                {
                    Name = $"Air{i}",
                    Source = new BitmapImage(new Uri(AssetPaths.Air, UriKind.Relative)),
                    Width = airWidth,
                    Height = airHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(airImage, BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Canvas.SetLeft(airImage, airLeft);
                System.Windows.Controls.Canvas.SetTop(airImage, airTopOffset);
                AirCanvas.Children.Add(airImage);

                var burstingImage = new System.Windows.Controls.Image
                {
                    Name = $"AirBursting{i}",
                    Source = new BitmapImage(new Uri(AssetPaths.AirBursting, UriKind.Relative)),
                    Width = burstingWidth,
                    Height = airHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(burstingImage, BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Canvas.SetLeft(burstingImage, airLeft + airWidth - burstingWidth);
                System.Windows.Controls.Canvas.SetTop(burstingImage, airTopOffset);
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

            // 向上取整到最近的5
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