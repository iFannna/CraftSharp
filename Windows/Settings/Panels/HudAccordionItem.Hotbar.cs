using CraftSharp.Models;
using CraftSharp.Services;
using CraftSharp.Helpers;
using CraftSharp.Windows.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Settings.Panels
{
    /// <summary>
    /// HudAccordionItem 快捷栏配置
    /// </summary>
    public partial class HudAccordionItem
    {
        // 预设文件名颜色列表
        private static readonly string[] PresetFileNameColors = new string[]
        {
            "#FFFFFF", // 白色
            "#FFFF55", // 黄色
            "#FFAA00", // 金色
            "#AA00AA", // 紫色
            "#FF5555", // 红色
            "#55FFFF"  // 青色
        };

        private void AddHotbarContent()
        {
            AddFileNameColorComboBox();
            AddClickModeComboBox();

            var hotbarToggle = AddToggleRow("HudOptionShowHotbar", "HudOptionShowHotbarDesc", _settings.Hotbar.Visible);
            hotbarToggle.Checked += (s, e) => { _settings.Hotbar.Visible = true; StatusBarService.Instance.SetHotbarVisible(true); SaveSettings(); };
            hotbarToggle.Unchecked += (s, e) => { _settings.Hotbar.Visible = false; StatusBarService.Instance.SetHotbarVisible(false); SaveSettings(); };

            var hoverToggle = AddToggleRow("HudOptionHoverEffect", "HudOptionHoverEffectDesc", _settings.Hotbar.HoverEffect);
            hoverToggle.Checked += (s, e) => { _settings.Hotbar.HoverEffect = true; StatusBarService.Instance.SetHotbarHoverEffect(true); SaveSettings(); };
            hoverToggle.Unchecked += (s, e) => { _settings.Hotbar.HoverEffect = false; StatusBarService.Instance.SetHotbarHoverEffect(false); SaveSettings(); };

            var leftOffhandToggle = AddToggleRow("HudOptionLeftOffhand", "HudOptionLeftOffhandDesc", _settings.Hotbar.LeftOffhand);
            leftOffhandToggle.Checked += (s, e) => { _settings.Hotbar.LeftOffhand = true; StatusBarService.Instance.SetOffhandConfig(true, _settings.Hotbar.RightOffhand); SaveSettings(); };
            leftOffhandToggle.Unchecked += (s, e) => { _settings.Hotbar.LeftOffhand = false; StatusBarService.Instance.SetOffhandConfig(false, _settings.Hotbar.RightOffhand); SaveSettings(); };

            var rightOffhandToggle = AddToggleRow("HudOptionRightOffhand", "HudOptionRightOffhandDesc", _settings.Hotbar.RightOffhand);
            rightOffhandToggle.Checked += (s, e) => { _settings.Hotbar.RightOffhand = true; StatusBarService.Instance.SetOffhandConfig(_settings.Hotbar.LeftOffhand, true); SaveSettings(); };
            rightOffhandToggle.Unchecked += (s, e) => { _settings.Hotbar.RightOffhand = false; StatusBarService.Instance.SetOffhandConfig(_settings.Hotbar.LeftOffhand, false); SaveSettings(); };

            var showTargetIconToggle = AddToggleRow("HudOptionShowTargetIcon", "HudOptionShowTargetIconDesc", _settings.Hotbar.ShowTargetIcon);
            showTargetIconToggle.Checked += (s, e) => {
                _settings.Hotbar.ShowTargetIcon = true;
                StatusBarService.Instance.RefreshHotbarIcons();
                if (System.Windows.Application.Current is App app) app.GetInventoryWindow()?.RefreshIcons();
                SaveSettings();
            };
            showTargetIconToggle.Unchecked += (s, e) => {
                _settings.Hotbar.ShowTargetIcon = false;
                StatusBarService.Instance.RefreshHotbarIcons();
                if (System.Windows.Application.Current is App app) app.GetInventoryWindow()?.RefreshIcons();
                SaveSettings();
            };
        }

        /// <summary>
        /// 添加文件名颜色选择下拉框
        /// </summary>
        private void AddFileNameColorComboBox()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("HudOptionFileNameColor"),
                FontWeight = FontWeights.Medium
            };
            titleLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextPrimaryBrush");
            var descLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("HudOptionFileNameColorDesc"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            descLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            // 预先计算所有选项的最大宽度
            double maxWidth = CalculateMaxComboBoxWidth();

            var comboBox = new System.Windows.Controls.ComboBox
            {
                Width = maxWidth,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left
            };

            string currentColor = _settings.Hotbar.FileNameColor;
            string? customColor = _settings.Hotbar.CustomFileNameColor;

            // 是否有自定义颜色
            bool hasCustomColor = !string.IsNullOrEmpty(customColor) && !PresetFileNameColors.Contains(customColor);

            // 构建下拉框选项
            int selectedIndex = -1;

            // 1. 如果有自定义颜色，添加自定义颜色选项在最前面
            if (hasCustomColor)
            {
                var customItem = CreateColorComboBoxItem(customColor, true);
                comboBox.Items.Add(customItem);
                if (currentColor == customColor)
                    selectedIndex = 0;
            }

            // 2. 添加预设颜色
            for (int i = 0; i < PresetFileNameColors.Length; i++)
            {
                var presetItem = CreateColorComboBoxItem(PresetFileNameColors[i], false);
                comboBox.Items.Add(presetItem);
                if (currentColor == PresetFileNameColors[i])
                    selectedIndex = hasCustomColor ? i + 1 : i;
            }

            // 3. 添加"其他..."选项
            var otherItem = new System.Windows.Controls.ComboBoxItem
            {
                Content = GetResourceString("HudOptionFileNameColorOther"),
                Tag = "other"
            };
            comboBox.Items.Add(otherItem);

            // 设置选中项
            if (selectedIndex >= 0)
                comboBox.SelectedIndex = selectedIndex;

            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                {
                    string tag = item.Tag?.ToString() ?? "";

                    if (tag == "other")
                    {
                        // 打开颜色选择弹窗
                        var colorPicker = new ColorPickerWindow(currentColor);
                        colorPicker.Owner = System.Windows.Window.GetWindow(this);

                        if (colorPicker.ShowDialog() == true)
                        {
                            string newColor = colorPicker.SelectedColorHex;

                            // 更新配置
                            _settings.Hotbar.FileNameColor = newColor;
                            _settings.Hotbar.CustomFileNameColor = newColor;
                            SaveSettings();

                            // 更新 StatusBarWindow
                            StatusBarService.Instance.RefreshFileNameColor();

                            // 刷新下拉框选项
                            RefreshFileNameColorComboBox(comboBox, newColor);
                        }
                        else
                        {
                            // 用户取消，恢复之前选中
                            RefreshFileNameColorComboBox(comboBox, currentColor);
                        }
                    }
                    else if (tag.StartsWith("#"))
                    {
                        // 预设颜色或自定义颜色
                        _settings.Hotbar.FileNameColor = tag;
                        if (!PresetFileNameColors.Contains(tag))
                        {
                            // 如果是自定义颜色，保留配置
                            _settings.Hotbar.CustomFileNameColor = tag;
                        }
                        SaveSettings();

                        // 更新 StatusBarWindow
                        StatusBarService.Instance.RefreshFileNameColor();

                        currentColor = tag;
                    }
                }
            };

            grid.Children.Add(comboBox);
            Grid.SetColumn(comboBox, 1);

            ContentPanel.Children.Add(grid);
        }

        /// <summary>
        /// 创建颜色下拉框选项（带颜色方块和十六进制值）
        /// </summary>
        private System.Windows.Controls.ComboBoxItem CreateColorComboBoxItem(string colorHex, bool isCustom)
        {
            var item = new System.Windows.Controls.ComboBoxItem
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
            var textBlock = new System.Windows.Controls.TextBlock
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
        private System.Windows.Media.DrawingBrush CreateCheckerboardBrush()
        {
            var brush = new System.Windows.Media.DrawingBrush
            {
                TileMode = System.Windows.Media.TileMode.Tile,
                Viewport = new System.Windows.Rect(0, 0, 4, 4),
                ViewportUnits = System.Windows.Media.BrushMappingMode.Absolute
            };

            var geometryGroup = new System.Windows.Media.GeometryGroup();
            geometryGroup.Children.Add(new System.Windows.Media.RectangleGeometry(new System.Windows.Rect(0, 0, 2, 2)));
            geometryGroup.Children.Add(new System.Windows.Media.RectangleGeometry(new System.Windows.Rect(2, 2, 2, 2)));

            var drawingGroup = new System.Windows.Media.DrawingGroup();
            drawingGroup.Children.Add(new System.Windows.Media.GeometryDrawing(
                System.Windows.Media.Brushes.White, null,
                new System.Windows.Media.RectangleGeometry(new System.Windows.Rect(0, 0, 4, 4))));
            drawingGroup.Children.Add(new System.Windows.Media.GeometryDrawing(
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204)), null,
                geometryGroup));

            brush.Drawing = drawingGroup;
            return brush;
        }

        /// <summary>
        /// 刷新文件名颜色下拉框选项
        /// </summary>
        private void RefreshFileNameColorComboBox(System.Windows.Controls.ComboBox comboBox, string selectedColor)
        {
            comboBox.Items.Clear();

            bool hasCustomColor = !string.IsNullOrEmpty(_settings.Hotbar.CustomFileNameColor) &&
                                  !PresetFileNameColors.Contains(_settings.Hotbar.CustomFileNameColor);

            int selectedIndex = -1;

            // 自定义颜色
            if (hasCustomColor)
            {
                var customItem = CreateColorComboBoxItem(_settings.Hotbar.CustomFileNameColor, true);
                comboBox.Items.Add(customItem);
                if (selectedColor == _settings.Hotbar.CustomFileNameColor)
                    selectedIndex = 0;
            }

            // 预设颜色
            for (int i = 0; i < PresetFileNameColors.Length; i++)
            {
                var presetItem = CreateColorComboBoxItem(PresetFileNameColors[i], false);
                comboBox.Items.Add(presetItem);
                if (selectedColor == PresetFileNameColors[i])
                    selectedIndex = hasCustomColor ? i + 1 : i;
            }

            // 其他...
            var otherItem = new System.Windows.Controls.ComboBoxItem
            {
                Content = GetResourceString("HudOptionFileNameColorOther"),
                Tag = "other"
            };
            comboBox.Items.Add(otherItem);

            if (selectedIndex >= 0)
                comboBox.SelectedIndex = selectedIndex;
        }

        /// <summary>
        /// 计算下拉框最大宽度（根据所有选项内容）
        /// </summary>
        private double CalculateMaxComboBoxWidth()
        {
            double maxWidth = 0;

            // 预设颜色选项
            foreach (var color in PresetFileNameColors)
            {
                double width = EstimateComboBoxItemWidth(color, false);
                if (width > maxWidth)
                    maxWidth = width;
            }

            // "其他..."选项
            double otherWidth = EstimateTextWidth(GetResourceString("HudOptionFileNameColorOther"));
            if (otherWidth > maxWidth)
                maxWidth = otherWidth;

            // 添加ComboBox边距和下拉箭头空间
            maxWidth += 60;

            // 限制最大宽度不超过300
            return Math.Min(maxWidth, 300);
        }

        /// <summary>
        /// 估算下拉框选项宽度
        /// </summary>
        private double EstimateComboBoxItemWidth(string colorHex, bool isCustom)
        {
            // 颜色方块宽度(16) + 间距(8) + 文本宽度
            double textWidth = EstimateTextWidth(colorHex);
            return 16 + 8 + textWidth;
        }

        /// <summary>
        /// 估算文本宽度
        /// </summary>
        private double EstimateTextWidth(string text)
        {
            // 使用平均字符宽度估算（假设12px字体，每个字符约7px宽）
            return text.Length * 7 + 10; // 加10px padding
        }

        /// <summary>
        /// 解析十六进制颜色字符串（支持 #RRGGBB 和 #AARRGGBB 格式）
        /// </summary>
        private System.Windows.Media.Color ParseColorHex(string hex)
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
    }
}