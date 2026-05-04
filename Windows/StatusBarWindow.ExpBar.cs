using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 经验条功能
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalExpBarWidth;
        private double _originalExpBarHeight;
        private double _spacing = 1; // 经验条与快捷栏之间的间距

        /// <summary>
        /// 加载经验条图片尺寸
        /// </summary>
        private void LoadExpBarDimensions()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.ExperienceBarBackground);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalExpBarWidth = frame.PixelWidth;
                    _originalExpBarHeight = frame.PixelHeight;
                }
            }
        }

        /// <summary>
        /// 设置经验条（和快捷栏一样水平居中）
        /// </summary>
        private void SetupExperienceBar()
        {
            double expBarWidth = _originalExpBarWidth * _scaleFactor;
            double expBarHeight = _originalExpBarHeight * _scaleFactor;

            ExperienceBarBackground.Source = LoadBitmapImage(AssetPaths.ExperienceBarBackground);
            ExperienceBarProgress.Source = LoadBitmapImage(AssetPaths.ExperienceBarProgress);

            ExperienceBarGrid.Width = expBarWidth;
            ExperienceBarGrid.Height = expBarHeight;

            ExperienceBarBackground.Width = expBarWidth;
            ExperienceBarBackground.Height = expBarHeight;

            ExperienceBarProgress.Height = expBarHeight;

            double hotbarWidth = _originalHotbarWidth * _scaleFactor;
            double offhandWidth = _originalOffhandWidth * _scaleFactor;
            double spacing = _offhandSpacing * _scaleFactor;

            double hotbarLeftInWindow = _offhandOnRight ? 0 : offhandWidth + spacing;
            double expBarTopOffset = GetExpBarTopOffset();

            System.Windows.Controls.Canvas.SetLeft(ExperienceBarGrid, hotbarLeftInWindow + (hotbarWidth - expBarWidth) / 2);
            System.Windows.Controls.Canvas.SetTop(ExperienceBarGrid, expBarTopOffset);

            UpdateBatteryLevel();
        }

        /// <summary>
        /// 更新电量显示（使用裁剪截断）
        /// </summary>
        private void UpdateBatteryLevel()
        {
            var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
            var batteryPercent = powerStatus.BatteryLifePercent;

            double expBarWidth = _originalExpBarWidth * _scaleFactor;
            double expBarHeight = _originalExpBarHeight * _scaleFactor;

            ExperienceBarProgress.Width = expBarWidth;
            ExperienceBarProgress.Height = expBarHeight;

            var clipRect = new Rect(0, 0, expBarWidth * batteryPercent, expBarHeight);
            ExperienceBarProgress.Clip = new RectangleGeometry(clipRect);
        }
    }
}