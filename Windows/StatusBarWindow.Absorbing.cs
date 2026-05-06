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
    /// 伤害吸收值功能
    ///
    /// 布局规则：
    /// 1. 伤害吸收值紧贴核心容器左边缘左对齐
    /// 2. 伤害吸收值在生命值上方，间距6px基准
    /// 3. XAML顺序：护甲值(最先) → 伤害吸收 → 生命值(最后)
    /// 4. 实际显示：护甲值(最上) → 伤害吸收 → 生命值(最下)
    ///
    /// 图标规则：
    /// - maxValue上限20，代表10个完整图标（半吸收=1，满吸收=2）
    /// - maxValue决定显示的背景图标数量
    /// - currentValue决定显示的half/full图标数量
    /// - 背景使用container图标（心形容器）
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalAbsorbingFullWidth;
        private double _originalAbsorbingFullHeight;
        private double _originalAbsorbingHalfWidth;
        private double _absorbingGap = -1; // 伤害吸收值之间的间距（基准像素）

        /// <summary>
        /// 加载伤害吸收值图片尺寸
        /// </summary>
        private void LoadAbsorbingDimensions()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.AbsorbingFull);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalAbsorbingFullWidth = frame.PixelWidth;
                    _originalAbsorbingFullHeight = frame.PixelHeight;
                }
            }

            path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.AbsorbingHalf);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalAbsorbingHalfWidth = frame.PixelWidth;
                }
            }
        }

        /// <summary>
        /// 设置伤害吸收值（紧贴核心容器左边缘左对齐，在生命值上方）
        /// Canvas用于绘制伤害吸收值图标，Grid用于容器布局
        /// </summary>
        private void SetupAbsorbing()
        {
            double absorbingWidth = _originalAbsorbingFullWidth * _scaleFactor;
            double absorbingHeight = _originalAbsorbingFullHeight * _scaleFactor;
            double halfWidth = _originalAbsorbingHalfWidth * _scaleFactor;
            double absorbingGap = _absorbingGap * _scaleFactor;

            // 获取配置值
            var settings = GetHudElementSettings("absorbing");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int slotCount = maxValue / 2; // 每个槽位代表2点（一个完整图标）

            // 设置AbsorbingCanvas尺寸（根据maxValue动态计算）
            double absorbingRowWidth = slotCount * absorbingWidth + (slotCount - 1) * absorbingGap;
            AbsorbingCanvas.Width = absorbingRowWidth;
            AbsorbingCanvas.Height = absorbingHeight;

            // 设置AbsorbingGrid尺寸和布局
            AbsorbingGrid.Width = absorbingRowWidth;
            AbsorbingGrid.Height = absorbingHeight;
            AbsorbingGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left; // 左对齐
            // 与下方生命值间距：6px基准（Margin.Bottom在上层元素上）
            AbsorbingGrid.Margin = new Thickness(0, 0, 0, BaseVerticalSpacing * _scaleFactor);
            AbsorbingGrid.Visibility = _absorbingVisible ? Visibility.Visible : Visibility.Collapsed;

            // 清除现有伤害吸收值图标
            AbsorbingCanvas.Children.Clear();

            // 根据maxValue绘制槽位（背景图标）
            for (int i = 0; i < slotCount; i++)
            {
                double iconLeft = i * (absorbingWidth + absorbingGap);

                // 心形容器（背景）
                var containerImage = new System.Windows.Controls.Image
                {
                    Name = $"AbsorbingContainer{i}",
                    Source = LoadBitmapImage(AssetPaths.HeartContainer),
                    Width = absorbingWidth,
                    Height = absorbingHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(containerImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(containerImage, iconLeft);
                Canvas.SetTop(containerImage, 0);
                AbsorbingCanvas.Children.Add(containerImage);

                // 半伤害吸收值
                var halfImage = new System.Windows.Controls.Image
                {
                    Name = $"AbsorbingHalf{i}",
                    Source = LoadBitmapImage(AssetPaths.AbsorbingHalf),
                    Width = halfWidth,
                    Height = absorbingHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(halfImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(halfImage, iconLeft);
                Canvas.SetTop(halfImage, 0);
                AbsorbingCanvas.Children.Add(halfImage);

                // 满伤害吸收值
                var fullImage = new System.Windows.Controls.Image
                {
                    Name = $"AbsorbingFull{i}",
                    Source = LoadBitmapImage(AssetPaths.AbsorbingFull),
                    Width = absorbingWidth,
                    Height = absorbingHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(fullImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(fullImage, iconLeft);
                Canvas.SetTop(fullImage, 0);
                AbsorbingCanvas.Children.Add(fullImage);
            }

            UpdateAbsorbing();
        }

        /// <summary>
        /// 更新伤害吸收值显示
        /// currentValue决定显示的half/full图标数量
        /// </summary>
        private void UpdateAbsorbing()
        {
            // 获取配置值
            var settings = GetHudElementSettings("absorbing");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int currentValue = settings?.CustomCurrentValue ?? 20;
            int slotCount = maxValue / 2;

            // 计算完整和半吸收数量
            // currentValue: 半吸收=1, 满吸收=2
            int fullAbsorbing = currentValue / 2;
            bool hasHalfAbsorbing = (currentValue % 2) == 1;

            for (int i = 0; i < slotCount; i++)
            {
                var fullImage = AbsorbingCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"AbsorbingFull{i}");
                var halfImage = AbsorbingCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"AbsorbingHalf{i}");

                if (i < fullAbsorbing)
                {
                    // 满吸收
                    if (fullImage != null) fullImage.Visibility = Visibility.Visible;
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                }
                else if (i == fullAbsorbing && hasHalfAbsorbing)
                {
                    // 半吸收
                    if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                    if (halfImage != null) halfImage.Visibility = Visibility.Visible;
                }
                else
                {
                    // 空（只有背景container可见）
                    if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                }
            }
        }
    }
}