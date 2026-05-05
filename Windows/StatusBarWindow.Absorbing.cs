using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalAbsorbingFullWidth;
        private double _originalAbsorbingFullHeight;
        private double _originalAbsorbingHalfWidth;
        private double _absorbingGap = -1; // 伤害吸收值之间的间距（基准像素）
        private double _absorbingRowSpacing = 1; // 伤害吸收值行之间的间距（基准像素）

        private int _absorbingValue = 175;
        private int _absorbingBackgroundValue = 175;
        private const int MaxAbsorbingValue = 5100;
        private const int AbsorbingPerIcon = 10;

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

        private int GetAbsorbingBackgroundRows()
        {
            int fullAbsorbing = _absorbingBackgroundValue / AbsorbingPerIcon;
            int remainder = _absorbingBackgroundValue % AbsorbingPerIcon;
            bool hasHalf = remainder >= 5;
            int totalIcons = fullAbsorbing + (hasHalf ? 1 : 0);
            return (int)Math.Ceiling(totalIcons / 10.0);
        }

        private int GetAbsorbingRows()
        {
            int fullAbsorbing = _absorbingValue / AbsorbingPerIcon;
            int remainder = _absorbingValue % AbsorbingPerIcon;
            bool hasHalf = remainder >= 5;
            int totalIcons = fullAbsorbing + (hasHalf ? 1 : 0);
            return (int)Math.Ceiling(totalIcons / 10.0);
        }

        private int GetMaxAbsorbingRows()
        {
            return Math.Max(GetAbsorbingBackgroundRows(), GetAbsorbingRows());
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
            double rowSpacing = _absorbingRowSpacing * _scaleFactor;

            int rows = Math.Max(GetAbsorbingBackgroundRows(), GetAbsorbingRows());

            // 设置AbsorbingCanvas尺寸（支持多行）
            double absorbingRowWidth = 10 * absorbingWidth + 9 * absorbingGap;
            double absorbingTotalHeight = rows * absorbingHeight + (rows - 1) * rowSpacing;
            AbsorbingCanvas.Width = absorbingRowWidth;
            AbsorbingCanvas.Height = absorbingTotalHeight;

            // 设置AbsorbingGrid尺寸和布局
            AbsorbingGrid.Width = absorbingRowWidth;
            AbsorbingGrid.Height = absorbingTotalHeight;
            AbsorbingGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left; // 左对齐
            // 与下方生命值间距：6px基准（Margin.Bottom在上层元素上）
            AbsorbingGrid.Margin = new Thickness(0, 0, 0, BaseVerticalSpacing * _scaleFactor);
            AbsorbingGrid.Visibility = _absorbingVisible ? Visibility.Visible : Visibility.Collapsed;

            // 清除现有伤害吸收值图标
            AbsorbingCanvas.Children.Clear();

            // 绘制多行伤害吸收值，从下往上排列
            for (int row = 0; row < rows; row++)
            {
                // 最底行(row=rows-1)在最下方，最顶行(row=0)在最上方
                double rowTopOffset = (rows - 1 - row) * (absorbingHeight + rowSpacing);

                for (int i = 0; i < 10; i++)
                {
                    int globalIndex = row * 10 + i;
                    double iconLeft = i * (absorbingWidth + absorbingGap);

                    // 心形容器（空心）
                    var containerImage = new System.Windows.Controls.Image
                    {
                        Name = $"AbsorbingContainer{globalIndex}",
                        Source = LoadBitmapImage(AssetPaths.HeartContainer),
                        Width = absorbingWidth,
                        Height = absorbingHeight,
                        Stretch = Stretch.Uniform,
                        UseLayoutRounding = true,
                        SnapsToDevicePixels = true
                    };
                    RenderOptions.SetBitmapScalingMode(containerImage, BitmapScalingMode.NearestNeighbor);
                    Canvas.SetLeft(containerImage, iconLeft);
                    Canvas.SetTop(containerImage, rowTopOffset);
                    AbsorbingCanvas.Children.Add(containerImage);

                    // 半伤害吸收值
                    var halfImage = new System.Windows.Controls.Image
                    {
                        Name = $"AbsorbingHalf{globalIndex}",
                        Source = LoadBitmapImage(AssetPaths.AbsorbingHalf),
                        Width = halfWidth,
                        Height = absorbingHeight,
                        Stretch = Stretch.Uniform,
                        UseLayoutRounding = true,
                        SnapsToDevicePixels = true
                    };
                    RenderOptions.SetBitmapScalingMode(halfImage, BitmapScalingMode.NearestNeighbor);
                    Canvas.SetLeft(halfImage, iconLeft);
                    Canvas.SetTop(halfImage, rowTopOffset);
                    AbsorbingCanvas.Children.Add(halfImage);

                    // 满伤害吸收值
                    var fullImage = new System.Windows.Controls.Image
                    {
                        Name = $"AbsorbingFull{globalIndex}",
                        Source = LoadBitmapImage(AssetPaths.AbsorbingFull),
                        Width = absorbingWidth,
                        Height = absorbingHeight,
                        Stretch = Stretch.Uniform,
                        UseLayoutRounding = true,
                        SnapsToDevicePixels = true
                    };
                    RenderOptions.SetBitmapScalingMode(fullImage, BitmapScalingMode.NearestNeighbor);
                    Canvas.SetLeft(fullImage, iconLeft);
                    Canvas.SetTop(fullImage, rowTopOffset);
                    AbsorbingCanvas.Children.Add(fullImage);
                }
            }

            UpdateAbsorbing();
        }

        /// <summary>
        /// 更新伤害吸收值显示
        /// </summary>
        private void UpdateAbsorbing()
        {
            int backgroundFull = _absorbingBackgroundValue / AbsorbingPerIcon;
            int backgroundRemainder = _absorbingBackgroundValue % AbsorbingPerIcon;
            bool backgroundHasHalf = backgroundRemainder >= 5;
            int backgroundTotalIcons = backgroundFull + (backgroundHasHalf ? 1 : 0);

            int fillFull = _absorbingValue / AbsorbingPerIcon;
            int fillRemainder = _absorbingValue % AbsorbingPerIcon;
            bool fillHasHalf = fillRemainder >= 5;

            int rows = GetMaxAbsorbingRows();

            for (int row = 0; row < rows; row++)
            {
                for (int i = 0; i < 10; i++)
                {
                    int globalIndex = row * 10 + i;

                    var containerImage = AbsorbingCanvas.Children.OfType<System.Windows.Controls.Image>()
                        .FirstOrDefault(img => img.Name == $"AbsorbingContainer{globalIndex}");
                    var fullImage = AbsorbingCanvas.Children.OfType<System.Windows.Controls.Image>()
                        .FirstOrDefault(img => img.Name == $"AbsorbingFull{globalIndex}");
                    var halfImage = AbsorbingCanvas.Children.OfType<System.Windows.Controls.Image>()
                        .FirstOrDefault(img => img.Name == $"AbsorbingHalf{globalIndex}");

                    if (globalIndex < backgroundTotalIcons)
                    {
                        if (containerImage != null) containerImage.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (containerImage != null) containerImage.Visibility = Visibility.Hidden;
                    }

                    if (globalIndex < fillFull)
                    {
                        if (fullImage != null) fullImage.Visibility = Visibility.Visible;
                        if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    }
                    else if (globalIndex == fillFull && fillHasHalf)
                    {
                        if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                        if (halfImage != null) halfImage.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                        if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    }
                }
            }
        }
    }
}