using CraftSharp.Models;
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

        public InventoryAccordionItem(AppSettings? settings, string titleResourceKey)
        {
            InitializeComponent();

            _settings = settings;
            _titleResourceKey = titleResourceKey;

            // 使用动态资源获取标题
            TitleText.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, titleResourceKey);

            // 根据卡片类型添加内容
            AddCardContent(titleResourceKey);
        }

        private void AddCardContent(string titleResourceKey)
        {
            if (titleResourceKey == "InventoryTitle" && _settings != null)
            {
                // 物品栏卡片：添加显示物品栏、锁定位置、记住位置开关
                AddToggleRow("InventoryOptionVisible", "InventoryOptionVisibleDesc", _settings.InventoryWindowVisible, v => _settings.InventoryWindowVisible = v);
                AddToggleRow("InventoryOptionLocked", "InventoryOptionLockedDesc", _settings.InventoryWindowLocked, v => _settings.InventoryWindowLocked = v);
                AddToggleRow("InventoryOptionRememberPosition", "InventoryOptionRememberPositionDesc", _settings.InventoryWindowRememberPosition, v => _settings.InventoryWindowRememberPosition = v);
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

        private void SaveSettings()
        {
            if (System.Windows.Application.Current is App app)
            {
                app.SaveSettings();
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
    }
}