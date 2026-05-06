using CraftSharp.Models;
using CraftSharp.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Panels
{
    public partial class HudAccordionItem : System.Windows.Controls.UserControl
    {
        private AppSettings _settings;
        private string _hudId;
        private bool _isExpanded = false;
        private bool _isAnimating = false;

        // UI element references for mutual exclusion and visibility control
        private ToggleSwitch? _mappingToggle;
        private System.Windows.Controls.ComboBox? _mappingComboBox;
        private ToggleSwitch? _customToggle;
        private StackPanel? _valueContainer;
        private System.Windows.Controls.TextBox? _currentValueTextBox;
        private System.Windows.Controls.TextBox? _maxValueTextBox;

        public HudAccordionItem(AppSettings settings, string id, string name)
        {
            InitializeComponent();
            _settings = settings;
            _hudId = id;

            TitleText.Text = name;
            AddHudContent(id);

            // 监听语言变化，重新构建内容
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            ContentPanel.Children.Clear();
            AddHudContent(_hudId);
        }

        private void Header_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating) return;

            _isExpanded = !_isExpanded;

            if (_isExpanded)
                AnimateExpand();
            else
                AnimateCollapse();

            // 箭头旋转动画
            var arrowAnimation = new DoubleAnimation
            {
                To = _isExpanded ? 0 : -90,
                Duration = TimeSpan.FromMilliseconds(_isExpanded ? 200 : 150),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            ArrowRotate.BeginAnimation(RotateTransform.AngleProperty, arrowAnimation);
        }

        private void AnimateExpand()
        {
            _isAnimating = true;

            // 计算内容实际高度
            ContentPanel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double targetHeight = ContentPanel.DesiredSize.Height + 32; // 加上 Padding

            var animation = new DoubleAnimation
            {
                From = 0,
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            animation.Completed += (s, e) =>
            {
                ContentBorder.Height = double.NaN; // 恢复为 Auto
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

        private void EnsureHudElementExists(string id)
        {
            if (_settings.HudElements.Any(h => h.Id == id)) return;

            var newElement = new HudElementSettings { Id = id };
            _settings.HudElements.Add(newElement);
        }

        private string GetResourceString(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string ?? key;
        }

        private void SaveSettings()
        {
            if (System.Windows.Application.Current is App app)
            {
                app.SaveSettings();
            }
        }

        private void RefreshContentHeight()
        {
            if (!_isExpanded || _isAnimating) return;

            // 计算新的目标高度
            ContentPanel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double newHeight = ContentPanel.DesiredSize.Height + 32;
            double currentHeight = ContentBorder.ActualHeight;

            // 如果高度变化明显，使用动画平滑过渡
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
            else
            {
                ContentBorder.BeginAnimation(FrameworkElement.HeightProperty, null);
                ContentBorder.Height = double.NaN;
            }
        }

        private void AddHudContent(string id)
        {
            // Reset UI references
            _mappingToggle = null;
            _mappingComboBox = null;
            _customToggle = null;
            _valueContainer = null;
            _currentValueTextBox = null;
            _maxValueTextBox = null;

            if (id == "statusbar")
            {
                var showToggle = AddToggleRow("HudOptionShowStatusBar", "HudOptionShowStatusBarDesc", _settings.StatusBarVisible);
                showToggle.Checked += (s, e) => { _settings.StatusBarVisible = true; StatusBarService.Instance.SetVisible(true); SaveSettings(); };
                showToggle.Unchecked += (s, e) => { _settings.StatusBarVisible = false; StatusBarService.Instance.SetVisible(false); SaveSettings(); };

                var lockToggle = AddToggleRow("HudOptionLockPosition", "HudOptionLockPositionDesc", _settings.StatusBarLocked);
                lockToggle.Checked += (s, e) => { _settings.StatusBarLocked = true; StatusBarService.Instance.SetLocked(true); SaveSettings(); };
                lockToggle.Unchecked += (s, e) => { _settings.StatusBarLocked = false; StatusBarService.Instance.SetLocked(false); SaveSettings(); };

                var rememberToggle = AddToggleRow("HudOptionRememberPosition", "HudOptionRememberPositionDesc", _settings.StatusBarRememberPosition);
                rememberToggle.Checked += (s, e) => { _settings.StatusBarRememberPosition = true; SaveSettings(); };
                rememberToggle.Unchecked += (s, e) => { _settings.StatusBarRememberPosition = false; SaveSettings(); };
            }
            else if (id == "hotbar")
            {
                var hotbarToggle = AddToggleRow("HudOptionShowHotbar", "HudOptionShowHotbarDesc", _settings.HotbarVisible);
                hotbarToggle.Checked += (s, e) => { _settings.HotbarVisible = true; StatusBarService.Instance.SetHotbarVisible(true); SaveSettings(); };
                hotbarToggle.Unchecked += (s, e) => { _settings.HotbarVisible = false; StatusBarService.Instance.SetHotbarVisible(false); SaveSettings(); };

                var leftOffhandToggle = AddToggleRow("HudOptionLeftOffhand", "HudOptionLeftOffhandDesc", _settings.HotbarLeftOffhand);
                leftOffhandToggle.Checked += (s, e) => { _settings.HotbarLeftOffhand = true; StatusBarService.Instance.SetOffhandConfig(true, _settings.HotbarRightOffhand); SaveSettings(); };
                leftOffhandToggle.Unchecked += (s, e) => { _settings.HotbarLeftOffhand = false; StatusBarService.Instance.SetOffhandConfig(false, _settings.HotbarRightOffhand); SaveSettings(); };

                var rightOffhandToggle = AddToggleRow("HudOptionRightOffhand", "HudOptionRightOffhandDesc", _settings.HotbarRightOffhand);
                rightOffhandToggle.Checked += (s, e) => { _settings.HotbarRightOffhand = true; StatusBarService.Instance.SetOffhandConfig(_settings.HotbarLeftOffhand, true); SaveSettings(); };
                rightOffhandToggle.Unchecked += (s, e) => { _settings.HotbarRightOffhand = false; StatusBarService.Instance.SetOffhandConfig(_settings.HotbarLeftOffhand, false); SaveSettings(); };
            }
            else if (id == "health")
            {
                // health 特殊：有恢复动画选项
                AddStandardHudElement(id, StatusBarService.Instance.SetHealthVisible, hasRegenAnimation: true);
            }
            else if (id == "expbar")
            {
                // 经验条特殊：只有当前值，没有最大值
                AddStandardHudElement(id, StatusBarService.Instance.SetExpBarVisible, hasMaxValue: false);
            }
            else if (id == "absorbing")
            {
                // 伤害吸收值特殊：maxValue上限1024，多行显示
                var setVisibleAction = GetSetVisibleAction(id);
                AddStandardHudElement(id, setVisibleAction, maxValueLimit: 1024);
            }
            else if (id == "air")
            {
                // 空气值特殊：有动画效果选项（仅UI开关，暂不实现动画）
                var setVisibleAction = GetSetVisibleAction(id);
                AddStandardHudElement(id, setVisibleAction, hasRegenAnimation: true);
            }
            else if (id == "food" || id == "armor")
            {
                // 标准HUD元素：显示开关 + 数据映射 + 自定义数值（maxValue上限20）
                var setVisibleAction = GetSetVisibleAction(id);
                AddStandardHudElement(id, setVisibleAction);
            }
        }

        /// <summary>
        /// 获取HUD元素的可见性设置方法
        /// </summary>
        private Action<bool>? GetSetVisibleAction(string id)
        {
            return id switch
            {
                "expbar" => StatusBarService.Instance.SetExpBarVisible,
                "food" => StatusBarService.Instance.SetFoodVisible,
                "air" => StatusBarService.Instance.SetAirVisible,
                "armor" => StatusBarService.Instance.SetArmorVisible,
                "absorbing" => StatusBarService.Instance.SetAbsorbingVisible,
                _ => null
            };
        }

        /// <summary>
        /// 添加标准HUD元素配置（显示开关 + 数据映射 + 自定义数值）
        /// </summary>
        private void AddStandardHudElement(string id, Action<bool>? setVisibleAction, bool hasRegenAnimation = false, bool hasMaxValue = true, int maxValueLimit = 20)
        {
            EnsureHudElementExists(id);
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == id);
            bool isVisible = settings?.IsVisible ?? true;
            bool regenAnim = settings?.RegenAnimation ?? false;
            bool dataMappingEnabled = settings?.DataMappingEnabled ?? false;
            string dataMappingType = settings?.DataMappingType ?? "电池电量";
            bool customValueEnabled = settings?.CustomValueEnabled ?? false;
            int customCurrentValue = settings?.CustomCurrentValue ?? 10;
            int customMaxValue = settings?.CustomMaxValue ?? 20;

            // 显示开关
            var showToggle = AddToggleRow("HudOptionShowElement", "HudOptionShowElementDesc", isVisible);
            showToggle.Checked += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null) elem.IsVisible = true;
                setVisibleAction?.Invoke(true);
                SaveSettings();
            };
            showToggle.Unchecked += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null) elem.IsVisible = false;
                setVisibleAction?.Invoke(false);
                SaveSettings();
            };

            // 恢复动画开关（仅生命值）
            if (hasRegenAnimation)
            {
                var regenToggle = AddToggleRow("HudOptionRegenAnim", "HudOptionRegenAnimDesc", regenAnim);
                regenToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists(id);
                    var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                    if (elem != null) elem.RegenAnimation = true;
                    SaveSettings();
                };
                regenToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists(id);
                    var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                    if (elem != null) elem.RegenAnimation = false;
                    SaveSettings();
                };
            }

            // 数据映射开关 + 下拉框
            AddDataMappingSection(id, dataMappingEnabled, dataMappingType);

            // 自定义数值开关 + 数值输入
            AddCustomValueSection(id, customValueEnabled, customCurrentValue, customMaxValue, hasMaxValue, maxValueLimit);
        }

        /// <summary>
        /// 添加数据映射配置区域（开关 + 下拉框）
        /// </summary>
        private void AddDataMappingSection(string id, bool enabled, string mappingType)
        {
            // 开关行
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("HudOptionDataMapping"),
                FontWeight = FontWeights.Medium,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextPrimaryBrush")
            };
            var descLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("HudOptionDataMappingDesc"),
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            _mappingToggle = new ToggleSwitch { IsChecked = enabled };
            grid.Children.Add(_mappingToggle);
            Grid.SetColumn(_mappingToggle, 1);

            ContentPanel.Children.Add(grid);

            // 下拉框（数据映射类型选择）
            _mappingComboBox = new System.Windows.Controls.ComboBox
            {
                Width = 160,
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Visibility = enabled ? Visibility.Visible : Visibility.Collapsed
            };

            var types = new[] { "电池电量", "内存占用率", "CPU利用率", "GPU利用率" };
            var typeKeys = new[] { "DataMappingBattery", "DataMappingMemory", "DataMappingCpu", "DataMappingGpu" };

            for (int i = 0; i < types.Length; i++)
            {
                var item = new System.Windows.Controls.ComboBoxItem { Content = GetResourceString(typeKeys[i]), Tag = types[i] };
                _mappingComboBox.Items.Add(item);
                if (mappingType == types[i])
                    _mappingComboBox.SelectedIndex = i;
            }
            if (_mappingComboBox.SelectedIndex < 0)
                _mappingComboBox.SelectedIndex = 0;

            _mappingComboBox.SelectionChanged += (s, e) =>
            {
                if (_mappingComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                {
                    EnsureHudElementExists(id);
                    var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                    if (elem != null)
                    {
                        elem.DataMappingType = item.Tag?.ToString() ?? "电池电量";
                        SaveSettings();
                    }
                }
            };

            ContentPanel.Children.Add(_mappingComboBox);

            // 开关事件：互斥逻辑
            _mappingToggle.Checked += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null)
                {
                    elem.DataMappingEnabled = true;
                    elem.CustomValueEnabled = false; // 自动关闭自定义数值
                }
                _mappingComboBox.Visibility = Visibility.Visible;

                // 关闭自定义数值开关和输入框
                if (_customToggle != null)
                {
                    _customToggle.IsChecked = false;
                }
                if (_valueContainer != null)
                {
                    _valueContainer.Visibility = Visibility.Collapsed;
                }

                SaveSettings();
                StatusBarService.Instance.RefreshHudElement(id);
                RefreshContentHeight();
            };
            _mappingToggle.Unchecked += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null) elem.DataMappingEnabled = false;
                _mappingComboBox.Visibility = Visibility.Collapsed;
                SaveSettings();
                StatusBarService.Instance.RefreshHudElement(id);
                RefreshContentHeight();
            };
        }

        /// <summary>
        /// 添加自定义数值配置区域（开关 + 当前值 + 最大值）
        /// </summary>
        private void AddCustomValueSection(string id, bool enabled, int currentValue, int maxValue, bool hasMaxValue = true, int maxValueLimit = 20)
        {
            // 开关行
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("HudOptionCustomValue"),
                FontWeight = FontWeights.Medium,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextPrimaryBrush")
            };
            var descLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("HudOptionCustomValueDesc"),
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            _customToggle = new ToggleSwitch { IsChecked = enabled };
            grid.Children.Add(_customToggle);
            Grid.SetColumn(_customToggle, 1);

            ContentPanel.Children.Add(grid);

            // 数值输入容器（垂直布局）
            _valueContainer = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical,
                Visibility = enabled ? Visibility.Visible : Visibility.Collapsed
            };

            // 当前值行
            var currentRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            currentRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            currentRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var currentLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("CustomCurrentValue") + ":",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush")
            };
            currentRow.Children.Add(currentLabel);

            _currentValueTextBox = new System.Windows.Controls.TextBox
            {
                Text = currentValue.ToString(),
                Width = 80,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            // 限制只能输入数字
            _currentValueTextBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = !e.Text.All(c => char.IsDigit(c));
            };
            // 限制粘贴内容
            System.Windows.DataObject.AddPastingHandler(_currentValueTextBox, (s, e) =>
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
            currentRow.Children.Add(_currentValueTextBox);
            Grid.SetColumn(_currentValueTextBox, 1);
            // 失焦时验证并保存数值（输入过程中不保存）
            _currentValueTextBox.LostFocus += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null)
                {
                    int maxVal = hasMaxValue ? elem.CustomMaxValue : 100; // expbar固定上限100
                    int val;
                    if (!int.TryParse(_currentValueTextBox.Text, out val) || _currentValueTextBox.Text.Length == 0)
                        val = maxVal; // 空或无效时设为最大值
                    if (val < 0) val = 0;
                    if (val > maxVal) val = maxVal; // 所有类型都限制上限
                    elem.CustomCurrentValue = val;
                    _currentValueTextBox.Text = val.ToString();
                    SaveSettings();
                    StatusBarService.Instance.RefreshHudElement(id);
                }
            };
            _valueContainer.Children.Add(currentRow);

            // 最大值行（仅当hasMaxValue为true时显示）
            if (hasMaxValue)
            {
                var maxRow = new Grid();
                maxRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                maxRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var maxLabel = new System.Windows.Controls.TextBlock
                {
                    Text = GetResourceString("CustomMaxValue") + ":",
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush")
                };
                maxRow.Children.Add(maxLabel);

                _maxValueTextBox = new System.Windows.Controls.TextBox
                {
                    Text = maxValue.ToString(),
                    Width = 80,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                // 限制只能输入数字
                _maxValueTextBox.PreviewTextInput += (s, e) =>
                {
                    e.Handled = !e.Text.All(c => char.IsDigit(c));
                };
                // 限制粘贴内容
                System.Windows.DataObject.AddPastingHandler(_maxValueTextBox, (s, e) =>
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
                maxRow.Children.Add(_maxValueTextBox);
                Grid.SetColumn(_maxValueTextBox, 1);
                _valueContainer.Children.Add(maxRow);
                // 失焦时验证并保存数值（输入过程中不保存）
                _maxValueTextBox.LostFocus += (s, e) =>
                {
                    EnsureHudElementExists(id);
                    var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                    if (elem != null)
                    {
                        int val;
                        if (!int.TryParse(_maxValueTextBox.Text, out val) || _maxValueTextBox.Text.Length == 0)
                            val = maxValueLimit; // 空或无效时设为默认最大值
                        // 上限maxValueLimit
                        if (val > maxValueLimit) val = maxValueLimit;
                        // 向下取整到2的倍数，最小值为2
                        val = Math.Max(2, (val / 2) * 2);
                        elem.CustomMaxValue = val;

                        // 如果当前值超过新最大值，自动调整
                        if (elem.CustomCurrentValue > val)
                        {
                            elem.CustomCurrentValue = val;
                            if (_currentValueTextBox != null)
                            {
                                _currentValueTextBox.Text = val.ToString();
                            }
                        }

                        _maxValueTextBox.Text = val.ToString();
                        SaveSettings();
                        StatusBarService.Instance.RefreshHudElement(id);
                    }
                };
            }

            ContentPanel.Children.Add(_valueContainer);

            // 开关事件：互斥逻辑
            _customToggle.Checked += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null)
                {
                    elem.CustomValueEnabled = true;
                    elem.DataMappingEnabled = false; // 自动关闭数据映射
                }
                _valueContainer.Visibility = Visibility.Visible;

                // 关闭数据映射开关和下拉框
                if (_mappingToggle != null)
                {
                    _mappingToggle.IsChecked = false;
                }
                if (_mappingComboBox != null)
                {
                    _mappingComboBox.Visibility = Visibility.Collapsed;
                }

                SaveSettings();
                StatusBarService.Instance.RefreshHudElement(id);
                RefreshContentHeight();
            };
            _customToggle.Unchecked += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null) elem.CustomValueEnabled = false;
                _valueContainer.Visibility = Visibility.Collapsed;
                SaveSettings();
                StatusBarService.Instance.RefreshHudElement(id);
                RefreshContentHeight();
            };
        }

        private ToggleSwitch AddToggleRow(string labelKey, string descKey, bool defaultVal)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString(labelKey),
                FontWeight = FontWeights.Medium,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextPrimaryBrush")
            };
            var descLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString(descKey),
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            var toggle = new ToggleSwitch { IsChecked = defaultVal };
            grid.Children.Add(toggle);
            Grid.SetColumn(toggle, 1);

            ContentPanel.Children.Add(grid);

            return toggle;
        }
    }
}