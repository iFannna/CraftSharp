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
            "#9A5CC6", // 紫色
            "#B4684D", // 棕色
            "#6EECD2", // 青色
            "#11A036", // 绿色
            "#DEB12D", // 黄色
            "#ECECEC", // 白色
            "#416E97", // 蓝色
            "#E3D4C4", // 米色
            "#625859", // 灰色
            "#971607", // 深红
            "#FC7812"  // 橙色
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

            var comboBox = new System.Windows.Controls.ComboBox
            {
                Width = 120,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
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

            // 颜色方块
            var colorBox = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush(ParseColorHex(colorHex))
            };

            // 十六进制值文本
            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = isCustom ? $"{GetResourceString("HudOptionFileNameColorCustom")}: {colorHex}" : colorHex
            };

            stackPanel.Children.Add(colorBox);
            stackPanel.Children.Add(textBlock);
            item.Content = stackPanel;

            return item;
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
        /// 解析十六进制颜色字符串
        /// </summary>
        private System.Windows.Media.Color ParseColorHex(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length != 6)
                return System.Windows.Media.Colors.White;

            try
            {
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return System.Windows.Media.Color.FromRgb(r, g, b);
            }
            catch
            {
                return System.Windows.Media.Colors.White;
            }
        }
    }
}