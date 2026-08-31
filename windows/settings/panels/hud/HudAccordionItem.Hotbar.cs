using CraftSharp.Models;
using CraftSharp.Services.Core;
using CraftSharp.Services.Hud;
using CraftSharp.Services.Slot;
using CraftSharp.Services.Resource;
using CraftSharp.Helpers;
using CraftSharp.Windows.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace CraftSharp.Windows.Settings.Panels.Hud
{
    /// <summary>
    /// HudAccordionItem 快捷栏配置
    /// </summary>
    public partial class HudAccordionItem
    {
        private void AddHotbarContent()
        {
            AddFileNameColorComboBox();
            AddClickModeComboBox();

            var hotbarToggle = AddToggleRow("HudOptionShowHotbar", "HudOptionShowHotbarDesc", _settings.Hotbar.Visible);
            hotbarToggle.Checked += (_, _) => { _settings.Hotbar.Visible = true; StatusBarService.Instance.SetHotbarVisible(true); SaveSettings(); };
            hotbarToggle.Unchecked += (_, _) => { _settings.Hotbar.Visible = false; StatusBarService.Instance.SetHotbarVisible(false); SaveSettings(); };

            var hoverToggle = AddToggleRow("HudOptionHoverEffect", "HudOptionHoverEffectDesc", _settings.Hotbar.HoverEffect);
            hoverToggle.Checked += (_, _) => { _settings.Hotbar.HoverEffect = true; StatusBarService.Instance.SetHotbarHoverEffect(true); SaveSettings(); };
            hoverToggle.Unchecked += (_, _) => { _settings.Hotbar.HoverEffect = false; StatusBarService.Instance.SetHotbarHoverEffect(false); SaveSettings(); };

            var leftOffhandToggle = AddToggleRow("HudOptionLeftOffhand", "HudOptionLeftOffhandDesc", _settings.Hotbar.LeftOffhand);
            leftOffhandToggle.Checked += (_, _) => { _settings.Hotbar.LeftOffhand = true; StatusBarService.Instance.SetOffhandConfig(true, _settings.Hotbar.RightOffhand); SaveSettings(); };
            leftOffhandToggle.Unchecked += (_, _) => { _settings.Hotbar.LeftOffhand = false; StatusBarService.Instance.SetOffhandConfig(false, _settings.Hotbar.RightOffhand); SaveSettings(); };

            var rightOffhandToggle = AddToggleRow("HudOptionRightOffhand", "HudOptionRightOffhandDesc", _settings.Hotbar.RightOffhand);
            rightOffhandToggle.Checked += (_, _) => { _settings.Hotbar.RightOffhand = true; StatusBarService.Instance.SetOffhandConfig(_settings.Hotbar.LeftOffhand, true); SaveSettings(); };
            rightOffhandToggle.Unchecked += (_, _) => { _settings.Hotbar.RightOffhand = false; StatusBarService.Instance.SetOffhandConfig(_settings.Hotbar.LeftOffhand, false); SaveSettings(); };

            var showTargetIconToggle = AddToggleRow("HudOptionShowTargetIcon", "HudOptionShowTargetIconDesc", _settings.Hotbar.ShowTargetIcon);
            showTargetIconToggle.Checked += (_, _) => {
                _settings.Hotbar.ShowTargetIcon = true;
                StatusBarService.Instance.RefreshHotbarIcons();
                if (System.Windows.Application.Current is App app) app.GetInventoryWindow()?.RefreshIcons();
                SaveSettings();
            };
            showTargetIconToggle.Unchecked += (_, _) => {
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
            double maxWidth = ColorPickerHelper.CalculateMaxComboBoxWidth(
                GetResourceString("HudOptionFileNameColorAuto"),
                GetResourceString("HudOptionFileNameColorOther"));

            var comboBox = new System.Windows.Controls.ComboBox
            {
                Width = maxWidth,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left
            };

            string currentColor = _settings.Hotbar.FileNameColor;
            string? customColor = _settings.Hotbar.CustomFileNameColor;

            // 是否有自定义颜色
            bool hasCustomColor = !string.IsNullOrEmpty(customColor) && !ColorPickerHelper.PresetColors.Contains(customColor);

            // 构建下拉框选项
            int selectedIndex = -1;

            // 1. 如果有自定义颜色，添加自定义颜色选项在最前面
            if (hasCustomColor)
            {
                var customItem = ColorPickerHelper.CreateColorComboBoxItem(customColor!);
                comboBox.Items.Add(customItem);
                if (currentColor == customColor)
                    selectedIndex = 0;
            }

            // 2. 添加预设颜色
            int autoIndexOffset = hasCustomColor ? 1 : 0;
            for (int i = 0; i < ColorPickerHelper.PresetColors.Length; i++)
            {
                var presetItem = ColorPickerHelper.CreateColorComboBoxItem(ColorPickerHelper.PresetColors[i]);
                comboBox.Items.Add(presetItem);
                if (currentColor == ColorPickerHelper.PresetColors[i])
                    selectedIndex = autoIndexOffset + i;
            }

            // 3. 添加"自动"选项（在"其他..."之前）
            var autoItem = new System.Windows.Controls.ComboBoxItem
            {
                Content = GetResourceString("HudOptionFileNameColorAuto"),
                Tag = "auto"
            };
            comboBox.Items.Add(autoItem);
            if (currentColor == "auto")
                selectedIndex = autoIndexOffset + ColorPickerHelper.PresetColors.Length;

            // 4. 添加"其他..."选项
            var otherItem = new System.Windows.Controls.ComboBoxItem
            {
                Content = GetResourceString("HudOptionFileNameColorOther"),
                Tag = "other"
            };
            comboBox.Items.Add(otherItem);

            // 设置选中项
            if (selectedIndex >= 0)
                comboBox.SelectedIndex = selectedIndex;

            comboBox.SelectionChanged += (_, _) =>
            {
                if (comboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                {
                    string tag = item.Tag?.ToString() ?? "";

                    if (tag == "other")
                    {
                        // 打开颜色选择弹窗
                        var colorPicker = new ColorPickerWindow(currentColor);
                        colorPicker.Owner = System.Windows.Window.GetWindow(this);

                        if (colorPicker.ShowDialogQuiet() == true)
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
                        if (!ColorPickerHelper.PresetColors.Contains(tag))
                        {
                            // 如果是自定义颜色，保留配置
                            _settings.Hotbar.CustomFileNameColor = tag;
                        }
                        SaveSettings();

                        // 更新 StatusBarWindow
                        StatusBarService.Instance.RefreshFileNameColor();

                        currentColor = tag;
                    }
                    else if (tag == "auto")
                    {
                        // 自动模式
                        _settings.Hotbar.FileNameColor = "auto";
                        SaveSettings();

                        // 更新 StatusBarWindow
                        StatusBarService.Instance.RefreshFileNameColor();

                        currentColor = "auto";
                    }
                }
            };

            grid.Children.Add(comboBox);
            Grid.SetColumn(comboBox, 1);

            ContentPanel.Children.Add(grid);
        }

        /// <summary>
        /// 刷新文件名颜色下拉框选项
        /// </summary>
        private void RefreshFileNameColorComboBox(System.Windows.Controls.ComboBox comboBox, string selectedColor)
        {
            comboBox.Items.Clear();

            bool hasCustomColor = !string.IsNullOrEmpty(_settings.Hotbar.CustomFileNameColor) &&
                                  !ColorPickerHelper.PresetColors.Contains(_settings.Hotbar.CustomFileNameColor);

            int selectedIndex = -1;
            int autoIndexOffset = hasCustomColor ? 1 : 0;

            // 自定义颜色
            if (hasCustomColor)
            {
                var customItem = ColorPickerHelper.CreateColorComboBoxItem(_settings.Hotbar.CustomFileNameColor!);
                comboBox.Items.Add(customItem);
                if (selectedColor == _settings.Hotbar.CustomFileNameColor)
                    selectedIndex = 0;
            }

            // 预设颜色
            for (int i = 0; i < ColorPickerHelper.PresetColors.Length; i++)
            {
                var presetItem = ColorPickerHelper.CreateColorComboBoxItem(ColorPickerHelper.PresetColors[i]);
                comboBox.Items.Add(presetItem);
                if (selectedColor == ColorPickerHelper.PresetColors[i])
                    selectedIndex = autoIndexOffset + i;
            }

            // 自动
            var autoItem = new System.Windows.Controls.ComboBoxItem
            {
                Content = GetResourceString("HudOptionFileNameColorAuto"),
                Tag = "auto"
            };
            comboBox.Items.Add(autoItem);
            if (selectedColor == "auto")
                selectedIndex = autoIndexOffset + ColorPickerHelper.PresetColors.Length;

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
    }
}