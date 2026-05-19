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
    /// 伤害吸收值功能
    ///
    /// 布局规则：
    /// 1. 伤害吸收值紧贴核心容器左边缘左对齐
    /// 2. 伤害吸收值在生命值上方，间距动态调整
    /// 3. XAML顺序：护甲值(最先) → 伤害吸收 → 生命值(最后)
    /// 4. 实际显示：护甲值(最上) → 伤害吸收 → 生命值(最下)
    ///
    /// 图标规则：
    /// - maxValue上限1024，代表最多512个完整图标（半吸收=1，满吸收=2）
    /// - 每行最多10个图标，超过则往上面加一行
    /// - 从下往上堆叠（第一行在最下面）
    /// - 后面的图标被前面的图标遮挡（层叠效果）
    /// - maxValue决定显示的背景图标数量
    /// - currentValue决定显示的half/full图标数量
    /// - 背景使用container图标（心形容器）
    ///
    /// 间距规则：
    /// - 1行：间距 = BaseVerticalSpacing
    /// - 2行：间距 = BaseVerticalSpacing - 1
    /// - 3行：间距 = BaseVerticalSpacing - 2
    /// - ...最多减7（8行及以上都是BaseVerticalSpacing - 7）
    /// - 此规则同时应用于行间距和与生命值的间距
    /// </summary>
    public partial class StatusBarWindow
    {
        private double _originalAbsorbingFullWidth;
        private double _originalAbsorbingFullHeight;
        private double _originalAbsorbingHalfWidth;
        private double _absorbingGap = -1; // 伤害吸收值之间的间距（基准像素）
        private const int AbsorbingIconsPerRow = 10; // 每行图标数量

        /// <summary>
        /// 加载伤害吸收值图片尺寸
        /// </summary>
        private void LoadAbsorbingDimensions()
        {
            var (width, height) = ImageService.Instance.GetImageDimensions(AssetPaths.AbsorbingFull);
            _originalAbsorbingFullWidth = width;
            _originalAbsorbingFullHeight = height;

            var (halfWidth, _) = ImageService.Instance.GetImageDimensions(AssetPaths.AbsorbingHalf);
            _originalAbsorbingHalfWidth = halfWidth;
        }

        /// <summary>
        /// 计算行间距调整值（根据行数递减，最多减7）
        /// </summary>
        private int GetRowGapAdjustment(int totalRows)
        {
            // 1行不减，2行减1，3行减2...最多减7
            return Math.Min(totalRows - 1, 7);
        }

        /// <summary>
        /// 设置伤害吸收值（紧贴核心容器左边缘左对齐，在生命值上方）
        /// Canvas用于绘制伤害吸收值图标，Grid用于容器布局
        /// 支持多行显示，每行10个图标，从下往上堆叠
        /// </summary>
        private void SetupAbsorbing()
        {
            double absorbingWidth = _originalAbsorbingFullWidth * _scaleFactor;
            double absorbingHeight = _originalAbsorbingFullHeight * _scaleFactor;
            double halfWidth = _originalAbsorbingHalfWidth * _scaleFactor;
            double absorbingGap = _absorbingGap * _scaleFactor;

            // 获取配置值
            var settings = GetHudElementSettings("Absorbing");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int slotCount = maxValue / 2; // 每个槽位代表2点（一个完整图标）
            string iconStyle = settings?.IconStyle ?? ""; // 图标样式

            // 计算行数（向上取整）
            int totalRows = (slotCount == 0) ? 1 : (slotCount - 1) / AbsorbingIconsPerRow + 1;

            // 计算行间距调整值
            int gapAdjustment = GetRowGapAdjustment(totalRows);
            double rowGap = (BaseVerticalSpacing - gapAdjustment) * _scaleFactor; // 行间距

            // 设置AbsorbingCanvas尺寸（根据行数动态计算）
            double rowWidth = AbsorbingIconsPerRow * absorbingWidth + (AbsorbingIconsPerRow - 1) * absorbingGap;
            double totalHeight = totalRows * absorbingHeight + (totalRows - 1) * rowGap;
            AbsorbingCanvas.Width = rowWidth;
            AbsorbingCanvas.Height = totalHeight;

            // 设置AbsorbingGrid尺寸和布局
            AbsorbingGrid.Width = rowWidth;
            AbsorbingGrid.Height = totalHeight;
            AbsorbingGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left; // 左对齐
            // 与下方生命值间距：同样应用行间距调整规则
            AbsorbingGrid.Margin = new Thickness(0, 0, 0, (BaseVerticalSpacing - gapAdjustment) * _scaleFactor);
            AbsorbingGrid.Visibility = _absorbingVisible ? Visibility.Visible : Visibility.Collapsed;

            // 清除现有伤害吸收值图标
            AbsorbingCanvas.Children.Clear();

            // 根据maxValue绘制槽位（背景图标）
            // 从下往上堆叠：行号0在最下面，行号越大越在上面
            // 图标层叠效果：后面加的图标层级更低，被前面的图标遮挡
            // 因此需要从最后一个槽位开始添加（槽位slotCount-1最先添加，在最下面）
            // 槽位0最后添加，在最上面，遮挡后面的图标
            for (int i = slotCount - 1; i >= 0; i--)
            {
                int row = i / AbsorbingIconsPerRow;        // 行号（0是第一行/最下面）
                int col = i % AbsorbingIconsPerRow;        // 列号
                // Y坐标：从下往上堆叠，所以最下面的行(row=0)的Y坐标是最大值
                double iconLeft = col * (absorbingWidth + absorbingGap);
                double iconTop = (totalRows - 1 - row) * (absorbingHeight + rowGap);

                // 同一个槽位内：container在最下面，half在中间，full在最上面
                // 因此先添加container，再添加half，最后添加full

                // 心形容器（背景）
                var containerImage = new System.Windows.Controls.Image
                {
                    Name = $"AbsorbingContainer{i}",
                    Source = LoadBitmapImage(AssetPaths.GetHeartPathWithFallback(iconStyle, "container")),
                    Width = absorbingWidth,
                    Height = absorbingHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(containerImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(containerImage, iconLeft);
                Canvas.SetTop(containerImage, iconTop);
                AbsorbingCanvas.Children.Add(containerImage);

                // 半伤害吸收值
                var halfImage = new System.Windows.Controls.Image
                {
                    Name = $"AbsorbingHalf{i}",
                    Source = LoadBitmapImage(AssetPaths.GetAbsorbingPathWithFallback(iconStyle, "half")),
                    Width = halfWidth,
                    Height = absorbingHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(halfImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(halfImage, iconLeft);
                Canvas.SetTop(halfImage, iconTop);
                AbsorbingCanvas.Children.Add(halfImage);

                // 满伤害吸收值
                var fullImage = new System.Windows.Controls.Image
                {
                    Name = $"AbsorbingFull{i}",
                    Source = LoadBitmapImage(AssetPaths.GetAbsorbingPathWithFallback(iconStyle, "full")),
                    Width = absorbingWidth,
                    Height = absorbingHeight,
                    Stretch = Stretch.Uniform,
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(fullImage, BitmapScalingMode.NearestNeighbor);
                Canvas.SetLeft(fullImage, iconLeft);
                Canvas.SetTop(fullImage, iconTop);
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
            var settings = GetHudElementSettings("Absorbing");
            int maxValue = settings?.CustomMaxValue ?? 20;
            int slotCount = maxValue / 2;

            // 计算当前值
            int currentValue;
            bool dataMappingEnabled = settings?.CustomValueEnabled != true;

            if (dataMappingEnabled)
            {
                // 数据映射开启：从数据源获取百分比，转换为伤害吸收值
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