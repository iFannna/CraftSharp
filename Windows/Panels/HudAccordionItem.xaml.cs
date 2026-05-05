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

        private void AddHudContent(string id)
        {
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
            else if (id == "expbar")
            {
                EnsureHudElementExists("expbar");
                var expBarSettings = _settings.HudElements.FirstOrDefault(h => h.Id == "expbar");
                bool isVisible = expBarSettings?.IsVisible ?? true;
                bool dataMappingEnabled = expBarSettings?.DataMappingEnabled ?? false;
                bool customValueEnabled = expBarSettings?.CustomValueEnabled ?? false;

                var showToggle = AddToggleRow("HudOptionShowElement", "HudOptionShowElementDesc", isVisible);
                showToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("expbar");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "expbar");
                    if (settings != null) settings.IsVisible = true;
                    StatusBarService.Instance.SetExpBarVisible(true);
                    SaveSettings();
                };
                showToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("expbar");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "expbar");
                    if (settings != null) settings.IsVisible = false;
                    StatusBarService.Instance.SetExpBarVisible(false);
                    SaveSettings();
                };

                var mappingToggle = AddMappingRow(dataMappingEnabled);
                mappingToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("expbar");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "expbar");
                    if (settings != null) settings.DataMappingEnabled = true;
                    SaveSettings();
                };
                mappingToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("expbar");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "expbar");
                    if (settings != null) settings.DataMappingEnabled = false;
                    SaveSettings();
                };

                var customToggle = AddCustomValueRow(customValueEnabled);
                customToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("expbar");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "expbar");
                    if (settings != null) settings.CustomValueEnabled = true;
                    SaveSettings();
                };
                customToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("expbar");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "expbar");
                    if (settings != null) settings.CustomValueEnabled = false;
                    SaveSettings();
                };
            }
            else if (id == "health")
            {
                EnsureHudElementExists("health");
                var healthSettings = _settings.HudElements.FirstOrDefault(h => h.Id == "health");
                bool isVisible = healthSettings?.IsVisible ?? true;
                bool regenAnim = healthSettings?.RegenAnimation ?? false;
                bool dataMappingEnabled = healthSettings?.DataMappingEnabled ?? false;
                bool customValueEnabled = healthSettings?.CustomValueEnabled ?? false;

                var showToggle = AddToggleRow("HudOptionShowElement", "HudOptionShowElementDesc", isVisible);
                showToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("health");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "health");
                    if (settings != null) settings.IsVisible = true;
                    StatusBarService.Instance.SetHealthVisible(true);
                    SaveSettings();
                };
                showToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("health");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "health");
                    if (settings != null) settings.IsVisible = false;
                    StatusBarService.Instance.SetHealthVisible(false);
                    SaveSettings();
                };

                var regenToggle = AddToggleRow("HudOptionRegenAnim", "HudOptionRegenAnimDesc", regenAnim);
                regenToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("health");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "health");
                    if (settings != null) settings.RegenAnimation = true;
                    SaveSettings();
                };
                regenToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("health");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "health");
                    if (settings != null) settings.RegenAnimation = false;
                    SaveSettings();
                };

                var mappingToggle = AddMappingRow(dataMappingEnabled);
                mappingToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("health");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "health");
                    if (settings != null) settings.DataMappingEnabled = true;
                    SaveSettings();
                };
                mappingToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("health");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "health");
                    if (settings != null) settings.DataMappingEnabled = false;
                    SaveSettings();
                };

                var customToggle = AddCustomValueRow(customValueEnabled);
                customToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("health");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "health");
                    if (settings != null) settings.CustomValueEnabled = true;
                    SaveSettings();
                };
                customToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("health");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "health");
                    if (settings != null) settings.CustomValueEnabled = false;
                    SaveSettings();
                };
            }
            else if (id == "food")
            {
                EnsureHudElementExists("food");
                var foodSettings = _settings.HudElements.FirstOrDefault(h => h.Id == "food");
                bool isVisible = foodSettings?.IsVisible ?? true;
                bool dataMappingEnabled = foodSettings?.DataMappingEnabled ?? false;
                bool customValueEnabled = foodSettings?.CustomValueEnabled ?? false;

                var showToggle = AddToggleRow("HudOptionShowElement", "HudOptionShowElementDesc", isVisible);
                showToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("food");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "food");
                    if (settings != null) settings.IsVisible = true;
                    StatusBarService.Instance.SetFoodVisible(true);
                    SaveSettings();
                };
                showToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("food");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "food");
                    if (settings != null) settings.IsVisible = false;
                    StatusBarService.Instance.SetFoodVisible(false);
                    SaveSettings();
                };

                var mappingToggle = AddMappingRow(dataMappingEnabled);
                mappingToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("food");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "food");
                    if (settings != null) settings.DataMappingEnabled = true;
                    SaveSettings();
                };
                mappingToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("food");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "food");
                    if (settings != null) settings.DataMappingEnabled = false;
                    SaveSettings();
                };

                var customToggle = AddCustomValueRow(customValueEnabled);
                customToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("food");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "food");
                    if (settings != null) settings.CustomValueEnabled = true;
                    SaveSettings();
                };
                customToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("food");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "food");
                    if (settings != null) settings.CustomValueEnabled = false;
                    SaveSettings();
                };
            }
            else if (id == "air")
            {
                EnsureHudElementExists("air");
                var airSettings = _settings.HudElements.FirstOrDefault(h => h.Id == "air");
                bool isVisible = airSettings?.IsVisible ?? true;
                bool dataMappingEnabled = airSettings?.DataMappingEnabled ?? false;
                bool customValueEnabled = airSettings?.CustomValueEnabled ?? false;

                var showToggle = AddToggleRow("HudOptionShowElement", "HudOptionShowElementDesc", isVisible);
                showToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("air");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "air");
                    if (settings != null) settings.IsVisible = true;
                    StatusBarService.Instance.SetAirVisible(true);
                    SaveSettings();
                };
                showToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("air");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "air");
                    if (settings != null) settings.IsVisible = false;
                    StatusBarService.Instance.SetAirVisible(false);
                    SaveSettings();
                };

                var mappingToggle = AddMappingRow(dataMappingEnabled);
                mappingToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("air");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "air");
                    if (settings != null) settings.DataMappingEnabled = true;
                    SaveSettings();
                };
                mappingToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("air");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "air");
                    if (settings != null) settings.DataMappingEnabled = false;
                    SaveSettings();
                };

                var customToggle = AddCustomValueRow(customValueEnabled);
                customToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("air");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "air");
                    if (settings != null) settings.CustomValueEnabled = true;
                    SaveSettings();
                };
                customToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("air");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "air");
                    if (settings != null) settings.CustomValueEnabled = false;
                    SaveSettings();
                };
            }
            else if (id == "armor")
            {
                EnsureHudElementExists("armor");
                var armorSettings = _settings.HudElements.FirstOrDefault(h => h.Id == "armor");
                bool isVisible = armorSettings?.IsVisible ?? true;
                bool dataMappingEnabled = armorSettings?.DataMappingEnabled ?? false;
                bool customValueEnabled = armorSettings?.CustomValueEnabled ?? false;

                var showToggle = AddToggleRow("HudOptionShowElement", "HudOptionShowElementDesc", isVisible);
                showToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("armor");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "armor");
                    if (settings != null) settings.IsVisible = true;
                    StatusBarService.Instance.SetArmorVisible(true);
                    SaveSettings();
                };
                showToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("armor");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "armor");
                    if (settings != null) settings.IsVisible = false;
                    StatusBarService.Instance.SetArmorVisible(false);
                    SaveSettings();
                };

                var mappingToggle = AddMappingRow(dataMappingEnabled);
                mappingToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("armor");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "armor");
                    if (settings != null) settings.DataMappingEnabled = true;
                    SaveSettings();
                };
                mappingToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("armor");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "armor");
                    if (settings != null) settings.DataMappingEnabled = false;
                    SaveSettings();
                };

                var customToggle = AddCustomValueRow(customValueEnabled);
                customToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("armor");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "armor");
                    if (settings != null) settings.CustomValueEnabled = true;
                    SaveSettings();
                };
                customToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("armor");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "armor");
                    if (settings != null) settings.CustomValueEnabled = false;
                    SaveSettings();
                };
            }
            else if (id == "absorbing")
            {
                EnsureHudElementExists("absorbing");
                var absorbingSettings = _settings.HudElements.FirstOrDefault(h => h.Id == "absorbing");
                bool isVisible = absorbingSettings?.IsVisible ?? true;
                bool dataMappingEnabled = absorbingSettings?.DataMappingEnabled ?? false;
                bool customValueEnabled = absorbingSettings?.CustomValueEnabled ?? false;

                var showToggle = AddToggleRow("HudOptionShowElement", "HudOptionShowElementDesc", isVisible);
                showToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("absorbing");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "absorbing");
                    if (settings != null) settings.IsVisible = true;
                    StatusBarService.Instance.SetAbsorbingVisible(true);
                    SaveSettings();
                };
                showToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("absorbing");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "absorbing");
                    if (settings != null) settings.IsVisible = false;
                    StatusBarService.Instance.SetAbsorbingVisible(false);
                    SaveSettings();
                };

                var mappingToggle = AddMappingRow(dataMappingEnabled);
                mappingToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("absorbing");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "absorbing");
                    if (settings != null) settings.DataMappingEnabled = true;
                    SaveSettings();
                };
                mappingToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("absorbing");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "absorbing");
                    if (settings != null) settings.DataMappingEnabled = false;
                    SaveSettings();
                };

                var customToggle = AddCustomValueRow(customValueEnabled);
                customToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists("absorbing");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "absorbing");
                    if (settings != null) settings.CustomValueEnabled = true;
                    SaveSettings();
                };
                customToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists("absorbing");
                    var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "absorbing");
                    if (settings != null) settings.CustomValueEnabled = false;
                    SaveSettings();
                };
            }
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

        private ToggleSwitch AddMappingRow(bool defaultVal)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
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

            var toggle = new ToggleSwitch { IsChecked = defaultVal };
            grid.Children.Add(toggle);
            Grid.SetColumn(toggle, 1);

            ContentPanel.Children.Add(grid);

            return toggle;
        }

        private ToggleSwitch AddCustomValueRow(bool defaultVal)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
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

            var toggle = new ToggleSwitch { IsChecked = defaultVal };
            grid.Children.Add(toggle);
            Grid.SetColumn(toggle, 1);

            ContentPanel.Children.Add(grid);

            return toggle;
        }
    }
}