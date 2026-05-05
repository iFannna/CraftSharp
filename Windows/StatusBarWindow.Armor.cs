using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 护甲值功能
    ///
    /// 布局规则：
    /// 1. 护甲值紧贴核心容器左边缘左对齐
    /// 2. 护甲值在伤害吸收值上方，间距6px基准
    /// 3. XAML顺序：护甲值(最先) → 伤害吸收 → 生命值(最后)
    /// 4. 实际显示：护甲值(最上) → 伤害吸收 → 生命值(最下)
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalArmorWidth;
        private double _originalArmorHeight;
        private double _originalHalfArmorWidth;
        private double _armorGap = -1; // 护甲值之间的间距（基准像素）

        /// <summary>
        /// 加载护甲值图片尺寸
        /// </summary>
        private void LoadArmorDimensions()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.ArmorFull);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalArmorWidth = frame.PixelWidth;
                    _originalArmorHeight = frame.PixelHeight;
                }
            }

            path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.ArmorHalf);
            if (System.IO.File.Exists(path))
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                    var frame = decoder.Frames[0];
                    _originalHalfArmorWidth = frame.PixelWidth;
                }
            }
        }

        /// <summary>
        /// 设置护甲值（紧贴核心容器左边缘左对齐，在伤害吸收值上方）
        /// 使用Canvas绘制护甲值图标，Grid用于容器布局
        /// </summary>
        private void SetupArmor()
        {
            double armorWidth = _originalArmorWidth * _scaleFactor;
            double armorHeight = _originalArmorHeight * _scaleFactor;
            double halfWidth = _originalHalfArmorWidth * _scaleFactor;
            double armorGap = _armorGap * _scaleFactor;

            // 设置ArmorGrid尺寸（暂时为空，等待实现）
            double armorsWidth = 10 * armorWidth + 9 * armorGap;
            ArmorGrid.Width = armorsWidth;
            ArmorGrid.Height = armorHeight;
            ArmorGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left; // 左对齐
            // 与下方伤害吸收值间距：6px基准（Margin.Bottom在上层元素上）
            ArmorGrid.Margin = new Thickness(0, 0, 0, BaseVerticalSpacing * _scaleFactor);
            ArmorGrid.Visibility = _armorVisible ? Visibility.Visible : Visibility.Collapsed;

            // 创建Canvas用于绘制护甲值图标
            Canvas armorCanvas = new Canvas
            {
                Width = armorsWidth,
                Height = armorHeight
            };

            // 清空ArmorGrid并添加Canvas
            ArmorGrid.Children.Clear();
            ArmorGrid.Children.Add(armorCanvas);

            // 绘制10个护甲值图标，从左到右排列
            for (int i = 0; i < 10; i++)
            {
                double iconLeft = i * (armorWidth + armorGap);

                // 空护甲值（背景）
                var emptyImage = new System.Windows.Controls.Image
                {
                    Name = $"ArmorEmpty{i}",
                    Source = LoadBitmapImage(AssetPaths.ArmorEmpty),
                    Width = armorWidth,
                    Height = armorHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(emptyImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(emptyImage, iconLeft);
                Canvas.SetTop(emptyImage, 0);
                armorCanvas.Children.Add(emptyImage);

                // 半护甲值
                var halfImage = new System.Windows.Controls.Image
                {
                    Name = $"ArmorHalf{i}",
                    Source = LoadBitmapImage(AssetPaths.ArmorHalf),
                    Width = halfWidth,
                    Height = armorHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(halfImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(halfImage, iconLeft);
                Canvas.SetTop(halfImage, 0);
                armorCanvas.Children.Add(halfImage);

                // 满护甲值
                var fullImage = new System.Windows.Controls.Image
                {
                    Name = $"ArmorFull{i}",
                    Source = LoadBitmapImage(AssetPaths.ArmorFull),
                    Width = armorWidth,
                    Height = armorHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(fullImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(fullImage, iconLeft);
                Canvas.SetTop(fullImage, 0);
                armorCanvas.Children.Add(fullImage);
            }

            UpdateArmorLevel();
        }

        /// <summary>
        /// 更新护甲值显示（暂时固定显示满值）
        /// TODO: 后续可连接到实际数据源
        /// </summary>
        private void UpdateArmorLevel()
        {
            // 暂时固定显示满护甲值
            int fullArmors = 10;

            Canvas armorCanvas = ArmorGrid.Children.OfType<Canvas>().FirstOrDefault();
            if (armorCanvas == null) return;

            for (int i = 0; i < 10; i++)
            {
                var halfImage = armorCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"ArmorHalf{i}");
                var fullImage = armorCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"ArmorFull{i}");

                if (i < fullArmors)
                {
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Visible;
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