using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CraftSharp.Models;

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
    ///
    /// 图标规则：
    /// - maxValue上限20，代表10个完整图标（半饥饿=1，满饥饿=2）
    /// - maxValue决定显示的背景图标数量
    /// - currentValue决定显示的half/full图标数量
    /// - 背景使用empty图标
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

            // 获取配置值
            var settings = GetHudElementSettings("food");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int slotCount = maxValue / 2; // 每个槽位代表2点（一个完整图标）

            // 设置FoodCanvas尺寸（根据maxValue动态计算）
            double foodsWidth = slotCount * foodWidth + (slotCount - 1) * foodGap;
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

            // 根据maxValue绘制槽位（背景图标），从右到左排列（因为右对齐）
            for (int i = 0; i < slotCount; i++)
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
        /// 更新饥饿值显示
        /// currentValue决定显示的half/full图标数量
        /// </summary>
        private void UpdateFoodLevel()
        {
            // 获取配置值
            var settings = GetHudElementSettings("food");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int currentValue = settings?.CustomCurrentValue ?? 20;
            int slotCount = maxValue / 2;

            // 计算完整和半饥饿数量
            // currentValue: 半饥饿=1, 满饥饿=2
            int fullFoods = currentValue / 2;
            bool hasHalfFood = (currentValue % 2) == 1;

            for (int i = 0; i < slotCount; i++)
            {
                var halfImage = FoodCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"FoodHalf{i}");
                var fullImage = FoodCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"FoodFull{i}");

                if (i < fullFoods)
                {
                    // 满饥饿
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Visible;
                }
                else if (i == fullFoods && hasHalfFood)
                {
                    // 半饥饿
                    if (halfImage != null) halfImage.Visibility = Visibility.Visible;
                    if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                }
                else
                {
                    // 空（只有背景empty可见）
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                }
            }
        }
    }
}