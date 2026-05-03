using CraftSharp.Models;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Panels
{
    public partial class HudAccordionItem : System.Windows.Controls.UserControl
    {
        private bool _isExpanded = false;
        private AppSettings _settings;
        private string _hudId;

        public HudAccordionItem(AppSettings settings, string id, string name, string iconColor)
        {
            InitializeComponent();
            _settings = settings;
            _hudId = id;

            TitleText.Text = name;
            AddHudContent(id);
        }

        private void Header_Click(object sender, RoutedEventArgs e)
        {
            _isExpanded = !_isExpanded;
            ContentCard.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
            ArrowRotate.Angle = _isExpanded ? 180 : 0;
        }

        private void AddHudContent(string id)
        {
            if (id == "hotbar")
            {
                AddToggleRow("显示快捷栏", "在桌面显示快捷栏窗口", true);
                AddToggleRow("锁定位置", "防止快捷栏被意外拖动", false);
                AddToggleRow("左副手槽", "在快捷栏左侧显示副手槽位", false);
                AddToggleRow("右副手槽", "在快捷栏右侧显示副手槽位", false);
            }
            else
            {
                AddToggleRow("显示元素", "在快捷栏上显示此 HUD 元素", true);
                if (id == "health")
                    AddToggleRow("恢复动画", "生命值恢复时显示动画效果", false);
                AddMappingRow();
                AddCustomValueRow();
            }
        }

        private void AddToggleRow(string label, string desc, bool defaultVal)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock { Text = label, FontWeight = FontWeights.Medium,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextPrimaryBrush") };
            var descLabel = new System.Windows.Controls.TextBlock { Text = desc,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush"), Margin = new Thickness(0, 4, 0, 0) };
            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            var toggle = new ToggleSwitch { IsChecked = defaultVal };
            grid.Children.Add(toggle);
            Grid.SetColumn(toggle, 1);

            ContentPanel.Children.Add(grid);
        }

        private void AddMappingRow()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock { Text = "数据映射", FontWeight = FontWeights.Medium,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextPrimaryBrush") };
            var descLabel = new System.Windows.Controls.TextBlock { Text = "开启后选择映射的系统数据",
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush"), Margin = new Thickness(0, 4, 0, 0) };
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
            var titleLabel = new System.Windows.Controls.TextBlock { Text = "自定义数值", FontWeight = FontWeights.Medium,
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextPrimaryBrush") };
            var descLabel = new System.Windows.Controls.TextBlock { Text = "开启后手动设置数值",
                Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush"), Margin = new Thickness(0, 4, 0, 0) };
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