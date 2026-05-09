using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CraftSharp.Models;
using CraftSharp.Helpers;
using CraftSharp.Services;

namespace CraftSharp.Windows.StatusBar
{
    /// <summary>
    /// 心形生命值功能
    ///
    /// 布局规则：
    /// 1. 生命值紧贴核心容器左边缘左对齐
    /// 2. 使用Canvas绘制心形，外层使用Grid+StackPanel布局
    /// 3. XAML顺序：护甲值(最先) → 伤害吸收 → 生命值(最后)
    /// 4. 实际显示：护甲值(最上) → 伤害吸收 → 生命值(最下)
    ///
    /// 图标规则：
    /// - maxValue上限20，代表10个完整图标（半心=1，满心=2）
    /// - maxValue决定显示的背景图标数量
    /// - currentValue决定显示的half/full图标数量
    /// - 背景使用container图标
    ///
    /// 动画规则（仅在恢复动画开关开启且数据映射开启时生效）：
    /// - 生命值增多或减少时：所有container变成container_blinking，持续1000ms
    /// - 生命值减少时：将旧值对应位置的图标变成blinking，新值图标叠加在上面
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalHeartWidth;
        private double _originalHeartHeight;
        private double _originalHalfHeartWidth;
        private double _heartGap = -1; // 心形之间的间距（基准像素）

        // 上一次的生命值（用于检测变化并触发动画）
        private int _previousHealthValue = -1;
        // 动画定时器
        private DispatcherTimer? _heartAnimationTimer;
        // 是否正在显示动画
        private bool _heartAnimationPlaying = false;
        // 动画阶段（用于实现闪烁两次）
        private int _heartAnimationPhase = 0;

        /// <summary>
        /// 加载心形图片尺寸
        /// </summary>
        private void LoadHeartDimensions()
        {
            var (width, height) = ImageService.Instance.GetImageDimensions(AssetPaths.HeartFull);
            _originalHeartWidth = width;
            _originalHeartHeight = height;

            var (halfWidth, _) = ImageService.Instance.GetImageDimensions(AssetPaths.HeartHalf);
            _originalHalfHeartWidth = halfWidth;
        }

        /// <summary>
        /// 设置心形生命值（紧贴核心容器左边缘左对齐）
        /// Canvas用于绘制心形，Grid用于容器布局
        /// </summary>
        private void SetupHearts()
        {
            double heartWidth = _originalHeartWidth * _scaleFactor;
            double heartHeight = _originalHeartHeight * _scaleFactor;
            double halfWidth = _originalHalfHeartWidth * _scaleFactor;
            double heartGap = _heartGap * _scaleFactor;

            // 获取配置值
            var settings = GetHudElementSettings("health");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int slotCount = maxValue / 2; // 每个槽位代表2点（一个完整图标）
            string iconStyle = settings?.IconStyle ?? ""; // 图标样式

            // 设置HeartCanvas尺寸（根据maxValue动态计算）
            double heartsWidth = slotCount * heartWidth + (slotCount - 1) * heartGap;
            HeartCanvas.Width = heartsWidth;
            HeartCanvas.Height = heartHeight;

            // 设置HeartGrid尺寸和布局
            HeartGrid.Width = heartsWidth;
            HeartGrid.Height = heartHeight;
            HeartGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left; // 左对齐
            // 生命值在最下方，无需上方间距（伤害吸收值会设置Margin）

            // 设置可见性
            HeartGrid.Visibility = _healthVisible ? Visibility.Visible : Visibility.Collapsed;

            // 清除现有心形
            HeartCanvas.Children.Clear();

            // 根据maxValue绘制槽位（背景图标）
            for (int i = 0; i < slotCount; i++)
            {
                double iconLeft = i * (heartWidth + heartGap);

                // 心形容器（背景）
                var containerImage = new System.Windows.Controls.Image
                {
                    Name = $"HeartContainer{i}",
                    Source = LoadBitmapImage(AssetPaths.GetHeartPathWithFallback(iconStyle, "container")),
                    Width = heartWidth,
                    Height = heartHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(containerImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(containerImage, iconLeft);
                Canvas.SetTop(containerImage, 0);
                HeartCanvas.Children.Add(containerImage);

                // 半心
                var halfImage = new System.Windows.Controls.Image
                {
                    Name = $"HeartHalf{i}",
                    Source = LoadBitmapImage(AssetPaths.GetHeartPathWithFallback(iconStyle, "half")),
                    Width = halfWidth,
                    Height = heartHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(halfImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(halfImage, iconLeft);
                Canvas.SetTop(halfImage, 0);
                HeartCanvas.Children.Add(halfImage);

                // 满心
                var fullImage = new System.Windows.Controls.Image
                {
                    Name = $"HeartFull{i}",
                    Source = LoadBitmapImage(AssetPaths.GetHeartPathWithFallback(iconStyle, "full")),
                    Width = heartWidth,
                    Height = heartHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(fullImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(fullImage, iconLeft);
                Canvas.SetTop(fullImage, 0);
                HeartCanvas.Children.Add(fullImage);
            }

            UpdateHeartLevel();
        }

        /// <summary>
        /// 更新心形生命值显示
        /// currentValue决定显示的half/full图标数量
        /// </summary>
        private void UpdateHeartLevel()
        {
            // 获取配置值
            var settings = GetHudElementSettings("health");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int slotCount = maxValue / 2;

            // 计算当前值
            int currentValue;
            bool dataMappingEnabled = settings?.CustomValueEnabled != true;

            if (dataMappingEnabled)
            {
                // 数据映射开启：从数据源获取百分比，转换为生命值
                string mappingType = settings?.DataMappingType ?? "电池电量";
                double percent = DataMappingService.Instance.GetValue(mappingType);
                currentValue = (int)(percent * maxValue);
            }
            else
            {
                // 自定义数值开启：使用配置的当前值
                currentValue = settings?.CustomCurrentValue ?? 20;
            }

            // 计算完整和半心数量
            // currentValue: 半心=1, 满心=2
            // 例如: currentValue=15 → 7满心(14) + 1半心(1)
            int fullHearts = currentValue / 2;
            bool hasHalfHeart = (currentValue % 2) == 1;

            // 检查是否需要触发动画（仅在恢复动画开启且数据映射开启时）
            bool regenAnimationEnabled = settings?.RegenAnimation ?? false;
            if (regenAnimationEnabled && dataMappingEnabled && _previousHealthValue >= 0 && _previousHealthValue != currentValue)
            {
                TriggerHeartAnimation(_previousHealthValue, currentValue, maxValue);
            }
            _previousHealthValue = currentValue;

            for (int i = 0; i < slotCount; i++)
            {
                var halfImage = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"HeartHalf{i}");
                var fullImage = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"HeartFull{i}");

                if (i < fullHearts)
                {
                    // 满心
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Visible;
                }
                else if (i == fullHearts && hasHalfHeart)
                {
                    // 半心
                    if (halfImage != null) halfImage.Visibility = Visibility.Visible;
                    if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                }
                else
                {
                    // 空（只有背景container可见）
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Hidden;
                }
            }
        }

        /// <summary>
        /// 触发生命值动画
        /// </summary>
        private void TriggerHeartAnimation(int oldValue, int newValue, int maxValue)
        {
            // 如果动画正在播放，先停止
            if (_heartAnimationPlaying)
            {
                _heartAnimationTimer?.Stop();
                ClearHeartAnimationIcons();
            }

            // 获取图标样式
            var settings = GetHudElementSettings("health");
            string iconStyle = settings?.IconStyle ?? "";

            double heartWidth = _originalHeartWidth * _scaleFactor;
            double heartHeight = _originalHeartHeight * _scaleFactor;
            double halfWidth = _originalHalfHeartWidth * _scaleFactor;
            double heartGap = _heartGap * _scaleFactor;
            int slotCount = maxValue / 2;

            bool isDecreasing = newValue < oldValue;

            // 计算旧值和新值的心形状态
            int oldFullHearts = oldValue / 2;
            bool oldHasHalfHeart = (oldValue % 2) == 1;
            int newFullHearts = newValue / 2;
            bool newHasHalfHeart = (newValue % 2) == 1;

            // 1. 隐藏原始 container 图标，显示 container_blinking
            for (int i = 0; i < slotCount; i++)
            {
                var container = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"HeartContainer{i}");
                if (container != null)
                {
                    container.Visibility = Visibility.Hidden;
                }

                double iconLeft = i * (heartWidth + heartGap);

                var containerBlinking = new System.Windows.Controls.Image
                {
                    Name = $"HeartContainerBlinking{i}",
                    Source = LoadBitmapImage(AssetPaths.GetHeartPathWithFallback(iconStyle, "container_blinking")),
                    Width = heartWidth,
                    Height = heartHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(containerBlinking, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(containerBlinking, iconLeft);
                Canvas.SetTop(containerBlinking, 0);
                Canvas.SetZIndex(containerBlinking, 0); // 最底层
                HeartCanvas.Children.Add(containerBlinking);
            }

            // 2. 设置 half/full 图标的 Z-index 为较高值，确保它们在 container_blinking 之上
            for (int i = 0; i < slotCount; i++)
            {
                var halfImage = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"HeartHalf{i}");
                var fullImage = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"HeartFull{i}");

                if (halfImage != null) Canvas.SetZIndex(halfImage, 10);
                if (fullImage != null) Canvas.SetZIndex(fullImage, 10);
            }

            // 3. 生命值减少时：在减少的位置显示 full/half_blinking，新值图标叠加在上面
            if (isDecreasing)
            {
                for (int i = 0; i < slotCount; i++)
                {
                    // 判断该位置旧值的状态
                    bool oldIsFull = i < oldFullHearts;
                    bool oldIsHalf = i == oldFullHearts && oldHasHalfHeart;

                    // 判断该位置新值的状态
                    bool newIsFull = i < newFullHearts;
                    bool newIsHalf = i == newFullHearts && newHasHalfHeart;

                    // 只在旧值有图标且新值没有（或更少）的位置显示 blinking
                    if (oldIsFull && !newIsFull)
                    {
                        double iconLeft = i * (heartWidth + heartGap);

                        // 显示 full_blinking
                        var fullBlinking = new System.Windows.Controls.Image
                        {
                            Name = $"HeartFullBlinking{i}",
                            Source = LoadBitmapImage(AssetPaths.GetHeartPathWithFallback(iconStyle, "full_blinking")),
                            Width = heartWidth,
                            Height = heartHeight,
                            Stretch = Stretch.Uniform,
                            UseLayoutRounding = true,
                            SnapsToDevicePixels = true
                        };
                        RenderOptions.SetBitmapScalingMode(fullBlinking, BitmapScalingMode.NearestNeighbor);
                        Canvas.SetLeft(fullBlinking, iconLeft);
                        Canvas.SetTop(fullBlinking, 0);
                        Canvas.SetZIndex(fullBlinking, 5); // 中间层
                        HeartCanvas.Children.Add(fullBlinking);

                        // 如果新值是半心，叠加半心图标
                        if (newIsHalf)
                        {
                            var halfOverlay = new System.Windows.Controls.Image
                            {
                                Name = $"HeartHalfOverlay{i}",
                                Source = LoadBitmapImage(AssetPaths.GetHeartPathWithFallback(iconStyle, "half")),
                                Width = halfWidth,
                                Height = heartHeight,
                                Stretch = Stretch.Uniform,
                                UseLayoutRounding = true,
                                SnapsToDevicePixels = true
                            };
                            RenderOptions.SetBitmapScalingMode(halfOverlay, BitmapScalingMode.NearestNeighbor);
                            Canvas.SetLeft(halfOverlay, iconLeft);
                            Canvas.SetTop(halfOverlay, 0);
                            Canvas.SetZIndex(halfOverlay, 15); // 最顶层
                            HeartCanvas.Children.Add(halfOverlay);
                        }
                    }
                    else if (oldIsHalf && !newIsFull && !newIsHalf)
                    {
                        double iconLeft = i * (heartWidth + heartGap);

                        // 显示 half_blinking
                        var halfBlinking = new System.Windows.Controls.Image
                        {
                            Name = $"HeartHalfBlinking{i}",
                            Source = LoadBitmapImage(AssetPaths.GetHeartPathWithFallback(iconStyle, "half_blinking")),
                            Width = halfWidth,
                            Height = heartHeight,
                            Stretch = Stretch.Uniform,
                            UseLayoutRounding = true,
                            SnapsToDevicePixels = true
                        };
                        RenderOptions.SetBitmapScalingMode(halfBlinking, BitmapScalingMode.NearestNeighbor);
                        Canvas.SetLeft(halfBlinking, iconLeft);
                        Canvas.SetTop(halfBlinking, 0);
                        Canvas.SetZIndex(halfBlinking, 5); // 中间层
                        HeartCanvas.Children.Add(halfBlinking);
                    }
                }
            }

            _heartAnimationPlaying = true;
            _heartAnimationPhase = 0;

            // 闪烁两次动画：200ms闪烁 → 100ms休息 → 200ms闪烁 → 结束
            _heartAnimationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _heartAnimationTimer.Tick += (s, e) =>
            {
                _heartAnimationPhase++;

                if (_heartAnimationPhase == 1)
                {
                    // 第一次闪烁结束，清除所有blinking图标（休息阶段）
                    ClearHeartBlinkingIconsOnly();
                    _heartAnimationTimer.Interval = TimeSpan.FromMilliseconds(100);
                }
                else if (_heartAnimationPhase == 2)
                {
                    // 休息结束，第二次闪烁开始
                    ShowHeartContainerBlinking(maxValue);
                    ShowHeartBlinkingIcons(oldValue, newValue, maxValue);
                    _heartAnimationTimer.Interval = TimeSpan.FromMilliseconds(200);
                }
                else if (_heartAnimationPhase == 3)
                {
                    // 第二次闪烁结束，完全清除动画图标
                    _heartAnimationTimer.Stop();
                    ClearHeartAnimationIcons();
                    _heartAnimationPlaying = false;
                    _heartAnimationPhase = 0;
                }
            };
            _heartAnimationTimer.Start();
        }

        /// <summary>
        /// 清除生命值动画图标
        /// </summary>
        private void ClearHeartAnimationIcons()
        {
            // 移除所有 blinking 相关图标
            var blinkingIcons = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                .Where(img => img.Name.Contains("Blinking") || img.Name.Contains("Overlay"))
                .ToList();

            foreach (var icon in blinkingIcons)
            {
                HeartCanvas.Children.Remove(icon);
            }

            // 恢复原始 container 的可见性
            var settings = GetHudElementSettings("health");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int slotCount = maxValue / 2;

            for (int i = 0; i < slotCount; i++)
            {
                var container = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"HeartContainer{i}");
                if (container != null)
                {
                    container.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 清除 blinking 图标（用于休息阶段，恢复原始状态）
        /// </summary>
        private void ClearHeartBlinkingIconsOnly()
        {
            // 移除 full/half_blinking 和 overlay 图标
            var blinkingIcons = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                .Where(img => img.Name.Contains("FullBlinking") || img.Name.Contains("HalfBlinking") || img.Name.Contains("Overlay"))
                .ToList();

            foreach (var icon in blinkingIcons)
            {
                HeartCanvas.Children.Remove(icon);
            }

            // 移除 container_blinking，恢复原始 container
            var containerBlinkingIcons = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                .Where(img => img.Name.Contains("ContainerBlinking"))
                .ToList();

            foreach (var icon in containerBlinkingIcons)
            {
                HeartCanvas.Children.Remove(icon);
            }

            // 恢复原始 container 的可见性
            var settings = GetHudElementSettings("health");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int slotCount = maxValue / 2;

            for (int i = 0; i < slotCount; i++)
            {
                var container = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"HeartContainer{i}");
                if (container != null)
                {
                    container.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 显示 blinking 图标（用于第二次闪烁）
        /// </summary>
        private void ShowHeartBlinkingIcons(int oldValue, int newValue, int maxValue)
        {
            // 获取图标样式
            var settings = GetHudElementSettings("health");
            string iconStyle = settings?.IconStyle ?? "";

            double heartWidth = _originalHeartWidth * _scaleFactor;
            double heartHeight = _originalHeartHeight * _scaleFactor;
            double halfWidth = _originalHalfHeartWidth * _scaleFactor;
            double heartGap = _heartGap * _scaleFactor;
            int slotCount = maxValue / 2;

            bool isDecreasing = newValue < oldValue;

            // 计算旧值和新值的心形状态
            int oldFullHearts = oldValue / 2;
            bool oldHasHalfHeart = (oldValue % 2) == 1;
            int newFullHearts = newValue / 2;
            bool newHasHalfHeart = (newValue % 2) == 1;

            // 生命值减少时：在减少的位置显示 full/half_blinking
            if (isDecreasing)
            {
                for (int i = 0; i < slotCount; i++)
                {
                    // 判断该位置旧值的状态
                    bool oldIsFull = i < oldFullHearts;
                    bool oldIsHalf = i == oldFullHearts && oldHasHalfHeart;

                    // 判断该位置新值的状态
                    bool newIsFull = i < newFullHearts;
                    bool newIsHalf = i == newFullHearts && newHasHalfHeart;

                    // 只在旧值有图标且新值没有（或更少）的位置显示 blinking
                    if (oldIsFull && !newIsFull)
                    {
                        double iconLeft = i * (heartWidth + heartGap);

                        // 显示 full_blinking
                        var fullBlinking = new System.Windows.Controls.Image
                        {
                            Name = $"HeartFullBlinking{i}",
                            Source = LoadBitmapImage(AssetPaths.GetHeartPathWithFallback(iconStyle, "full_blinking")),
                            Width = heartWidth,
                            Height = heartHeight,
                            Stretch = Stretch.Uniform,
                            UseLayoutRounding = true,
                            SnapsToDevicePixels = true
                        };
                        RenderOptions.SetBitmapScalingMode(fullBlinking, BitmapScalingMode.NearestNeighbor);
                        Canvas.SetLeft(fullBlinking, iconLeft);
                        Canvas.SetTop(fullBlinking, 0);
                        Canvas.SetZIndex(fullBlinking, 5); // 中间层
                        HeartCanvas.Children.Add(fullBlinking);

                        // 如果新值是半心，叠加半心图标
                        if (newIsHalf)
                        {
                            var halfOverlay = new System.Windows.Controls.Image
                            {
                                Name = $"HeartHalfOverlay{i}",
                                Source = LoadBitmapImage(AssetPaths.GetHeartPathWithFallback(iconStyle, "half")),
                                Width = halfWidth,
                                Height = heartHeight,
                                Stretch = Stretch.Uniform,
                                UseLayoutRounding = true,
                                SnapsToDevicePixels = true
                            };
                            RenderOptions.SetBitmapScalingMode(halfOverlay, BitmapScalingMode.NearestNeighbor);
                            Canvas.SetLeft(halfOverlay, iconLeft);
                            Canvas.SetTop(halfOverlay, 0);
                            Canvas.SetZIndex(halfOverlay, 15); // 最顶层
                            HeartCanvas.Children.Add(halfOverlay);
                        }
                    }
                    else if (oldIsHalf && !newIsFull && !newIsHalf)
                    {
                        double iconLeft = i * (heartWidth + heartGap);

                        // 显示 half_blinking
                        var halfBlinking = new System.Windows.Controls.Image
                        {
                            Name = $"HeartHalfBlinking{i}",
                            Source = LoadBitmapImage(AssetPaths.GetHeartPathWithFallback(iconStyle, "half_blinking")),
                            Width = halfWidth,
                            Height = heartHeight,
                            Stretch = Stretch.Uniform,
                            UseLayoutRounding = true,
                            SnapsToDevicePixels = true
                        };
                        RenderOptions.SetBitmapScalingMode(halfBlinking, BitmapScalingMode.NearestNeighbor);
                        Canvas.SetLeft(halfBlinking, iconLeft);
                        Canvas.SetTop(halfBlinking, 0);
                        Canvas.SetZIndex(halfBlinking, 5); // 中间层
                        HeartCanvas.Children.Add(halfBlinking);
                    }
                }
            }
        }

        /// <summary>
        /// 显示 container_blinking 图标（用于第二次闪烁）
        /// </summary>
        private void ShowHeartContainerBlinking(int maxValue)
        {
            // 获取图标样式
            var settings = GetHudElementSettings("health");
            string iconStyle = settings?.IconStyle ?? "";

            double heartWidth = _originalHeartWidth * _scaleFactor;
            double heartHeight = _originalHeartHeight * _scaleFactor;
            double heartGap = _heartGap * _scaleFactor;
            int slotCount = maxValue / 2;

            // 隐藏原始 container 图标
            for (int i = 0; i < slotCount; i++)
            {
                var container = HeartCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"HeartContainer{i}");
                if (container != null)
                {
                    container.Visibility = Visibility.Hidden;
                }

                double iconLeft = i * (heartWidth + heartGap);

                // 显示 container_blinking
                var containerBlinking = new System.Windows.Controls.Image
                {
                    Name = $"HeartContainerBlinking{i}",
                    Source = LoadBitmapImage(AssetPaths.GetHeartPathWithFallback(iconStyle, "container_blinking")),
                    Width = heartWidth,
                    Height = heartHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(containerBlinking, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(containerBlinking, iconLeft);
                Canvas.SetTop(containerBlinking, 0);
                Canvas.SetZIndex(containerBlinking, 0); // 最底层
                HeartCanvas.Children.Add(containerBlinking);
            }
        }
    }
}