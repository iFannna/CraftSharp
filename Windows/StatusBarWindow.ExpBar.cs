using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 经验条功能
    ///
    /// 布局规则：
    /// 1. 经验条宽度占满核心容器（182×缩放比例）
    /// 2. 与下方快捷栏间距6px基准（通过StackPanel Margin实现）
    /// 3. 使用Grid布局，居中显示
    ///
    /// 数值规则：
    /// - CustomCurrentValue 范围 0-100（百分比）
    /// - CustomValueEnabled 时使用自定义值，否则使用数据映射（电池等）
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalExpBarWidth;
        private double _originalExpBarHeight;

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
        /// 设置经验条（居中于核心容器，在快捷栏上方6px）
        /// 使用StackPanel布局，通过Margin实现间距
        /// </summary>
        private void SetupExperienceBar()
        {
            double expBarHeight = _originalExpBarHeight * _scaleFactor;
            double coreWidth = GetCoreContainerWidth();

            // 设置经验条尺寸（高度按原图比例，宽度可适当调整）
            ExperienceBarGrid.Height = expBarHeight;
            ExperienceBarGrid.Width = coreWidth; // 占满核心容器

            // 设置背景图片
            ExperienceBarBackground.Source = LoadBitmapImage(AssetPaths.ExperienceBarBackground);
            ExperienceBarBackground.Width = coreWidth;
            ExperienceBarBackground.Height = expBarHeight;

            // 设置进度图片
            ExperienceBarProgress.Source = LoadBitmapImage(AssetPaths.ExperienceBarProgress);
            ExperienceBarProgress.Height = expBarHeight;

            // 与下方快捷栏间距：6px基准（Margin.Bottom在上层元素上）
            ExperienceBarGrid.Margin = new Thickness(0, 0, 0, BaseVerticalSpacing * _scaleFactor);
            ExperienceBarGrid.Visibility = _expBarVisible ? Visibility.Visible : Visibility.Collapsed;

            UpdateExpBarProgress();
        }

        /// <summary>
        /// 更新经验条进度显示
        /// </summary>
        private void UpdateExpBarProgress()
        {
            double coreWidth = GetCoreContainerWidth();
            double expBarHeight = _originalExpBarHeight * _scaleFactor;

            ExperienceBarProgress.Width = coreWidth;
            ExperienceBarProgress.Height = expBarHeight;

            // 获取进度百分比
            double percent = GetExpBarPercent();

            var clipRect = new Rect(0, 0, coreWidth * percent, expBarHeight);
            ExperienceBarProgress.Clip = new RectangleGeometry(clipRect);
        }

        /// <summary>
        /// 获取经验条进度百分比（0.0 - 1.0）
        /// </summary>
        private double GetExpBarPercent()
        {
            var settings = GetHudElementSettings("expbar");

            // 如果启用自定义数值，使用配置的当前值（0-100）
            if (settings?.CustomValueEnabled == true)
            {
                int currentValue = settings.CustomCurrentValue;
                return currentValue / 100.0;
            }

            // 否则使用电池电量（数据映射）
            var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
            return powerStatus.BatteryLifePercent;
        }
    }
}