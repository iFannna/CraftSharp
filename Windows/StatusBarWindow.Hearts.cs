using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 心形生命值功能
    /// 生命值左对齐快捷栏
    /// </summary>
    public partial class StatusBarWindow
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
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.HeartFull);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalHeartWidth = frame.PixelWidth;
                    _originalHeartHeight = frame.PixelHeight;
                }
            }

            path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.HeartHalf);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalHalfHeartWidth = frame.PixelWidth;
                }
            }
        }

        /// <summary>
        /// 设置心形生命值（左对齐快捷栏）
        /// </summary>
        private void SetupHearts()
        {
            double heartWidth = _originalHeartWidth * _scaleFactor;
            double heartHeight = _originalHeartHeight * _scaleFactor;
            double halfWidth = _originalHalfHeartWidth * _scaleFactor;
            double heartTopOffset = GetHeartY();

            // 生命值左对齐快捷栏
            double hotbarLeft = GetHotbarLeft();
            double expBarWidth = _originalExpBarWidth * _scaleFactor;
            double hotbarWidth = _originalHotbarWidth * _scaleFactor;

            // 经验条居中于快捷栏，生命值左对齐经验条左边界
            double expBarLeft = hotbarLeft + (hotbarWidth - expBarWidth) / 2;
            double heartLeft = expBarLeft;

            double heartGap = _heartGap * _scaleFactor;

            for (int i = 0; i < 10; i++)
            {
                double iconLeft = heartLeft + i * (heartWidth + heartGap);

                var containerImage = new System.Windows.Controls.Image
                {
                    Name = $"HeartContainer{i}",
                    Source = LoadBitmapImage(AssetPaths.HeartContainer),
                    Width = heartWidth,
                    Height = heartHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(containerImage, BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Canvas.SetLeft(containerImage, iconLeft);
                System.Windows.Controls.Canvas.SetTop(containerImage, heartTopOffset);
                HeartCanvas.Children.Add(containerImage);

                var halfImage = new System.Windows.Controls.Image
                {
                    Name = $"HeartHalf{i}",
                    Source = LoadBitmapImage(AssetPaths.HeartHalf),
                    Width = halfWidth,
                    Height = heartHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(halfImage, BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Canvas.SetLeft(halfImage, iconLeft);
                System.Windows.Controls.Canvas.SetTop(halfImage, heartTopOffset);
                HeartCanvas.Children.Add(halfImage);

                var fullImage = new System.Windows.Controls.Image
                {
                    Name = $"HeartFull{i}",
                    Source = LoadBitmapImage(AssetPaths.HeartFull),
                    Width = heartWidth,
                    Height = heartHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(fullImage, BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Canvas.SetLeft(fullImage, iconLeft);
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