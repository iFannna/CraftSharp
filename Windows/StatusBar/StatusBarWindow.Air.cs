using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CraftSharp.Helpers;
using System.Windows.Threading;
using CraftSharp.Services;

namespace CraftSharp.Windows.StatusBar
{
    /// <summary>
    /// 空气值功能
    ///
    /// 布局规则：
    /// 1. 空气值紧贴核心容器右边缘右对齐
    /// 2. 空气值在饥饿值上方，间距6px基准
    /// 3. XAML顺序：空气值(最先) → 饥饿值(最后)
    /// 4. 实际显示：空气值(最上) → 饥饿值(最下)
    ///
    /// 数值规则：
    /// - 只允许2的倍数，向下取整
    /// - 当前值最小0，最大值最小2
    /// - 当前值不得超过最大值
    /// - 图标数量 = maxValue / 2
    /// - 满图标数量 = currentValue / 2
    /// - 没有半图标，只有满(air.png)和空(air_empty.png)两种状态
    ///
    /// 动画规则（仅在动画效果开关开启且数据映射开启时生效）：
    /// - 空气值增加时：无动画效果
    /// - 空气值减少时：消失的air.png变成air_bursting.png，持续1000ms后消失
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalAirWidth;
        private double _originalAirHeight;
        private double _airGap = -1; // 空气值之间的间距（基准像素）

        // 上一次的空气值（用于检测变化并触发动画）
        private int _previousAirValue = -1;
        // 动画定时器
        private DispatcherTimer? _airAnimationTimer;
        // 是否正在显示动画
        private bool _airAnimationPlaying = false;

        /// <summary>
        /// 加载空气值图片尺寸
        /// </summary>
        private void LoadAirDimensions()
        {
            var (width, height) = ImageService.Instance.GetImageDimensions(AssetPaths.Air);
            _originalAirWidth = width;
            _originalAirHeight = height;
        }

        /// <summary>
        /// 设置空气值（紧贴核心容器右边缘右对齐，在饥饿值上方）
        /// Canvas用于绘制空气值图标，Grid用于容器布局
        /// </summary>
        private void SetupAir()
        {
            double airWidth = _originalAirWidth * _scaleFactor;
            double airHeight = _originalAirHeight * _scaleFactor;
            double airGap = _airGap * _scaleFactor;

            // 获取配置值
            var settings = GetHudElementSettings("Air");
            int maxValue = Math.Max(2, settings?.CustomMaxValue ?? 20); // 最大值最小为2
            int slotCount = maxValue / 2; // 图标数量

            // 设置AirCanvas尺寸（根据maxValue动态计算）
            double airsWidth = slotCount * airWidth + (slotCount - 1) * airGap;
            AirCanvas.Width = airsWidth;
            AirCanvas.Height = airHeight;

            // 设置AirGrid尺寸和布局
            AirGrid.Width = airsWidth;
            AirGrid.Height = airHeight;
            AirGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Right; // 右对齐
            // 与下方饥饿值间距：6px基准（Margin.Bottom在上层元素上）
            AirGrid.Margin = new Thickness(0, 0, 0, BaseVerticalSpacing * _scaleFactor);
            AirGrid.Visibility = _airVisible ? Visibility.Visible : Visibility.Collapsed;

            // 清除现有空气值图标
            AirCanvas.Children.Clear();

            // 绘制空气值图标，从右到左排列（因为右对齐）
            for (int i = 0; i < slotCount; i++)
            {
                // 计算图标位置：从右往左
                double iconLeft = airsWidth - (i + 1) * (airWidth + airGap) + airGap;

                // 空空气值（背景）
                var emptyImage = new System.Windows.Controls.Image
                {
                    Name = $"AirEmpty{i}",
                    Source = LoadBitmapImage(AssetPaths.AirEmpty),
                    Width = airWidth,
                    Height = airHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(emptyImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(emptyImage, iconLeft);
                Canvas.SetTop(emptyImage, 0);
                Canvas.SetZIndex(emptyImage, 0); // 最底层
                AirCanvas.Children.Add(emptyImage);

                // 满空气值
                var fullImage = new System.Windows.Controls.Image
                {
                    Name = $"AirFull{i}",
                    Source = LoadBitmapImage(AssetPaths.Air),
                    Width = airWidth,
                    Height = airHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(fullImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(fullImage, iconLeft);
                Canvas.SetTop(fullImage, 0);
                Canvas.SetZIndex(fullImage, 10); // 中间层
                AirCanvas.Children.Add(fullImage);
            }

            UpdateAirLevel();
        }

        /// <summary>
        /// 更新空气值显示
        /// currentValue决定显示的满图标数量
        /// </summary>
        private void UpdateAirLevel()
        {
            // 获取配置值
            var settings = GetHudElementSettings("Air");
            int maxValue = Math.Max(2, settings?.CustomMaxValue ?? 20);
            int slotCount = maxValue / 2;

            // 计算当前值
            int currentValue;
            bool dataMappingEnabled = settings?.CustomValueEnabled != true;

            if (dataMappingEnabled)
            {
                // 数据映射开启：从数据源获取百分比，转换为空气值
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

            // 向下取整到2的倍数
            currentValue = (currentValue / 2) * 2;

            // 满图标数量
            int fullAirs = currentValue / 2;

            // 检查是否需要触发动画（仅在动画效果开启、数据映射开启、且值减少时）
            bool animationEnabled = settings?.RegenAnimation ?? false;
            if (animationEnabled && dataMappingEnabled && _previousAirValue >= 0 && currentValue < _previousAirValue)
            {
                TriggerAirAnimation(currentValue, _previousAirValue, maxValue);
            }
            _previousAirValue = currentValue;

            for (int i = 0; i < slotCount; i++)
            {
                // 如果该位置正在播放动画，跳过（不改变其可见性）
                if (_airAnimationPlaying)
                {
                    var burstingImage = AirCanvas.Children.OfType<System.Windows.Controls.Image>()
                        .FirstOrDefault(img => img.Name == $"AirBursting{i}");
                    if (burstingImage != null && burstingImage.Visibility == Visibility.Visible)
                    {
                        continue; // 跳过正在播放动画的位置
                    }
                }

                var emptyImage = AirCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"AirEmpty{i}");
                var fullImage = AirCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"AirFull{i}");

                if (i < fullAirs)
                {
                    // 满空气
                    if (emptyImage != null) emptyImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Visible;
                }
                else
                {
                    // 空空气（只有背景empty可见）
                    if (emptyImage != null) emptyImage.Visibility = Visibility.Visible;
                    if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                }
            }
        }

        /// <summary>
        /// 触发空气值动画（仅在值减少时）
        /// </summary>
        private void TriggerAirAnimation(int newValue, int oldValue, int maxValue)
        {
            // 如果动画正在播放，先停止
            if (_airAnimationPlaying)
            {
                _airAnimationTimer?.Stop();
                ClearAirAnimationIcons();
            }

            double airWidth = _originalAirWidth * _scaleFactor;
            double airHeight = _originalAirHeight * _scaleFactor;
            double airGap = _airGap * _scaleFactor;
            int slotCount = maxValue / 2;

            // 计算消失的位置范围：从 newValue/2 到 oldValue/2-1
            int newFullAirs = newValue / 2;
            int oldFullAirs = oldValue / 2;

            // 在消失的位置显示 bursting 图标
            for (int i = newFullAirs; i < oldFullAirs; i++)
            {
                // 计算图标位置：从右往左（i=0是最右边）
                double iconLeft = AirCanvas.Width - (i + 1) * (airWidth + airGap) + airGap;

                // 隐藏该位置的 empty 和 full 图标
                var emptyImage = AirCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"AirEmpty{i}");
                if (emptyImage != null)
                {
                    emptyImage.Visibility = Visibility.Hidden;
                }

                var fullImage = AirCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"AirFull{i}");
                if (fullImage != null)
                {
                    fullImage.Visibility = Visibility.Hidden;
                }

                // 显示 bursting 图标
                var burstingImage = new System.Windows.Controls.Image
                {
                    Name = $"AirBursting{i}",
                    Source = LoadBitmapImage(AssetPaths.AirBursting),
                    Width = airWidth,
                    Height = airHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(burstingImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(burstingImage, iconLeft);
                Canvas.SetTop(burstingImage, 0);
                Canvas.SetZIndex(burstingImage, 20); // 最顶层，覆盖 empty 和 full
                AirCanvas.Children.Add(burstingImage);
            }

            _airAnimationPlaying = true;

            // 200ms 后移除动画图标
            _airAnimationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _airAnimationTimer.Tick += (_, _) =>
            {
                _airAnimationTimer.Stop();
                ClearAirAnimationIcons();
                _airAnimationPlaying = false;
            };
            _airAnimationTimer.Start();
        }

        /// <summary>
        /// 清除空气值动画图标
        /// </summary>
        private void ClearAirAnimationIcons()
        {
            // 移除所有 bursting 图标
            var burstingIcons = AirCanvas.Children.OfType<System.Windows.Controls.Image>()
                .Where(img => img.Name.Contains("Bursting"))
                .ToList();

            foreach (var icon in burstingIcons)
            {
                // 从图标名称提取索引（如 AirBursting3 → 3）
                string indexStr = icon.Name.Replace("AirBursting", "");
                if (int.TryParse(indexStr, out int i))
                {
                    // 恢复该位置的 empty 图标显示
                    var emptyImage = AirCanvas.Children.OfType<System.Windows.Controls.Image>()
                        .FirstOrDefault(img => img.Name == $"AirEmpty{i}");
                    if (emptyImage != null)
                    {
                        emptyImage.Visibility = Visibility.Visible;
                    }
                }

                AirCanvas.Children.Remove(icon);
            }
        }
    }
}