using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 心形生命值功能
    /// </summary>
    public partial class HotbarWindow
    {
        private double _originalHeartWidth;
        private double _originalHeartHeight;
        private double _originalHalfHeartWidth;
        private double _heartSpacing = 1; // 心形生命条与经验条之间的间距
        private double _heartGap = -1; // 心形之间的间距

        /// <summary>
        /// 加载心形图片尺寸
        /// </summary>
        private void LoadHeartDimensions()
        {
            // heart/full.png（用于获取心形整体尺寸）
            var uri = new Uri(AssetPaths.HeartFull, UriKind.Relative);
            var stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
            if (stream != null)
            {
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                var frame = decoder.Frames[0];
                _originalHeartWidth = frame.PixelWidth;
                _originalHeartHeight = frame.PixelHeight;
            }

            // heart/half.png（半颗心图片，只需宽度）
            uri = new Uri(AssetPaths.HeartHalf, UriKind.Relative);
            stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
            if (stream != null)
            {
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                var frame = decoder.Frames[0];
                _originalHalfHeartWidth = frame.PixelWidth;
            }
        }

        /// <summary>
        /// 设置心形生命值（左对齐经验条）
        /// </summary>
        private void SetupHearts()
        {
            double heartWidth = _originalHeartWidth * _scaleFactor;
            double heartHeight = _originalHeartHeight * _scaleFactor;
            double halfWidth = _originalHalfHeartWidth * _scaleFactor;
            double heartTopOffset = GetHeartY();

            double hotbarWidth = _originalHotbarWidth * _scaleFactor;
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double spacing = _offhandSpacing * _scaleFactor;

            double hotbarLeftInWindow = _offhandOnRight ? 0 : offhandWidth + spacing;
            double expBarWidth = _originalExpBarWidth * _scaleFactor;
            double expBarLeft = hotbarLeftInWindow + (hotbarWidth - expBarWidth) / 2;
            double heartGap = _heartGap * _scaleFactor;

            for (int i = 0; i < 10; i++)
            {
                double heartLeft = expBarLeft + i * (heartWidth + heartGap);

                // 背景：container.png
                var containerImage = new System.Windows.Controls.Image
                {
                    Name = $"HeartContainer{i}",
                    Source = new BitmapImage(new Uri(AssetPaths.HeartContainer, UriKind.Relative)),
                    Width = heartWidth,
                    Height = heartHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(containerImage, BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Canvas.SetLeft(containerImage, heartLeft);
                System.Windows.Controls.Canvas.SetTop(containerImage, heartTopOffset);
                HeartCanvas.Children.Add(containerImage);

                // 半心：half.png
                var halfImage = new System.Windows.Controls.Image
                {
                    Name = $"HeartHalf{i}",
                    Source = new BitmapImage(new Uri(AssetPaths.HeartHalf, UriKind.Relative)),
                    Width = halfWidth,
                    Height = heartHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(halfImage, BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Canvas.SetLeft(halfImage, heartLeft);
                System.Windows.Controls.Canvas.SetTop(halfImage, heartTopOffset);
                HeartCanvas.Children.Add(halfImage);

                // 完整心：full.png
                var fullImage = new System.Windows.Controls.Image
                {
                    Name = $"HeartFull{i}",
                    Source = new BitmapImage(new Uri(AssetPaths.HeartFull, UriKind.Relative)),
                    Width = heartWidth,
                    Height = heartHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(fullImage, BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Canvas.SetLeft(fullImage, heartLeft);
                System.Windows.Controls.Canvas.SetTop(fullImage, heartTopOffset);
                HeartCanvas.Children.Add(fullImage);
            }

            UpdateHeartLevel();
        }

        /// <summary>
        /// 更新心形生命值显示
        /// </summary>
        private void UpdateHeartLevel()
        {
            var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
            var batteryPercent = powerStatus.BatteryLifePercent;

            int fullHearts = (int)(batteryPercent * 10);
            double remainder = batteryPercent * 100 - fullHearts * 10;
            bool hasHalfHeart = remainder >= 5;

            for (int i = 0; i < 10; i++)
            {
                var halfImage = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"HeartHalf{i}");
                var fullImage = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"HeartFull{i}");

                if (i < fullHearts)
                {
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Visible;
                }
                else if (i == fullHearts && hasHalfHeart)
                {
                    if (halfImage != null) halfImage.Visibility = Visibility.Visible;
                    if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                }
                else
                {
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                }
            }
        }
    }
}