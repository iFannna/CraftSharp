using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 伤害吸收值功能
    /// </summary>
    public partial class HotbarWindow
    {
        private double _originalAbsorbingFullWidth;
        private double _originalAbsorbingFullHeight;
        private double _originalAbsorbingHalfWidth;
        private double _absorbingGap = -1; // 伤害吸收值之间的间距
        private double _absorbingRowSpacing = 1; // 伤害吸收值行之间的间距
        private double _absorbingToHeartSpacing = 1; // 伤害吸收值与心形之间的间距

        private int _absorbingValue = 175;
        private int _absorbingBackgroundValue = 175;
        private const int MaxAbsorbingValue = 5100;
        private const int AbsorbingPerIcon = 10;

        /// <summary>
        /// 加载伤害吸收值图片尺寸
        /// </summary>
        private void LoadAbsorbingDimensions()
        {
            var uri = new Uri(AssetPaths.AbsorbingFull, UriKind.Relative);
            var stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
            if (stream != null)
            {
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                var frame = decoder.Frames[0];
                _originalAbsorbingFullWidth = frame.PixelWidth;
                _originalAbsorbingFullHeight = frame.PixelHeight;
            }

            uri = new Uri(AssetPaths.AbsorbingHalf, UriKind.Relative);
            stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
            if (stream != null)
            {
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                var frame = decoder.Frames[0];
                _originalAbsorbingHalfWidth = frame.PixelWidth;
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
        /// 设置伤害吸收值（左对齐经验条）
        /// </summary>
        private void SetupAbsorbing()
        {
            double absorbingWidth = _originalAbsorbingFullWidth * _scaleFactor;
            double absorbingHeight = _originalAbsorbingFullHeight * _scaleFactor;
            double halfWidth = _originalAbsorbingHalfWidth * _scaleFactor;

            double hotbarWidth = _originalHotbarWidth * _scaleFactor;
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double spacing = _offhandSpacing * _scaleFactor;

            double hotbarLeftInWindow = _offhandOnRight ? 0 : offhandWidth + spacing;
            double expBarWidth = _originalExpBarWidth * _scaleFactor;
            double expBarLeft = hotbarLeftInWindow + (hotbarWidth - expBarWidth) / 2;

            double absorbingGap = _absorbingGap * _scaleFactor;
            double rowSpacing = _absorbingRowSpacing * _scaleFactor;
            double absorbingToHeartSpacing = _absorbingToHeartSpacing * _scaleFactor;

            int rows = Math.Max(GetAbsorbingBackgroundRows(), GetAbsorbingRows());
            double heartY = GetHeartY();
            double absorbingBottomRowY = heartY - absorbingToHeartSpacing - absorbingHeight;

            for (int row = 0; row < rows; row++)
            {
                double rowTopOffset = absorbingBottomRowY - row * (absorbingHeight + rowSpacing);

                for (int i = 0; i < 10; i++)
                {
                    int globalIndex = row * 10 + i;
                    double iconLeft = expBarLeft + i * (absorbingWidth + absorbingGap);

                    var containerImage = new System.Windows.Controls.Image
                    {
                        Name = $"AbsorbingContainer{globalIndex}",
                        Source = new BitmapImage(new Uri(AssetPaths.HeartContainer, UriKind.Relative)),
                        Width = absorbingWidth,
                        Height = absorbingHeight,
                        Stretch = Stretch.Uniform,
                        UseLayoutRounding = true,
                        SnapsToDevicePixels = true
                    };
                    RenderOptions.SetBitmapScalingMode(containerImage, BitmapScalingMode.NearestNeighbor);
                    System.Windows.Controls.Canvas.SetLeft(containerImage, iconLeft);
                    System.Windows.Controls.Canvas.SetTop(containerImage, rowTopOffset);
                    AbsorbingCanvas.Children.Add(containerImage);

                    var halfImage = new System.Windows.Controls.Image
                    {
                        Name = $"AbsorbingHalf{globalIndex}",
                        Source = new BitmapImage(new Uri(AssetPaths.AbsorbingHalf, UriKind.Relative)),
                        Width = halfWidth,
                        Height = absorbingHeight,
                        Stretch = Stretch.Uniform,
                        UseLayoutRounding = true,
                        SnapsToDevicePixels = true
                    };
                    RenderOptions.SetBitmapScalingMode(halfImage, BitmapScalingMode.NearestNeighbor);
                    System.Windows.Controls.Canvas.SetLeft(halfImage, iconLeft);
                    System.Windows.Controls.Canvas.SetTop(halfImage, rowTopOffset);
                    AbsorbingCanvas.Children.Add(halfImage);

                    var fullImage = new System.Windows.Controls.Image
                    {
                        Name = $"AbsorbingFull{globalIndex}",
                        Source = new BitmapImage(new Uri(AssetPaths.AbsorbingFull, UriKind.Relative)),
                        Width = absorbingWidth,
                        Height = absorbingHeight,
                        Stretch = Stretch.Uniform,
                        UseLayoutRounding = true,
                        SnapsToDevicePixels = true
                    };
                    RenderOptions.SetBitmapScalingMode(fullImage, BitmapScalingMode.NearestNeighbor);
                    System.Windows.Controls.Canvas.SetLeft(fullImage, iconLeft);
                    System.Windows.Controls.Canvas.SetTop(fullImage, rowTopOffset);
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