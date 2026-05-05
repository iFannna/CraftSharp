using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 心形生命值功能
    ///
    /// 布局规则：
    /// 1. 生命值紧贴核心容器左边缘左对齐
    /// 2. 使用Canvas绘制心形，外层使用Grid+StackPanel布局
    /// 3. XAML顺序：护甲值(最先) → 伤害吸收 → 生命值(最后)
    /// 4. 实际显示：护甲值(最上) → 伤害吸收 → 生命值(最下)
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalHeartWidth;
        private double _originalHeartHeight;
        private double _originalHalfHeartWidth;
        private double _heartGap = -1; // 心形之间的间距（基准像素）

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
        /// 设置心形生命值（紧贴核心容器左边缘左对齐）
        /// Canvas用于绘制心形，Grid用于容器布局
        /// </summary>
        private void SetupHearts()
        {
            double heartWidth = _originalHeartWidth * _scaleFactor;
            double heartHeight = _originalHeartHeight * _scaleFactor;
            double halfWidth = _originalHalfHeartWidth * _scaleFactor;
            double heartGap = _heartGap * _scaleFactor;

            // 设置HeartCanvas尺寸
            double heartsWidth = 10 * heartWidth + 9 * heartGap;
            HeartCanvas.Width = heartsWidth;
            HeartCanvas.Height = heartHeight;

            // 设置HeartGrid尺寸和布局
            HeartGrid.Width = heartsWidth;
            HeartGrid.Height = heartHeight;
            HeartGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left; // 左对齐
            // 生命值在最下方，无需上方间距（伤害吸收值会设置Margin）

            // 设置可见性
            HeartGrid.Visibility = _healthVisible ? Visibility.Visible : Visibility.Collapsed;

            // 清除现有心形
            HeartCanvas.Children.Clear();

            // 绘制10颗心形，从左到右排列
            for (int i = 0; i < 10; i++)
            {
                double iconLeft = i * (heartWidth + heartGap);

                // 心形容器（空心）
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
                Canvas.SetLeft(containerImage, iconLeft);
                Canvas.SetTop(containerImage, 0);
                HeartCanvas.Children.Add(containerImage);

                // 半心
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
                Canvas.SetLeft(halfImage, iconLeft);
                Canvas.SetTop(halfImage, 0);
                HeartCanvas.Children.Add(halfImage);

                // 满心
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
                Canvas.SetLeft(fullImage, iconLeft);
                Canvas.SetTop(fullImage, 0);
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