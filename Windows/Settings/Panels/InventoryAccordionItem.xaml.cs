using CraftSharp.Models;
using System;
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

        public InventoryAccordionItem(AppSettings? settings, string titleResourceKey)
        {
            InitializeComponent();

            _settings = settings;
            _titleResourceKey = titleResourceKey;

            // 使用动态资源获取标题
            TitleText.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, titleResourceKey);

            // 读取保存的展开状态（如果启用了记住卡片状态）
            if (_settings != null && _settings.RememberCardStates)
            {
                // InventoryTitle 默认展开，其他默认折叠
                bool defaultExpanded = titleResourceKey == "InventoryTitle";

                if (_settings.CardExpandedStates.TryGetValue(titleResourceKey, out bool savedExpanded))
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
                // 物品栏卡片：添加显示物品栏、锁定位置、记住位置开关
                AddToggleRow("InventoryOptionVisible", "InventoryOptionVisibleDesc", _settings.InventoryWindowVisible, v => _settings.InventoryWindowVisible = v);
                AddToggleRow("InventoryOptionLocked", "InventoryOptionLockedDesc", _settings.InventoryWindowLocked, v => _settings.InventoryWindowLocked = v);
                AddToggleRow("InventoryOptionRememberPosition", "InventoryOptionRememberPositionDesc", _settings.InventoryWindowRememberPosition, v => _settings.InventoryWindowRememberPosition = v);
                // 灰色蒙版开关 + 透明度输入框
                var grayOverlayToggle = AddToggleRow("InventoryOptionGrayOverlay", "InventoryOptionGrayOverlayDesc", _settings.InventoryWindowGrayOverlay, v => _settings.InventoryWindowGrayOverlay = v);
                AddGrayOverlayOpacitySection(grayOverlayToggle);
                // 隐藏状态栏开关
                AddToggleRow("InventoryOptionHideStatusBar", "InventoryOptionHideStatusBarDesc", _settings.InventoryWindowHideStatusBar, v => _settings.InventoryWindowHideStatusBar = v);
            }
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
                Visibility = _settings!.InventoryWindowGrayOverlay ? Visibility.Visible : Visibility.Collapsed
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
                Text = _settings!.InventoryWindowGrayOverlayOpacity.ToString(),
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
                _settings!.InventoryWindowGrayOverlayOpacity = val;
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
            if (_settings != null && _settings.RememberCardStates)
            {
                _settings.CardExpandedStates[_titleResourceKey] = _isExpanded;
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