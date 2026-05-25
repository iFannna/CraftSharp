using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CraftSharp.Helpers
{
    /// <summary>
    /// 颜色选择器帮助类
    /// </summary>
    public static class ColorPickerHelper
    {
        /// <summary>
        /// 预设文件名颜色列表
        /// </summary>
        public static readonly string[] PresetColors = new string[]
        {
            "#FCFCFC", // 白色
            "#A8A8A8", // 灰色
            "#FCFC54", // 黄色
            "#5454FC", // 蓝色
            "#FC54FC", // 粉色
            "#A800A8", // 紫色
            "#FC5454", // 红色
            "#54FCFC", // 青色
            "#00A800"  // 绿色
        };

        /// <summary>
        /// 根据文件名计算自动颜色（相同文件名返回相同颜色）
        /// </summary>
        public static string GetAutoColorForFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return PresetColors[0];

            int hash = Math.Abs(fileName.GetHashCode());
            int index = hash % PresetColors.Length;
            return PresetColors[index];
        }

        /// <summary>
        /// 创建颜色下拉框选项（带颜色方块和十六进制值）
        /// </summary>
        public static ComboBoxItem CreateColorComboBoxItem(string colorHex)
        {
            var item = new ComboBoxItem
            {
                Tag = colorHex
            };

            // 创建内容面板
            var stackPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            // 颜色方块容器（带棋盘格背景显示透明效果）
            var colorBoxContainer = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 8, 0),
                Background = CreateCheckerboardBrush()
            };

            // 颜色方块（实际颜色）
            var colorBox = new Border
            {
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(ParseColorHex(colorHex))
            };
            colorBoxContainer.Child = colorBox;

            // 显示简化的文本（不带 Alpha 的 6 位格式）
            var displayText = colorHex;
            if (colorHex.StartsWith("#") && colorHex.Length == 9)
            {
                // 8 位格式，显示 RGB 部分
                displayText = "#" + colorHex.Substring(3);
            }

            // 十六进制值文本
            var textBlock = new TextBlock
            {
                Text = displayText
            };

            stackPanel.Children.Add(colorBoxContainer);
            stackPanel.Children.Add(textBlock);
            item.Content = stackPanel;

            return item;
        }

        /// <summary>
        /// 创建棋盘格背景画刷（用于显示透明效果）
        /// </summary>
        public static DrawingBrush CreateCheckerboardBrush()
        {
            var brush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 4, 4),
                ViewportUnits = BrushMappingMode.Absolute
            };

            var geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, 2, 2)));
            geometryGroup.Children.Add(new RectangleGeometry(new Rect(2, 2, 2, 2)));

            var drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(new GeometryDrawing(
                System.Windows.Media.Brushes.White, null,
                new RectangleGeometry(new Rect(0, 0, 4, 4))));
            drawingGroup.Children.Add(new GeometryDrawing(
                new SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204)), null,
                geometryGroup));

            brush.Drawing = drawingGroup;
            return brush;
        }

        /// <summary>
        /// 解析十六进制颜色字符串（支持 #RRGGBB 和 #AARRGGBB 格式）
        /// </summary>
        public static System.Windows.Media.Color ParseColorHex(string hex)
        {
            hex = hex.TrimStart('#');

            try
            {
                if (hex.Length == 8)
                {
                    // 8 位格式（#AARRGGBB），使用 Alpha
                    byte a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                    return System.Windows.Media.Color.FromArgb(a, r, g, b);
                }
                else if (hex.Length == 6)
                {
                    // 6 位格式（#RRGGBB），Alpha 默认 255
                    byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    return System.Windows.Media.Color.FromRgb(r, g, b);
                }
            }
            catch
            {
            }

            return System.Windows.Media.Colors.White;
        }

        /// <summary>
        /// 估算下拉框选项宽度
        /// </summary>
        public static double EstimateComboBoxItemWidth(string colorHex)
        {
            // 颜色方块宽度(16) + 间距(8) + 文本宽度
            double textWidth = EstimateTextWidth(colorHex);
            return 16 + 8 + textWidth;
        }

        /// <summary>
        /// 估算文本宽度
        /// </summary>
        public static double EstimateTextWidth(string text)
        {
            // 使用平均字符宽度估算（假设12px字体，每个字符约7px宽）
            return text.Length * 7 + 10; // 加10px padding
        }

        /// <summary>
        /// 计算下拉框最大宽度（根据所有选项内容）
        /// </summary>
        public static double CalculateMaxComboBoxWidth(string autoText, string otherText)
        {
            double maxWidth = 0;

            // 预设颜色选项
            foreach (var color in PresetColors)
            {
                double width = EstimateComboBoxItemWidth(color);
                if (width > maxWidth)
                    maxWidth = width;
            }

            // "自动"选项
            double autoWidth = EstimateTextWidth(autoText);
            if (autoWidth > maxWidth)
                maxWidth = autoWidth;

            // "其他..."选项
            double otherWidth = EstimateTextWidth(otherText);
            if (otherWidth > maxWidth)
                maxWidth = otherWidth;

            // 添加ComboBox边距和下拉箭头空间
            maxWidth += 60;

            // 限制最大宽度不超过300
            return Math.Min(maxWidth, 300);
        }
    }
}