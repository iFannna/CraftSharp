using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CraftSharp.Models;
using CraftSharp.Helpers;
using CraftSharp.Services;

namespace CraftSharp.Windows.StatusBar
{
    /// <summary>
    /// 护甲值功能
    ///
    /// 布局规则：
    /// 1. 护甲值紧贴核心容器左边缘左对齐
    /// 2. 护甲值在伤害吸收值上方，间距6px基准
    /// 3. XAML顺序：护甲值(最先) → 伤害吸收 → 生命值(最后)
    /// 4. 实际显示：护甲值(最上) → 伤害吸收 → 生命值(最下)
    ///
    /// 图标规则：
    /// - maxValue上限20，代表10个完整图标（半护甲=1，满护甲=2）
    /// - maxValue决定显示的背景图标数量
    /// - currentValue决定显示的half/full图标数量
    /// - 背景使用empty图标
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
            var (width, height) = ImageService.Instance.GetImageDimensions(AssetPaths.ArmorFull);
            _originalArmorWidth = width;
            _originalArmorHeight = height;

            var (halfWidth, _) = ImageService.Instance.GetImageDimensions(AssetPaths.ArmorHalf);
            _originalHalfArmorWidth = halfWidth;
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

            // 获取配置值
            var settings = GetHudElementSettings("armor");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int slotCount = maxValue / 2; // 每个槽位代表2点（一个完整图标）

            // 设置ArmorGrid尺寸（根据maxValue动态计算）
            double armorsWidth = slotCount * armorWidth + (slotCount - 1) * armorGap;
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

            // 根据maxValue绘制槽位（背景图标）
            for (int i = 0; i < slotCount; i++)
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
        /// 更新护甲值显示
        /// currentValue决定显示的half/full图标数量
        /// </summary>
        private void UpdateArmorLevel()
        {
            // 获取配置值
            var settings = GetHudElementSettings("armor");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int slotCount = maxValue / 2;

            Canvas armorCanvas = ArmorGrid.Children.OfType<Canvas>().FirstOrDefault();
            if (armorCanvas == null) return;

            // 计算当前值
            int currentValue;
            bool dataMappingEnabled = settings?.CustomValueEnabled != true;

            if (dataMappingEnabled)
            {
                // 数据映射开启：从数据源获取百分比，转换为护甲值
                string mappingType = settings?.DataMappingType ?? "电池电量";
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

            // 计算完整和半护甲数量
            // currentValue: 半护甲=1, 满护甲=2
            int fullArmors = currentValue / 2;
            bool hasHalfArmor = (currentValue % 2) == 1;

            for (int i = 0; i < slotCount; i++)
            {
                var halfImage = armorCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"ArmorHalf{i}");
                var fullImage = armorCanvas.Children.OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Name == $"ArmorFull{i}");

                if (i < fullArmors)
                {
                    // 满护甲
                    if (halfImage != null) halfImage.Visibility = Visibility.Hidden;
                    if (fullImage != null) fullImage.Visibility = Visibility.Visible;
                }
                else if (i == fullArmors && hasHalfArmor)
                {
                    // 半护甲
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