using CraftSharp.Models;
using CraftSharp.Services;
using CraftSharp.Helpers;
using CraftSharp.Windows.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Settings.Panels
{
    /// <summary>
    /// HudAccordionItem 通用UI构建方法
    /// </summary>
    public partial class HudAccordionItem
    {
        private ToggleSwitch AddToggleRow(string labelKey, string descKey, bool defaultVal)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString(labelKey),
                FontWeight = FontWeights.Medium
            };
            titleLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextPrimaryBrush");
            var descLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString(descKey),
                Margin = new Thickness(0, 4, 0, 0)
            };
            descLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            var toggle = new ToggleSwitch { IsChecked = defaultVal };
            grid.Children.Add(toggle);
            Grid.SetColumn(toggle, 1);

            ContentPanel.Children.Add(grid);

            return toggle;
        }

        private void AddClickModeComboBox()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("HudOptionClickMode"),
                FontWeight = FontWeights.Medium
            };
            titleLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextPrimaryBrush");
            var descLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("HudOptionClickModeDesc"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            descLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            var comboBox = new System.Windows.Controls.ComboBox
            {
                Width = 100,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            var singleItem = new System.Windows.Controls.ComboBoxItem
            {
                Content = GetResourceString("HudOptionClickModeSingle"),
                Tag = "single"
            };
            var doubleItem = new System.Windows.Controls.ComboBoxItem
            {
                Content = GetResourceString("HudOptionClickModeDouble"),
                Tag = "double"
            };
            comboBox.Items.Add(singleItem);
            comboBox.Items.Add(doubleItem);

            if (_settings.Hotbar.ClickMode == "single")
                comboBox.SelectedIndex = 0;
            else
                comboBox.SelectedIndex = 1;

            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                {
                    var mode = item.Tag?.ToString() ?? "double";
                    _settings.Hotbar.ClickMode = mode;
                    StatusBarService.Instance.SetHotbarClickMode(mode);
                    SaveSettings();
                }
            };

            grid.Children.Add(comboBox);
            Grid.SetColumn(comboBox, 1);

            ContentPanel.Children.Add(grid);
        }

        private System.Windows.Controls.Border? _iconPreviewBorder;

        private void AddIconPreviewRow(string labelKey, string iconPath)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString(labelKey),
                FontWeight = FontWeights.Medium
            };
            titleLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextPrimaryBrush");
            var descLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString(labelKey + "Desc"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            descLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            _iconPreviewBorder = new System.Windows.Controls.Border
            {
                Height = 32,
                MaxWidth = 256,
                CornerRadius = new CornerRadius(6),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = GetResourceString("AppIconTooltip")
            };

            string? backgroundPath = GetBackgroundIconPath(_hudId);
            if (backgroundPath != null)
            {
                var iconGrid = new Grid();

                var backgroundImage = new System.Windows.Controls.Image
                {
                    Source = LoadBitmapImage(backgroundPath),
                    Stretch = Stretch.Uniform
                };
                RenderOptions.SetBitmapScalingMode(backgroundImage, BitmapScalingMode.NearestNeighbor);
                iconGrid.Children.Add(backgroundImage);

                var iconImage = new System.Windows.Controls.Image
                {
                    Name = "IconPreviewImage",
                    Source = LoadBitmapImage(iconPath),
                    Stretch = Stretch.Uniform
                };
                RenderOptions.SetBitmapScalingMode(iconImage, BitmapScalingMode.NearestNeighbor);
                iconGrid.Children.Add(iconImage);

                _iconPreviewBorder.Child = iconGrid;
            }
            else
            {
                var iconImage = new System.Windows.Controls.Image
                {
                    Name = "IconPreviewImage",
                    Source = LoadBitmapImage(iconPath),
                    Stretch = Stretch.Uniform
                };
                RenderOptions.SetBitmapScalingMode(iconImage, BitmapScalingMode.NearestNeighbor);
                _iconPreviewBorder.Child = iconImage;
            }

            _iconPreviewBorder.MouseLeftButtonDown += IconPreview_Click;
            grid.Children.Add(_iconPreviewBorder);
            Grid.SetColumn(_iconPreviewBorder, 1);

            ContentPanel.Children.Add(grid);
        }

        private static string? GetBackgroundIconPath(string hudId)
        {
            return hudId switch
            {
                "health" => AssetPaths.HeartContainer,
                "food" => AssetPaths.FoodEmpty,
                "absorbing" => AssetPaths.HeartContainer,
                _ => null
            };
        }

        private void IconPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string elementType = GetElementTypeFromHudId(_hudId);

            var picker = new HudIconPickerWindow(elementType);
            picker.Owner = System.Windows.Window.GetWindow(this);

            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == _hudId);
            string currentStyle = settings?.IconStyle ?? "";

            if (picker.ShowDialog() == true && picker.SelectedIconStyle != null)
            {
                EnsureHudElementExists(_hudId);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == _hudId);
                if (elem != null)
                {
                    elem.IconStyle = picker.SelectedIconStyle;
                    SaveSettings();
                }

                if (_iconPreviewBorder != null)
                {
                    System.Windows.Controls.Image? iconImage = null;
                    if (_iconPreviewBorder.Child is Grid iconGrid)
                    {
                        iconImage = iconGrid.Children.OfType<System.Windows.Controls.Image>().LastOrDefault();
                    }
                    else if (_iconPreviewBorder.Child is System.Windows.Controls.Image directImage)
                    {
                        iconImage = directImage;
                    }

                    if (iconImage != null)
                    {
                        string newIconPath = GetIconPathFromStyle(_hudId, picker.SelectedIconStyle);
                        iconImage.Source = LoadBitmapImage(newIconPath);
                    }
                }

                StatusBarService.Instance.RefreshHudElement(_hudId);
            }
        }

        private static string GetElementTypeFromHudId(string hudId)
        {
            return hudId switch
            {
                "health" => "heart",
                "food" => "food",
                "absorbing" => "absorbing",
                "armor" => "armor",
                "air" => "air",
                "expbar" => "expbar",
                "crosshair" => "crosshair",
                "attackindicator" => "attackindicator",
                _ => hudId
            };
        }

        private static string GetIconPathFromStyle(string hudId, string iconStyle)
        {
            return hudId switch
            {
                "health" => AssetPaths.GetHeartPathWithFallback(iconStyle, "full"),
                "food" => AssetPaths.GetFoodPath(iconStyle, "full"),
                "absorbing" => GetAbsorbingIconPath(iconStyle),
                "armor" => AssetPaths.ArmorFull,
                "air" => AssetPaths.Air,
                "expbar" => GetExpBarIconPath(iconStyle),
                "crosshair" => AssetPaths.Crosshair,
                "attackindicator" => AssetPaths.CrosshairAttackIndicatorFull,
                _ => ""
            };
        }

        private static string GetExpBarIconPath(string iconStyle)
        {
            if (string.IsNullOrEmpty(iconStyle) || iconStyle == "experience_bar_progress")
            {
                return AssetPaths.ExperienceBarProgress;
            }
            return AssetPaths.JumpBarProgress;
        }

        private static string GetAbsorbingIconPath(string iconStyle)
        {
            if (string.IsNullOrEmpty(iconStyle) || iconStyle == "absorbing_full")
            {
                return AssetPaths.AbsorbingFull;
            }
            return "Assets/minecraft/textures/gui/sprites/hud/heart/absorbing_hardcore_full.png";
        }

        private static BitmapImage LoadBitmapImage(string relativePath)
        {
            var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}