using CraftSharp.Models;
using CraftSharp.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
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
        private System.Windows.Controls.TextBlock? _maxValueDisplay; // 显示"/最大值"
        private System.Windows.Controls.TextBox? _saturationTextBox; // 饱和度输入框
        private System.Windows.Controls.TextBlock? _saturationLimitDisplay; // 饱和度上限显示

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
            _maxValueDisplay = null;
            _saturationTextBox = null;
            _saturationLimitDisplay = null;

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
                // health 特殊：有恢复动画选项 + 图标预览
                var settings = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                string iconStyle = settings?.IconStyle ?? "";
                string iconPath = AssetPaths.GetHeartPathWithFallback(iconStyle, "full");
                AddStandardHudElement(id, StatusBarService.Instance.SetHealthVisible, hasRegenAnimation: true, iconPath: iconPath);
            }
            else if (id == "expbar")
            {
                // 经验条特殊：只有当前值，没有最大值，可选经验条或跳跃进度条
                var settings = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                string iconStyle = settings?.IconStyle ?? "";
                string iconPath = GetExpBarIconPath(iconStyle);
                AddStandardHudElement(id, StatusBarService.Instance.SetExpBarVisible, hasMaxValue: false, iconPath: iconPath);
            }
            else if (id == "absorbing")
            {
                // 伤害吸收值特殊：maxValue上限1024，多行显示 + 图标样式选择
                var setVisibleAction = GetSetVisibleAction(id);
                var settings = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                string iconStyle = settings?.IconStyle ?? "";
                string iconPath = GetAbsorbingIconPath(iconStyle);
                AddStandardHudElement(id, setVisibleAction, maxValueLimit: 1024, iconPath: iconPath);
            }
            else if (id == "air")
            {
                // 空气值特殊：有动画效果选项（气泡破裂） + 图标预览
                var setVisibleAction = GetSetVisibleAction(id);
                AddStandardHudElement(id, setVisibleAction, hasRegenAnimation: true, hasAirAnimation: true, iconPath: AssetPaths.Air);
            }
            else if (id == "food" || id == "armor")
            {
                // 标准HUD元素：显示开关 + 数据映射 + 自定义数值（maxValue上限20）
                var setVisibleAction = GetSetVisibleAction(id);
                string iconPath = "";
                if (id == "food")
                {
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                    string iconStyle = settings?.IconStyle ?? "";
                    iconPath = AssetPaths.GetFoodPath(iconStyle, "full");
                }
                else if (id == "armor")
                {
                    iconPath = AssetPaths.ArmorFull;
                }
                AddStandardHudElement(id, setVisibleAction, hasSaturation: id == "food", iconPath: iconPath);
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
        private void AddStandardHudElement(string id, Action<bool>? setVisibleAction, bool hasRegenAnimation = false, bool hasAirAnimation = false, bool hasMaxValue = true, int maxValueLimit = 20, bool hasSaturation = false, string iconPath = "")
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
            int customSaturationValue = settings?.CustomSaturationValue ?? 0;

            // 元素图标预览（如果提供了iconPath）
            if (!string.IsNullOrEmpty(iconPath))
            {
                AddIconPreviewRow("HudOptionElementIcon", iconPath);
            }

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
                // 空气值使用独立的动画描述
                string animLabelKey = hasAirAnimation ? "HudOptionAirAnim" : "HudOptionRegenAnim";
                string animDescKey = hasAirAnimation ? "HudOptionAirAnimDesc" : "HudOptionRegenAnimDesc";
                var regenToggle = AddToggleRow(animLabelKey, animDescKey, regenAnim);
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
            AddCustomValueSection(id, customValueEnabled, customCurrentValue, customMaxValue, hasMaxValue, maxValueLimit, hasSaturation, customSaturationValue);
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
        /// 添加自定义数值配置区域（开关 + 当前值 + 最大值 + 饱和度）
        /// </summary>
        private void AddCustomValueSection(string id, bool enabled, int currentValue, int maxValue, bool hasMaxValue = true, int maxValueLimit = 20, bool hasSaturation = false, int saturationValue = 0)
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
            currentRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            currentRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            currentRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var currentLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("CustomCurrentValue") + ":",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush")
            };
            currentRow.Children.Add(currentLabel);

            // 输入框 + 最大值显示容器
            var inputContainer = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(8, 0, 0, 0)
            };

            _currentValueTextBox = new System.Windows.Controls.TextBox
            {
                Text = currentValue.ToString(),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
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
            inputContainer.Children.Add(_currentValueTextBox);

            // 显示 "/最大值"（如果没有最大值输入框则显示固定上限100）
            _maxValueDisplay = new System.Windows.Controls.TextBlock
            {
                Text = "/" + (hasMaxValue ? maxValue : 100),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0),
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush")
            };
            inputContainer.Children.Add(_maxValueDisplay);

            currentRow.Children.Add(inputContainer);
            Grid.SetColumn(inputContainer, 1);
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
                var maxRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                maxRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                maxRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                maxRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var maxLabel = new System.Windows.Controls.TextBlock
                {
                    Text = GetResourceString("CustomMaxValue") + ":",
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush")
                };
                maxRow.Children.Add(maxLabel);

                // 输入框 + 最大上限显示容器
                var maxInputContainer = new StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Margin = new Thickness(8, 0, 0, 0)
                };

                _maxValueTextBox = new System.Windows.Controls.TextBox
                {
                    Text = maxValue.ToString(),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
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
                maxInputContainer.Children.Add(_maxValueTextBox);

                // 显示"/最大上限"
                var maxValueLimitDisplay = new System.Windows.Controls.TextBlock
                {
                    Text = "/" + maxValueLimit,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 0, 0),
                    Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush")
                };
                maxInputContainer.Children.Add(maxValueLimitDisplay);

                maxRow.Children.Add(maxInputContainer);
                Grid.SetColumn(maxInputContainer, 1);
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

                        // 更新当前值旁边的"/最大值"显示
                        if (_maxValueDisplay != null)
                        {
                            _maxValueDisplay.Text = "/" + val;
                        }

                        // 更新饱和度上限显示和数值（如果存在饱和度字段）
                        if (_saturationLimitDisplay != null)
                        {
                            _saturationLimitDisplay.Text = "/" + val;
                        }
                        if (elem.CustomSaturationValue > val)
                        {
                            elem.CustomSaturationValue = val;
                            if (_saturationTextBox != null)
                            {
                                _saturationTextBox.Text = val.ToString();
                            }
                        }

                        SaveSettings();
                        StatusBarService.Instance.RefreshHudElement(id);
                    }
                };
            }

            // 饱和度行（仅当hasSaturation为true时显示）
            if (hasSaturation)
            {
                var saturationRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                saturationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                saturationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                saturationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var saturationLabel = new System.Windows.Controls.TextBlock
                {
                    Text = GetResourceString("CustomSaturationValue") + ":",
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush")
                };
                saturationRow.Children.Add(saturationLabel);

                // 输入框 + 最大值显示容器
                var saturationInputContainer = new StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Margin = new Thickness(8, 0, 0, 0)
                };

                _saturationTextBox = new System.Windows.Controls.TextBox
                {
                    Text = saturationValue.ToString(),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                // 限制只能输入数字
                _saturationTextBox.PreviewTextInput += (s, e) =>
                {
                    e.Handled = !e.Text.All(c => char.IsDigit(c));
                };
                // 限制粘贴内容
                System.Windows.DataObject.AddPastingHandler(_saturationTextBox, (s, e) =>
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
                saturationInputContainer.Children.Add(_saturationTextBox);

                // 显示"/最大值"（饱和度上限跟随最大值）
                _saturationLimitDisplay = new System.Windows.Controls.TextBlock
                {
                    Text = "/" + maxValue,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 0, 0),
                    Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush")
                };
                saturationInputContainer.Children.Add(_saturationLimitDisplay);

                saturationRow.Children.Add(saturationInputContainer);
                Grid.SetColumn(saturationInputContainer, 1);
                _valueContainer.Children.Add(saturationRow);

                // 失焦时验证并保存数值
                _saturationTextBox.LostFocus += (s, e) =>
                {
                    EnsureHudElementExists(id);
                    var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                    if (elem != null)
                    {
                        int maxVal = elem.CustomMaxValue; // 饱和度上限跟随最大值
                        int val;
                        if (!int.TryParse(_saturationTextBox.Text, out val) || _saturationTextBox.Text.Length == 0)
                            val = 0; // 空或无效时设为默认值0
                        if (val < 0) val = 0;
                        if (val > maxVal) val = maxVal; // 饱和度上限为最大值
                        elem.CustomSaturationValue = val;
                        _saturationTextBox.Text = val.ToString();
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

        // 图标预览 Border（用于点击打开选择器）
        private System.Windows.Controls.Border? _iconPreviewBorder;

        /// <summary>
        /// 添加图标预览行（左侧标签 + 右侧图标预览）
        /// </summary>
        private void AddIconPreviewRow(string labelKey, string iconPath)
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
                Text = GetResourceString(labelKey + "Desc"),
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            // 图标预览
            _iconPreviewBorder = new System.Windows.Controls.Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(6),
                Background = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("CardBackgroundBrush"),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = GetResourceString("AppIconTooltip")
            };
            var iconImage = new System.Windows.Controls.Image
            {
                Name = "IconPreviewImage",
                Source = LoadBitmapImage(iconPath),
                Stretch = Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(iconImage, BitmapScalingMode.NearestNeighbor);
            _iconPreviewBorder.Child = iconImage;
            _iconPreviewBorder.MouseLeftButtonDown += IconPreview_Click;
            grid.Children.Add(_iconPreviewBorder);
            Grid.SetColumn(_iconPreviewBorder, 1);

            ContentPanel.Children.Add(grid);
        }

        /// <summary>
        /// 图标预览点击事件：打开图标选择器
        /// </summary>
        private void IconPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 确定 HUD 元素类型
            string elementType = GetElementTypeFromHudId(_hudId);

            var picker = new HudIconPickerWindow(elementType);
            picker.Owner = System.Windows.Window.GetWindow(this);

            // 获取当前设置的 IconStyle
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == _hudId);
            string currentStyle = settings?.IconStyle ?? "";

            if (picker.ShowDialog() == true && picker.SelectedIconStyle != null)
            {
                // 更新设置
                EnsureHudElementExists(_hudId);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == _hudId);
                if (elem != null)
                {
                    elem.IconStyle = picker.SelectedIconStyle;
                    SaveSettings();
                }

                // 更新图标预览
                if (_iconPreviewBorder != null)
                {
                    var iconImage = _iconPreviewBorder.Child as System.Windows.Controls.Image;
                    if (iconImage != null)
                    {
                        string newIconPath = GetIconPathFromStyle(_hudId, picker.SelectedIconStyle);
                        iconImage.Source = LoadBitmapImage(newIconPath);
                    }
                }

                // 刷新 HUD 元素显示
                StatusBarService.Instance.RefreshHudElement(_hudId);
            }
        }

        /// <summary>
        /// 根据 HudId 获取元素类型（用于图标选择器）
        /// </summary>
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
                _ => hudId
            };
        }

        /// <summary>
        /// 根据 IconStyle 获取图标路径（用于预览更新）
        /// </summary>
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
                _ => ""
            };
        }

        /// <summary>
        /// 根据 IconStyle 获取经验条图标路径
        /// </summary>
        private static string GetExpBarIconPath(string iconStyle)
        {
            if (string.IsNullOrEmpty(iconStyle) || iconStyle == "experience_bar_progress")
            {
                return AssetPaths.ExperienceBarProgress;
            }
            // jump_bar_progress
            return AssetPaths.JumpBarProgress;
        }

        /// <summary>
        /// 根据 IconStyle 获取伤害吸收值图标路径
        /// </summary>
        private static string GetAbsorbingIconPath(string iconStyle)
        {
            if (string.IsNullOrEmpty(iconStyle) || iconStyle == "absorbing_full")
            {
                return AssetPaths.AbsorbingFull;
            }
            // absorbing_hardcore_full
            return "Assets/minecraft/textures/gui/sprites/heart/absorbing_hardcore_full.png";
        }

        /// <summary>
        /// 从文件路径加载 BitmapImage
        /// </summary>
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