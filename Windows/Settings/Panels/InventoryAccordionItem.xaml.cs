using CraftSharp.Models;
using CraftSharp.Windows.Dialogs;
using CraftSharp.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Settings.Panels
{
    /// <summary>
    /// 物品栏设置卡片组件
    /// </summary>
    public partial class InventoryAccordionItem : System.Windows.Controls.UserControl
    {
        private AppSettings? _settings;
        private string _titleResourceKey;
        private bool _isExpanded = false;
        private bool _isAnimating = false;

        // 灰色蒙版透明度输入框容器
        private StackPanel? _grayOverlayOpacityContainer;

        /// <summary>
        /// 展开状态变化事件
        /// </summary>
        public event EventHandler<(string Key, bool IsExpanded)>? ExpandedChanged;

        /// <summary>
        /// 卡片标题资源 Key
        /// </summary>
        public string TitleResourceKey => _titleResourceKey;

        /// <summary>
        /// 当前是否展开
        /// </summary>
        public bool IsExpanded => _isExpanded;

        public InventoryAccordionItem(AppSettings? settings, string titleResourceKey)
        {
            InitializeComponent();

            _settings = settings;
            _titleResourceKey = titleResourceKey;

            // 使用动态资源获取标题
            TitleText.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, titleResourceKey);

            // 读取保存的展开状态（如果启用了记住卡片状态）
            if (_settings != null && _settings.System.RememberCardStates)
            {
                // InventoryTitle 默认展开，其他默认折叠
                bool defaultExpanded = titleResourceKey == "InventoryTitle";

                if (_settings.System.CardExpandedStates.TryGetValue(titleResourceKey, out bool savedExpanded))
                {
                    _isExpanded = savedExpanded;
                }
                else
                {
                    _isExpanded = defaultExpanded;
                }
            }
            else
            {
                // 未启用记住卡片状态：使用默认值（InventoryTitle 展开）
                _isExpanded = titleResourceKey == "InventoryTitle";
            }

            // 根据卡片类型添加内容
            AddCardContent(titleResourceKey);

            // 窗口加载后设置初始展开状态
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 根据初始展开状态设置 UI（不执行动画）
            if (_isExpanded)
            {
                ContentBorder.Height = double.NaN;
                ArrowRotate.Angle = 0;
            }
            else
            {
                ContentBorder.Height = 0;
                ArrowRotate.Angle = -90;
            }
        }

        private void AddCardContent(string titleResourceKey)
        {
            if (titleResourceKey == "InventoryTitle" && _settings != null)
            {
                // 物品栏样式（打开样式预览弹窗）
                AddStylePickerRow();
                // 打开动作（单击/双击）
                AddClickModeComboBox();
                // 物品栏卡片：添加显示物品栏、锁定位置、记住位置开关
                AddToggleRow("InventoryOptionVisible", "InventoryOptionVisibleDesc", _settings.Inventory.Visible, v => _settings.Inventory.Visible = v);
                AddToggleRow("InventoryOptionSharedData", "InventoryOptionSharedDataDesc", _settings.Inventory.SharedData, v => {
                    _settings.Inventory.SharedData = v;
                    // 切换共享数据开关后刷新物品栏和快捷栏图标
                    RefreshInventoryAndHotbarIcons();
                });
                AddToggleRow("InventoryOptionLocked", "InventoryOptionLockedDesc", _settings.Inventory.Locked, v => _settings.Inventory.Locked = v);
                AddToggleRow("InventoryOptionRememberPosition", "InventoryOptionRememberPositionDesc", _settings.Inventory.RememberPosition, v => _settings.Inventory.RememberPosition = v);
                AddToggleRow("InventoryOptionHoverEffect", "InventoryOptionHoverEffectDesc", _settings.Inventory.HoverEffect, v => {
                    _settings.Inventory.HoverEffect = v;
                    // 切换悬浮效果开关后刷新物品栏窗口
                    RefreshInventoryHoverEffect();
                });
                // 灰色蒙版开关 + 透明度输入框
                var grayOverlayToggle = AddToggleRow("InventoryOptionGrayOverlay", "InventoryOptionGrayOverlayDesc", _settings.Inventory.GrayOverlay, v => _settings.Inventory.GrayOverlay = v);
                AddGrayOverlayOpacitySection(grayOverlayToggle);
                // 隐藏状态栏开关
                AddToggleRow("InventoryOptionHideStatusBar", "InventoryOptionHideStatusBarDesc", _settings.Inventory.HideStatusBar, v => _settings.Inventory.HideStatusBar = v);
            }
        }

        /// <summary>
        /// 切换共享数据开关后刷新物品栏和快捷栏图标
        /// </summary>
        private void RefreshInventoryAndHotbarIcons()
        {
            if (System.Windows.Application.Current is App app)
            {
                // 刷新物品栏图标
                var inventoryWindow = app.GetInventoryWindow();
                if (inventoryWindow != null)
                {
                    inventoryWindow.RefreshIcons();
                }
                // 刷新快捷栏图标（hotbar 格子也受 SharedData 配置影响）
                StatusBarService.Instance.RefreshHotbarIcons();
            }
        }

        /// <summary>
        /// 切换悬浮效果开关后刷新物品栏窗口
        /// </summary>
        private void RefreshInventoryHoverEffect()
        {
            if (System.Windows.Application.Current is App app)
            {
                var inventoryWindow = app.GetInventoryWindow();
                if (inventoryWindow != null)
                {
                    inventoryWindow.RefreshHoverEffect();
                }
            }
        }

        /// <summary>
        /// 添加物品栏样式选项（打开样式预览弹窗）
        /// </summary>
        private void AddStylePickerRow()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12), Cursor = System.Windows.Input.Cursors.Hand };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock
            {
                FontWeight = FontWeights.Medium
            };
            titleLabel.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, "InventoryStyleOption");
            titleLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextPrimaryBrush");

            var descLabel = new System.Windows.Controls.TextBlock
            {
                Margin = new Thickness(0, 4, 0, 0)
            };
            descLabel.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, "InventoryStyleOptionDesc");
            descLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");

            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            // 当前样式名称显示
            var styleNameText = new System.Windows.Controls.TextBlock
            {
                FontWeight = FontWeights.Medium,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            styleNameText.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextPrimaryBrush");

            // 根据当前样式获取国际化名称
            string currentStyle = _settings!.Inventory.StylePath;
            string styleNameKey = GetStyleNameKey(currentStyle);
            string styleName = System.Windows.Application.Current.TryFindResource(styleNameKey) as string;
            if (string.IsNullOrEmpty(styleName))
            {
                // 如果没有找到国际化字符串，使用文件名
                styleName = Path.GetFileNameWithoutExtension(currentStyle);
            }
            styleNameText.Text = styleName;

            grid.Children.Add(styleNameText);
            Grid.SetColumn(styleNameText, 1);

            // 点击打开样式预览弹窗
            grid.MouseLeftButtonDown += (s, e) =>
            {
                var previewWindow = new StylePreviewWindow(_settings.Inventory.StylePath);
                previewWindow.Owner = Window.GetWindow(this);
                previewWindow.StyleSelected += (sender, selectedStyle) =>
                {
                    // 更新样式配置
                    _settings.Inventory.StylePath = selectedStyle;
                    SaveSettings();

                    // 更新显示名称
                    string newStyleNameKey = GetStyleNameKey(selectedStyle);
                    string newStyleName = System.Windows.Application.Current.TryFindResource(newStyleNameKey) as string;
                    if (string.IsNullOrEmpty(newStyleName))
                    {
                        newStyleName = Path.GetFileNameWithoutExtension(selectedStyle);
                    }
                    styleNameText.Text = newStyleName;

                    // 立即刷新物品栏窗口样式（即时生效）
                    if (System.Windows.Application.Current is App app)
                    {
                        app.RefreshInventoryStyle(selectedStyle);
                    }
                };
                previewWindow.ShowDialog();
                e.Handled = true;
            };

            ContentPanel.Children.Add(grid);
        }

        /// <summary>
        /// 根据样式文件名获取国际化字符串 Key
        /// </summary>
        private string GetStyleNameKey(string fileName)
        {
            // inventory.png → InventoryStyleInventory
            // brewing_stand.png → InventoryStyleBrewingStand
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            // 转换为 PascalCase
            string pascalName = ConvertToPascalCase(baseName);
            return $"InventoryStyle{pascalName}";
        }

        /// <summary>
        /// 将 snake_case 转换为 PascalCase
        /// </summary>
        private string ConvertToPascalCase(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase))
                return "";

            var parts = snakeCase.Split('_');
            var result = new System.Text.StringBuilder();
            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    result.Append(char.ToUpperInvariant(part[0]));
                    if (part.Length > 1)
                        result.Append(part.Substring(1));
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// 添加点击模式选项（打开动作）
        /// </summary>
        private void AddClickModeComboBox()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock
            {
                FontWeight = FontWeights.Medium
            };
            titleLabel.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, "HudOptionClickMode");
            titleLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextPrimaryBrush");

            var descLabel = new System.Windows.Controls.TextBlock
            {
                Margin = new Thickness(0, 4, 0, 0)
            };
            descLabel.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, "HudOptionClickModeDesc");
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
                Content = System.Windows.Application.Current.TryFindResource("HudOptionClickModeSingle") as string ?? "单击",
                Tag = "single"
            };
            var doubleItem = new System.Windows.Controls.ComboBoxItem
            {
                Content = System.Windows.Application.Current.TryFindResource("HudOptionClickModeDouble") as string ?? "双击",
                Tag = "double"
            };
            comboBox.Items.Add(singleItem);
            comboBox.Items.Add(doubleItem);

            if (_settings!.Inventory.ClickMode == "single")
                comboBox.SelectedIndex = 0;
            else
                comboBox.SelectedIndex = 1;

            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                {
                    var mode = item.Tag?.ToString() ?? "single";
                    _settings.Inventory.ClickMode = mode;
                    SaveSettings();

                    // 立即通知 App 更新物品栏窗口的点击模式（即时生效）
                    if (System.Windows.Application.Current is App app)
                    {
                        app.SetInventoryClickMode(mode);
                    }
                }
            };

            grid.Children.Add(comboBox);
            Grid.SetColumn(comboBox, 1);

            ContentPanel.Children.Add(grid);
        }

        private ToggleSwitch AddToggleRow(string labelKey, string descKey, bool currentValue, Action<bool> onToggle)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock
            {
                FontWeight = FontWeights.Medium
            };
            titleLabel.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, labelKey);
            titleLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextPrimaryBrush");

            var descLabel = new System.Windows.Controls.TextBlock
            {
                Margin = new Thickness(0, 4, 0, 0)
            };
            descLabel.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, descKey);
            descLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");

            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            var toggle = new ToggleSwitch { IsChecked = currentValue };
            toggle.Checked += (s, e) =>
            {
                onToggle(true);
                SaveSettings();
            };
            toggle.Unchecked += (s, e) =>
            {
                onToggle(false);
                SaveSettings();
            };

            grid.Children.Add(toggle);
            Grid.SetColumn(toggle, 1);

            ContentPanel.Children.Add(grid);

            return toggle;
        }

        /// <summary>
        /// 添加灰色蒙版透明度输入区域（HUD风格）
        /// </summary>
        private void AddGrayOverlayOpacitySection(ToggleSwitch grayOverlayToggle)
        {
            // 创建容器（用于控制可见性）
            _grayOverlayOpacityContainer = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 12),
                Visibility = _settings!.Inventory.GrayOverlay ? Visibility.Visible : Visibility.Collapsed
            };

            // 创建输入行 Grid
            var inputRow = new Grid();
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Label
            var opacityLabel = new System.Windows.Controls.TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            opacityLabel.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, "InventoryOptionGrayOverlayOpacity");
            opacityLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            inputRow.Children.Add(opacityLabel);

            // 输入容器：TextBox + /100
            var inputContainer = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(8, 0, 0, 0)
            };

            var opacityTextBox = new System.Windows.Controls.TextBox
            {
                Text = _settings!.Inventory.GrayOverlayOpacity.ToString(),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            // 输入验证：只允许数字
            opacityTextBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = !e.Text.All(c => char.IsDigit(c));
            };
            System.Windows.DataObject.AddPastingHandler(opacityTextBox, (s, e) =>
            {
                if (e.DataObject.GetDataPresent(typeof(string)))
                {
                    var text = (string)e.DataObject.GetData(typeof(string));
                    if (!text.All(c => char.IsDigit(c)))
                        e.CancelCommand();
                }
                else
                    e.CancelCommand();
            });

            inputContainer.Children.Add(opacityTextBox);

            var maxDisplay = new System.Windows.Controls.TextBlock
            {
                Text = "/100%",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            maxDisplay.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            inputContainer.Children.Add(maxDisplay);

            inputRow.Children.Add(inputContainer);
            Grid.SetColumn(inputContainer, 1);

            _grayOverlayOpacityContainer.Children.Add(inputRow);
            ContentPanel.Children.Add(_grayOverlayOpacityContainer);

            // LostFocus 时保存值
            opacityTextBox.LostFocus += (s, e) =>
            {
                int val;
                if (!int.TryParse(opacityTextBox.Text, out val) || opacityTextBox.Text.Length == 0)
                    val = 50; // 默认值
                if (val < 0) val = 0;
                if (val > 100) val = 100;
                _settings!.Inventory.GrayOverlayOpacity = val;
                opacityTextBox.Text = val.ToString();
                SaveSettings();
                // 下次打开物品栏时会自动应用新透明度
            };

            // 监听灰色蒙版开关控制透明度输入框可见性
            grayOverlayToggle.Checked += (s, e) =>
            {
                if (_grayOverlayOpacityContainer != null)
                {
                    _grayOverlayOpacityContainer.Visibility = Visibility.Visible;
                    RefreshContentHeight();
                }
            };
            grayOverlayToggle.Unchecked += (s, e) =>
            {
                if (_grayOverlayOpacityContainer != null)
                {
                    _grayOverlayOpacityContainer.Visibility = Visibility.Collapsed;
                    RefreshContentHeight();
                }
            };
        }

        private void SaveSettings()
        {
            if (System.Windows.Application.Current is App app)
            {
                app.SaveSettings();
            }
        }

        /// <summary>
        /// 刷新内容区域高度（用于动画）
        /// </summary>
        private void RefreshContentHeight()
        {
            if (!_isExpanded || _isAnimating) return;

            ContentPanel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double newHeight = ContentPanel.DesiredSize.Height + 32;
            double currentHeight = ContentBorder.ActualHeight;

            if (Math.Abs(newHeight - currentHeight) > 5)
            {
                _isAnimating = true;
                var animation = new DoubleAnimation
                {
                    From = currentHeight,
                    To = newHeight,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                animation.Completed += (s, e) =>
                {
                    ContentBorder.Height = double.NaN;
                    _isAnimating = false;
                };
                ContentBorder.BeginAnimation(FrameworkElement.HeightProperty, animation);
            }
        }

        private void Header_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating) return;

            _isExpanded = !_isExpanded;

            if (_isExpanded)
                AnimateExpand();
            else
                AnimateCollapse();

            var arrowAnimation = new DoubleAnimation
            {
                To = _isExpanded ? 0 : -90,
                Duration = TimeSpan.FromMilliseconds(_isExpanded ? 200 : 150),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            ArrowRotate.BeginAnimation(RotateTransform.AngleProperty, arrowAnimation);

            // 如果启用了记住卡片状态，保存到配置
            if (_settings != null && _settings.System.RememberCardStates)
            {
                _settings.System.CardExpandedStates[_titleResourceKey] = _isExpanded;
                SaveSettings();
            }

            // 触发展开状态变化事件
            ExpandedChanged?.Invoke(this, (_titleResourceKey, _isExpanded));
        }

        private void AnimateExpand()
        {
            _isAnimating = true;

            ContentPanel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double targetHeight = ContentPanel.DesiredSize.Height + 32;

            var animation = new DoubleAnimation
            {
                From = 0,
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            animation.Completed += (s, e) =>
            {
                ContentBorder.Height = double.NaN;
                _isAnimating = false;
            };

            ContentBorder.Height = 0;
            ContentBorder.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        private void AnimateCollapse()
        {
            _isAnimating = true;

            double currentHeight = ContentBorder.ActualHeight;

            var animation = new DoubleAnimation
            {
                From = currentHeight,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            animation.Completed += (s, e) =>
            {
                ContentBorder.Height = 0;
                _isAnimating = false;
            };

            ContentBorder.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        /// <summary>
        /// 设置展开状态（用于外部控制，不触发保存）
        /// </summary>
        public void SetExpanded(bool expanded, bool animate = true)
        {
            if (_isAnimating) return;

            _isExpanded = expanded;

            if (animate)
            {
                if (_isExpanded)
                    AnimateExpand();
                else
                    AnimateCollapse();

                var arrowAnimation = new DoubleAnimation
                {
                    To = _isExpanded ? 0 : -90,
                    Duration = TimeSpan.FromMilliseconds(_isExpanded ? 200 : 150),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                ArrowRotate.BeginAnimation(RotateTransform.AngleProperty, arrowAnimation);
            }
            else
            {
                // 不执行动画，直接设置状态
                if (_isExpanded)
                {
                    ContentBorder.Height = double.NaN;
                    ArrowRotate.Angle = 0;
                }
                else
                {
                    ContentBorder.Height = 0;
                    ArrowRotate.Angle = -90;
                }
            }
        }
    }
}