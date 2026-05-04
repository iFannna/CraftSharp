using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 饥饿值功能
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalFoodWidth;
        private double _originalFoodHeight;
        private double _originalHalfFoodWidth;
        private double _foodGap = -1; // 饥饿值之间的间距

        /// <summary>
        /// 加载饥饿值图片尺寸
        /// </summary>
        private void LoadFoodDimensions()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.FoodFull);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalFoodWidth = frame.PixelWidth;
                    _originalFoodHeight = frame.PixelHeight;
                }
            }

            path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.FoodHalf);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalHalfFoodWidth = frame.PixelWidth;
                }
            }
        }

        /// <summary>
        /// 设置饥饿值（右对齐经验条）
        /// </summary>
        private void SetupFood()
        {
            double foodWidth = _originalFoodWidth * _scaleFactor;
            double foodHeight = _originalFoodHeight * _scaleFactor;
            double halfWidth = _originalHalfFoodWidth * _scaleFactor;
            double foodTopOffset = GetHeartY();

            double hotbarWidth = _originalHotbarWidth * _scaleFactor;
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double spacing = _offhandSpacing * _scaleFactor;

            double hotbarLeftInWindow = _offhandOnRight ? 0 : offhandWidth + spacing;
            double expBarWidth = _originalExpBarWidth * _scaleFactor;
            double expBarRight = hotbarLeftInWindow + (hotbarWidth + expBarWidth) / 2;
            double foodGap = _foodGap * _scaleFactor;

            for (int i = 0; i < 10; i++)
            {
                double foodLeft = expBarRight - (i + 1) * (foodWidth + foodGap) + foodGap;

                var emptyImage = new System.Windows.Controls.Image
                {
                    Name = $"FoodEmpty{i}",
                    Source = LoadBitmapImage(AssetPaths.FoodEmpty),
                    Width = foodWidth,
                    Height = foodHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(emptyImage, BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Canvas.SetLeft(emptyImage, foodLeft);
                System.Windows.Controls.Canvas.SetTop(emptyImage, foodTopOffset);
                FoodCanvas.Children.Add(emptyImage);

                var halfImage = new System.Windows.Controls.Image
                {
                    Name = $"FoodHalf{i}",
                    Source = LoadBitmapImage(AssetPaths.FoodHalf),
                    Width = halfWidth,
                    Height = foodHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(halfImage, BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Canvas.SetLeft(halfImage, foodLeft + foodWidth - halfWidth);
                System.Windows.Controls.Canvas.SetTop(halfImage, foodTopOffset);
                FoodCanvas.Children.Add(halfImage);

                var fullImage = new System.Windows.Controls.Image
                {
                    Name = $"FoodFull{i}",
                    Source = LoadBitmapImage(AssetPaths.FoodFull),
                    Width = foodWidth,
                    Height = foodHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(fullImage, BitmapScalingMode.NearestNeighbor);
                System.Windows.Controls.Canvas.SetLeft(fullImage, foodLeft);
                System.Windows.Controls.Canvas.SetTop(fullImage, foodTopOffset);
                FoodCanvas.Children.Add(fullImage);
            }

            UpdateFoodLevel();
        }

        /// <summary>
        /// 更新饥饿值显示（向上取整）
        /// </summary>
        private void UpdateFoodLevel()
        {
            var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
            var batteryPercent = powerStatus.BatteryLifePercent;

            // 向上取整到最近的5
            int percent = (int)Math.Ceiling(batteryPercent * 100);
            int roundedPercent = ((percent + 4) / 5) * 5;
            if (roundedPercent > 100) roundedPercent = 100;

            int fullFoods = roundedPercent / 10;
            bool hasHalfFood = (roundedPercent % 10) == 5;

            for (int i = 0; i < 10; i++)
            {
                var halfImage = FoodCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"FoodHalf{i}");
                var fullImage = FoodCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"FoodFull{i}");

                if (i < fullFoods)
                {
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Visible;
                }
                else if (i == fullFoods && hasHalfFood)
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