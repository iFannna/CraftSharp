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
                AddToggleRow("HudOptionShowHotbar", "HudOptionShowHotbarDesc", true);

                var leftOffhandToggle = AddToggleRow("HudOptionLeftOffhand", "HudOptionLeftOffhandDesc", _settings.HotbarLeftOffhand);
                leftOffhandToggle.Checked += (s, e) => { _settings.HotbarLeftOffhand = true; StatusBarService.Instance.SetOffhandConfig(true, _settings.HotbarRightOffhand); SaveSettings(); };
                leftOffhandToggle.Unchecked += (s, e) => { _settings.HotbarLeftOffhand = false; StatusBarService.Instance.SetOffhandConfig(false, _settings.HotbarRightOffhand); SaveSettings(); };

                var rightOffhandToggle = AddToggleRow("HudOptionRightOffhand", "HudOptionRightOffhandDesc", _settings.HotbarRightOffhand);
                rightOffhandToggle.Checked += (s, e) => { _settings.HotbarRightOffhand = true; StatusBarService.Instance.SetOffhandConfig(_settings.HotbarLeftOffhand, true); SaveSettings(); };
                rightOffhandToggle.Unchecked += (s, e) => { _settings.HotbarRightOffhand = false; StatusBarService.Instance.SetOffhandConfig(_settings.HotbarLeftOffhand, false); SaveSettings(); };
            }
            else
            {
                AddToggleRow("HudOptionShowElement", "HudOptionShowElementDesc", true);
                if (id == "health")
                    AddToggleRow("HudOptionRegenAnim", "HudOptionRegenAnimDesc", false);
                AddMappingRow();
                AddCustomValueRow();
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

        private void AddMappingRow()
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

            var toggle = new ToggleSwitch();
            grid.Children.Add(toggle);
            Grid.SetColumn(toggle, 1);

            ContentPanel.Children.Add(grid);
        }

        private void AddCustomValueRow()
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

            var toggle = new ToggleSwitch();
            grid.Children.Add(toggle);
            Grid.SetColumn(toggle, 1);

            ContentPanel.Children.Add(grid);
        }
    }
}