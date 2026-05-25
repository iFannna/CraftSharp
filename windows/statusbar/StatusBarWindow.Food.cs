using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CraftSharp.Models;
using CraftSharp.Helpers;
using CraftSharp.Services.Core;
using CraftSharp.Services.Hud;
using CraftSharp.Services.Slot;
using CraftSharp.Services.Resource;

namespace CraftSharp.Windows.StatusBar
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
    ///
    /// 饱和度规则：
    /// - 饱和度叠加在饥饿值上方，ZIndex最高
    /// - 饱和度只有满和半两种图标，没有空图标
    /// - 饱和度为0时，所有饱和度图标隐藏，露出下面的饥饿值
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalFoodWidth;
        private double _originalFoodHeight;
        private double _originalHalfFoodWidth;
        private double _foodGap = -1; // 饥饿值之间的间距（基准像素）

        private double _originalSaturationWidth;
        private double _originalSaturationHeight;
        private double _originalHalfSaturationWidth;

        /// <summary>
        /// 加载饥饿值图片尺寸
        /// </summary>
        private void LoadFoodDimensions()
        {
            var (width, height) = ImageService.Instance.GetImageDimensions(AssetPaths.FoodFull);
            _originalFoodWidth = width;
            _originalFoodHeight = height;

            var (halfWidth, _) = ImageService.Instance.GetImageDimensions(AssetPaths.FoodHalf);
            _originalHalfFoodWidth = halfWidth;
        }

        /// <summary>
        /// 加载饱和度图片尺寸
        /// </summary>
        private void LoadSaturationDimensions()
        {
            var (width, height) = ImageService.Instance.GetImageDimensions(AssetPaths.SaturationFull);
            _originalSaturationWidth = width;
            _originalSaturationHeight = height;

            var (halfWidth, _) = ImageService.Instance.GetImageDimensions(AssetPaths.SaturationHalf);
            _originalHalfSaturationWidth = halfWidth;
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
            var settings = GetHudElementSettings("Food");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int slotCount = maxValue / 2; // 每个槽位代表2点（一个完整图标）
            string iconStyle = settings?.IconStyle ?? ""; // 图标样式（food_full 或 food_full_hunger）

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
                    Source = LoadBitmapImage(AssetPaths.GetFoodPath(iconStyle, "empty")),
                    Width = foodWidth,
                    Height = foodHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(emptyImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(emptyImage, iconLeft);
                Canvas.SetTop(emptyImage, 0);
                Canvas.SetZIndex(emptyImage, 0); // 最底层
                FoodCanvas.Children.Add(emptyImage);

                // 半饥饿值
                var halfImage = new System.Windows.Controls.Image
                {
                    Name = $"FoodHalf{i}",
                    Source = LoadBitmapImage(AssetPaths.GetFoodPath(iconStyle, "half")),
                    Width = halfWidth,
                    Height = foodHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(halfImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(halfImage, iconLeft + foodWidth - halfWidth);
                Canvas.SetTop(halfImage, 0);
                Canvas.SetZIndex(halfImage, 10);
                FoodCanvas.Children.Add(halfImage);

                // 满饥饿值
                var fullImage = new System.Windows.Controls.Image
                {
                    Name = $"FoodFull{i}",
                    Source = LoadBitmapImage(AssetPaths.GetFoodPath(iconStyle, "full")),
                    Width = foodWidth,
                    Height = foodHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(fullImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(fullImage, iconLeft);
                Canvas.SetTop(fullImage, 0);
                Canvas.SetZIndex(fullImage, 20);
                FoodCanvas.Children.Add(fullImage);

                // 半饱和度（叠加在饥饿值上方）
                var saturationHalfImage = new System.Windows.Controls.Image
                {
                    Name = $"SaturationHalf{i}",
                    Source = LoadBitmapImage(AssetPaths.SaturationHalf),
                    Width = _originalHalfSaturationWidth * _scaleFactor,
                    Height = _originalSaturationHeight * _scaleFactor,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true,
                    Visibility = Visibility.Hidden // 默认隐藏
                };
                RenderOptions.SetBitmapScalingMode(saturationHalfImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(saturationHalfImage, iconLeft + foodWidth - _originalHalfSaturationWidth * _scaleFactor);
                Canvas.SetTop(saturationHalfImage, 0);
                Canvas.SetZIndex(saturationHalfImage, 30); // 高于饥饿值
                FoodCanvas.Children.Add(saturationHalfImage);

                // 满饱和度（叠加在饥饿值上方，最高层）
                var saturationFullImage = new System.Windows.Controls.Image
                {
                    Name = $"SaturationFull{i}",
                    Source = LoadBitmapImage(AssetPaths.SaturationFull),
                    Width = _originalSaturationWidth * _scaleFactor,
                    Height = _originalSaturationHeight * _scaleFactor,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true,
                    Visibility = Visibility.Hidden // 默认隐藏
                };
                RenderOptions.SetBitmapScalingMode(saturationFullImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(saturationFullImage, iconLeft);
                Canvas.SetTop(saturationFullImage, 0);
                Canvas.SetZIndex(saturationFullImage, 40); // 最高层
                FoodCanvas.Children.Add(saturationFullImage);
            }

            UpdateFoodLevel();
            UpdateSaturationLevel();
        }

        /// <summary>
        /// 更新饥饿值显示
        /// currentValue决定显示的half/full图标数量
        /// </summary>
        private void UpdateFoodLevel()
        {
            // 获取配置值
            var settings = GetHudElementSettings("Food");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int slotCount = maxValue / 2;

            // 计算当前值
            int currentValue;
            bool dataMappingEnabled = settings?.CustomValueEnabled != true;

            if (dataMappingEnabled)
            {
                // 数据映射开启：从数据源获取百分比，转换为饥饿值
                string mappingType = settings?.DataMappingType ?? "BatteryLevel";
                double percent = DataMappingService.Instance.GetValue(mappingType);
                currentValue = (int)(percent * maxValue);
            }
            else
            {
                // 自定义数值开启：使用配置的当前值
                currentValue = settings?.CustomCurrentValue ?? 20;
            }

            // 当前值不超过最大值
            currentValue = Math.Max(0, Math.Min(currentValue, maxValue));

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

        /// <summary>
        /// 更新饱和度显示
        /// 饱和度叠加在饥饿值上方，只有满和半两种状态
        /// </summary>
        private void UpdateSaturationLevel()
        {
            // 获取配置值
            var settings = GetHudElementSettings("Food");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int saturationValue = settings?.CustomSaturationValue ?? 0;
            int slotCount = maxValue / 2;

            // 计算完整和半饱和度数量
            // saturationValue: 半饱和=1, 满饱和=2
            int fullSaturation = saturationValue / 2;
            bool hasHalfSaturation = (saturationValue % 2) == 1;

            for (int i = 0; i < slotCount; i++)
            {
                var halfImage = FoodCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"SaturationHalf{i}");
                var fullImage = FoodCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"SaturationFull{i}");

                if (i < fullSaturation)
                {
                    // 满饱和度
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Visible;
                }
                else if (i == fullSaturation && hasHalfSaturation)
                {
                    // 半饱和度
                    if (halfImage != null) halfImage.Visibility = Visibility.Visible;
                    if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                }
                else
                {
                    // 无饱和度（隐藏，露出下方饥饿值）
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                }
            }
        }
    }
}