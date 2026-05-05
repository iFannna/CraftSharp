using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 饥饿值功能
    ///
    /// 布局规则：
    /// 1. 饥饿值紧贴核心容器右边缘右对齐
    /// 2. 使用Canvas绘制饥饿值图标，外层使用Grid+StackPanel布局
    /// 3. XAML顺序：空气值(最先) → 饥饿值(最后)
    /// 4. 实际显示：空气值(最上) → 饥饿值(最下)
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalFoodWidth;
        private double _originalFoodHeight;
        private double _originalHalfFoodWidth;
        private double _foodGap = -1; // 饥饿值之间的间距（基准像素）

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
        /// 设置饥饿值（紧贴核心容器右边缘右对齐）
        /// Canvas用于绘制饥饿值图标，Grid用于容器布局
        /// </summary>
        private void SetupFood()
        {
            double foodWidth = _originalFoodWidth * _scaleFactor;
            double foodHeight = _originalFoodHeight * _scaleFactor;
            double halfWidth = _originalHalfFoodWidth * _scaleFactor;
            double foodGap = _foodGap * _scaleFactor;

            // 设置FoodCanvas尺寸
            double foodsWidth = 10 * foodWidth + 9 * foodGap;
            FoodCanvas.Width = foodsWidth;
            FoodCanvas.Height = foodHeight;

            // 设置FoodGrid尺寸和布局
            FoodGrid.Width = foodsWidth;
            FoodGrid.Height = foodHeight;
            FoodGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Right; // 右对齐
            // 饥饿值在最下方，无需上方间距（空气值会设置Margin）

            // 设置可见性
            FoodGrid.Visibility = _foodVisible ? Visibility.Visible : Visibility.Collapsed;

            // 清除现有饥饿值图标
            FoodCanvas.Children.Clear();

            // 绘制10个饥饿值图标，从右到左排列（因为右对齐）
            for (int i = 0; i < 10; i++)
            {
                // 计算图标位置：从右往左
                double iconLeft = foodsWidth - (i + 1) * (foodWidth + foodGap) + foodGap;

                // 空饥饿值（背景）
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
                Canvas.SetLeft(emptyImage, iconLeft);
                Canvas.SetTop(emptyImage, 0);
                FoodCanvas.Children.Add(emptyImage);

                // 半饥饿值
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
                Canvas.SetLeft(halfImage, iconLeft + foodWidth - halfWidth);
                Canvas.SetTop(halfImage, 0);
                FoodCanvas.Children.Add(halfImage);

                // 满饥饿值
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
                Canvas.SetLeft(fullImage, iconLeft);
                Canvas.SetTop(fullImage, 0);
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